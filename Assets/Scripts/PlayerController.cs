using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The player ship. A SpaceObject that can be damaged, and that owns the
/// weapon state + firing timing for the player.
///
/// Responsibilities (kept separated on purpose):
///   - INPUT: move + fire (reads the new Input System actions).
///   - WEAPON STATE: the current weapon (SOWeaponSpecs) + a GLOBAL weapon
///     level that persists across weapon switches.
///   - FIRING TIMING: the player owns the fire timer — the weapon data asset
///     only supplies the interval number.
///   - OWN HIT RESPONSE: TakeDamage (via IDamageable) + Collect (collectable
///     hook) + OnCollision (what touching each object type means for the player).
///
/// Test hooks (removable later):
///   [1..5]  switch weapon (by index in the WeaponSpecs list)
///   E       level up    Q       level down
/// </summary>
public class PlayerController : SpaceObject, IDamageable
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;

    [Header("Weapon")]
    [Tooltip("The weapon the player starts with (e.g. MachineGunWeaponSpecs).")]
    [SerializeField] private SOWeaponSpecs defaultWeapon;
    [Tooltip("All weapon types the player can switch between (test hook: [1..5]).")]
    [SerializeField] private List<SOWeaponSpecs> weaponSpecs = new List<SOWeaponSpecs>();

    [Header("Health")]
    [Tooltip("Player hit points. At 0 the player dies (Die). Lives / respawn are a later feature.")]
    [SerializeField] private int startHitPoints = 100;

    private int hitPoints;

    /// <summary>Current hit points. Read-only from outside.</summary>
    public int HitPoints => hitPoints;

    private InputSystem_Actions controls;
    private Vector2 moveInput;

    private Weapon currentWeapon;
    private int currentLevel = Weapon.MinLevel; // GLOBAL level — persists across weapon switches
    private float fireTimer;
    private bool isFiringHeld; // true while the fire key is held down (performed..canceled)

    /// <summary>The player's current weapon state (read-only).</summary>
    public Weapon CurrentWeapon => currentWeapon;

    /// <summary>The player's global weapon level (1..max of current weapon).</summary>
    public int CurrentLevel => currentLevel;

    #region Unity lifecycle

    /// <summary>
    /// One-time setup: seeds HP from the prefab's default, wires the input
    /// actions (move + held-fire) and initializes the starting weapon.
    /// </summary>
    private void Awake()
    {
        hitPoints = startHitPoints; // seed HP from the prefab's default

        controls = new InputSystem_Actions();

        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        // Fire is HELD (continuous) input, not a single edge press:
        //   performed → mark "holding" true
        //   canceled  → mark "holding" false
        // Update() then tries to fire every frame while held, and the
        // fireTimer cooldown in FireAtInterval() enforces the weapon's interval.
        controls.Player.Jump.performed += ctx => isFiringHeld = true;
        controls.Player.Jump.canceled += ctx => isFiringHeld = false;

        InitializeWeapon();
    }

    /// <summary>Activates the input actions when the player becomes active.</summary>
    private void OnEnable()
    {
        controls.Enable();
    }

    /// <summary>Deactivates the input actions when the player becomes inactive.</summary>
    private void OnDisable()
    {
        controls.Disable();
    }

    /// <summary>
    /// Per-frame update:
    ///   1. Move the ship by the current input * speed * frame time.
    ///   2. While the fire key is held, try to fire (gated by the cooldown).
    ///   3. Handle the test keyboard hooks (weapon switch / level change).
    /// </summary>
    private void Update()
    {
        Vector3 movement = new Vector3(moveInput.x, moveInput.y, 0f);
        transform.position += movement * moveSpeed * Time.deltaTime;

        // Continuous fire: while the key is held, try to fire every frame.
        // FireAtInterval() gates it with the fireTimer cooldown, so holding the
        // key auto-fires at the weapon's fire rate and spams can't exceed it.
        if (isFiringHeld)
        {
            FireAtInterval();
        }

        HandleTestHooks();
    }

    #endregion

    #region Firing (fireRate is the player's responsibility)

    /// <summary>
    /// Fire-rate gate: the player owns the timer, the weapon only supplies the
    /// interval number. Called on the first press AND every frame while the
    /// fire key is held — the fireTimer cooldown is what keeps the rate from
    /// exceeding the weapon's interval.
    /// </summary>
    private void FireAtInterval()
    {
        if (currentWeapon == null)
        {
            return;
        }

        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f)
        {
            return; // still cooling down
        }

        fireTimer = currentWeapon.FireInterval;
        FireCurrentWeapon();
    }

    /// <summary>
    /// Fire ONE bullet from the current weapon:
    ///   1. Resolve the weapon's bullet prefab to a pooled Bullet component.
    ///   2. Place it at the weapon's spawn offset from the player.
    ///   3. Mark the player as the bullet's OWNER + set the current level's
    ///      damage — the owner is what makes it a "player bullet".
    ///   4. Play the weapon's shoot sound.
    /// </summary>
    private void FireCurrentWeapon()
    {
        if (currentWeapon.BulletPrefab == null)
        {
            Debug.LogError("[PlayerController] Weapon '" + currentWeapon.Name + "' has no BulletPrefab assigned.");
            return;
        }

        // Get a ready bullet from the pool (the pool keys on the GameObject
        // prefab and resolves the ROOT Bullet component internally).
        Bullet b = ApplicationController.Instance.ObjectPool.Get<Bullet>(currentWeapon.BulletPrefab);
        if (b == null)
        {
            Debug.LogError("[PlayerController] ObjectPool.Get<Bullet> returned null — is the bullet prefab in PoolSpaceObjects?");
            return;
        }

        // Position it (the pool returns it at whatever position it was last at;
        // the spawner is responsible for placing it).
        b.transform.position = transform.position + new Vector3(currentWeapon.SpawnOffset.x, currentWeapon.SpawnOffset.y, 0f);

        // Set owner = player + damage = the current level's damage.
        // SetOwnerAndDamage RETURNS this so it could chain after Get; we use it
        // as a statement here for readability.
        b.SetOwnerAndDamage(this, currentWeapon.CurrentDamage);

        if (currentWeapon.ShootAudioClip != null)
        {
            AudioController.Instance.PlaySoundEffect(currentWeapon.ShootAudioClip);
        }
    }

    #endregion

    #region Weapon state + switching (global level)

    /// <summary>
    /// Sets up the player's starting weapon (the defaultWeapon asset).
    /// </summary>
    private void InitializeWeapon()
    {
        if (defaultWeapon == null)
        {
            Debug.LogWarning("[PlayerController] No defaultWeapon assigned — the player cannot fire.");
            return;
        }

        SetWeapon(defaultWeapon);
    }

    /// <summary>
    /// Switches the player's weapon and KEEPS the player's global level
    /// (clamped to the new weapon's max). Updates the HUD.
    /// </summary>
    public void SetWeapon(SOWeaponSpecs specs)
    {
        if (specs == null)
        {
            return;
        }

        currentWeapon = new Weapon(specs, currentLevel);
        fireTimer = 0f;

        UiViewHud.Instance?.UpdateWeapon(currentWeapon.Name);
        UiViewHud.Instance?.UpdateLevel(currentLevel);
    }

    /// <summary>Level up (clamped to the current weapon's maxLevel). Test hook: E.</summary>
    public void IncreaseLevel()
    {
        SetLevel(currentLevel + 1);
    }

    /// <summary>Level down (clamped to 1). Test hook: Q.</summary>
    public void DecreaseLevel()
    {
        SetLevel(currentLevel - 1);
    }

    /// <summary>
    /// Changes the player's global weapon level (clamped to the weapon's
    /// range) and rebuilds the current Weapon at the new level. Updates the HUD.
    /// </summary>
    private void SetLevel(int newLevel)
    {
        if (currentWeapon == null)
        {
            return;
        }

        currentLevel = Mathf.Clamp(newLevel, Weapon.MinLevel, currentWeapon.MaxLevel);
        currentWeapon = new Weapon(currentWeapon.Specs, currentLevel);

        UiViewHud.Instance?.UpdateLevel(currentLevel);
    }

    /// <summary>
    /// Test hook: switch weapons / change level from the keyboard.
    ///   [1..5] → weapon index 0..4 (edge-triggered: fires once per press,
    ///            not every frame while held; guarded so a short list is safe)
    ///   E      → level up
    ///   Q      → level down
    /// </summary>
    private void HandleTestHooks()
    {
        if (weaponSpecs.Count == 0)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (weaponSpecs.Count > 0 && Keyboard.current.digit1Key.wasPressedThisFrame) SetWeapon(weaponSpecs[0]);
        if (weaponSpecs.Count > 1 && Keyboard.current.digit2Key.wasPressedThisFrame) SetWeapon(weaponSpecs[1]);
        if (weaponSpecs.Count > 2 && Keyboard.current.digit3Key.wasPressedThisFrame) SetWeapon(weaponSpecs[2]);
        if (weaponSpecs.Count > 3 && Keyboard.current.digit4Key.wasPressedThisFrame) SetWeapon(weaponSpecs[3]);
        if (weaponSpecs.Count > 4 && Keyboard.current.digit5Key.wasPressedThisFrame) SetWeapon(weaponSpecs[4]);

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            IncreaseLevel();
        }
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            DecreaseLevel();
        }
    }

    #endregion

    #region SpaceObject / IDamageable / Collectable hooks

    /// <summary>
    /// The player's own hit response: reduce HP, log, update the HUD, and at
    /// 0 HP call Die(). Lives / respawn / invulnerability are a later feature.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (!IsAlive) return; // ignore hits while dead

        hitPoints -= amount;
        Debug.Log("[Player] Took " + amount + " damage → HP: " + hitPoints);
        UiViewHud.Instance?.UpdateHealth(hitPoints);

        if (hitPoints <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// The player's death ritual. "Test-only" for now: log a clear message and
    /// mark the player dead. Lives / respawn / invulnerability are a later
    /// feature — the Die() hook is the extension point.
    /// </summary>
    public override void Die()
    {
        base.Die(); // sets IsAlive = false + logs
        Debug.Log("[Player] Player has died.");
        // Later: respawn / game-over UI / input stop.
    }

    /// <summary>
    /// Collectable hook — the player owns the "what do I gain" logic
    /// (separation of responsibilities). The collectable points the player at
    /// itself; the player logs what it gained (and, later, applies the payload
    /// — weapon switch / level up). The collectable's own Collect() handles its
    /// own pool Return, so this method does NOT call back into it (that would
    /// double-Return).
    ///
    /// For a WeaponPickup specifically, this method logs the PLAYER's
    /// perspective: "I currently have weapon X at level Y, and I just picked
    /// up Z." The detailed outcome (level-up vs. switch) is logged by the
    /// WeaponPickup's own Collect(). Together the two logs give a complete
    /// picture.
    /// </summary>
    public void Collect(Collectable collectable)
    {
        Debug.Log("[Player] Picked up collectable: " + collectable.GetType().Name
            + " (payload: " + collectable.PayloadDescription + ")");

        // Extra context for weapon pickups: log the player's current weapon
        // state so the log reader can see the before/after.
        if (collectable is WeaponPickup pickup)
        {
            Debug.Log("[Player] My current weapon: "
                + (CurrentWeapon != null ? CurrentWeapon.Name + " Lv." + CurrentLevel : "NONE")
                + " → picked up: "
                + (pickup.WeaponSpecs != null ? pickup.WeaponSpecs.WeaponName : "unknown"));
        }

        // Later: apply the collectable's payload (weapon switch / level up).
        // For a WeaponPickup this would be:
        //   if same weapon  → IncreaseLevel()
        //   if different    → SetWeapon(pickup.WeaponSpecs)  (level is kept)
    }

    /// <summary>
    /// What it means for the PLAYER to touch something:
    ///   Enemy       → player takes contact damage (placeholder value for now).
    ///   Obstacle    → player takes contact damage (placeholder value for now).
    ///   Collectable → the COLLECTABLE handles its own side (it calls
    ///                  player.Collect(this) for the gain log + pool Return).
    ///                  We do NOT react here — that would double-Return.
    ///   Bullet      → the bullet handles its own side (it damages us through
    ///                  our public TakeDamage and returns itself to the pool).
    /// </summary>
    protected override void OnCollision(SpaceObject other)
    {
        if (other is Enemy)
        {
            // Placeholder contact damage (data-driven enemy stats = later feature).
            TakeDamage(1);
        }
        else if (other is Obstacle)
        {
            // Placeholder contact damage.
            TakeDamage(1);
        }
        // Collectables + Bullets handle THEIR OWN side — they call our public
        // APIs (Collect / TakeDamage) when they touch us.
    }

    #endregion
}
