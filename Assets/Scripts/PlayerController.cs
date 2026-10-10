using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The player ship — MODIFIED IN PLACE (rule 2.1: name + file kept), now a
/// SpaceObject (Task 1) with the weapon system wired in (Task 3).
///
/// Responsibilities (kept separated):
///   - INPUT: move + fire (template pattern — flagged for the later
///     InputController refactor, NOT fixed in this task).
///   - WEAPON STATE: current weapon (SOWeaponSpecs) + the GLOBAL level
///     (Q-T3-4: level persists across weapon switches).
///   - FIRING TIMING: fireRate/fireInterval is THE PLAYER'S responsibility
///     (teacher's brief) — the fireTimer lives here; the Weapon only supplies
///     the interval number.
///   - OWN HIT RESPONSE: TakeDamage (IDamageable) + Collect (Collectable hook).
///
/// TEST HOOKS (Q-T3-4/Q-T3-5, removable later — see documentation):
///   [1..5]  switch weapon (by index in the WeaponSpecs list)
///   E       level up    Q       level down
/// </summary>
public class PlayerController : SpaceObject, IDamageable
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;

    [Header("Weapon (Task 3)")]
    [Tooltip("The weapon the player starts with (e.g. MachineGunWeaponSpecs).")]
    [SerializeField] private SOWeaponSpecs defaultWeapon;
    [Tooltip("All weapon types the player can switch between (test hook: [1..5]).")]
    [SerializeField] private List<SOWeaponSpecs> weaponSpecs = new List<SOWeaponSpecs>();

    private InputSystem_Actions controls;
    private Vector2 moveInput;

    private Weapon currentWeapon;
    private int currentLevel = Weapon.MinLevel; // GLOBAL level — persists across weapon switches (Q-T3-4)
    private float fireTimer;
    private bool isFiringHeld; // true while the fire key is held down (performed..canceled)

    /// <summary>The player's current weapon state (read-only).</summary>
    public Weapon CurrentWeapon => currentWeapon;

    /// <summary>The player's global weapon level (1..max of current weapon).</summary>
    public int CurrentLevel => currentLevel;

    #region Unity lifecycle

    private void Awake()
    {
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

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

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
    /// Fire-rate gate: the player owns the timer (brief), the weapon only
    /// supplies the interval number. Called on the first press AND every frame
    /// while the fire key is held — the fireTimer cooldown is what keeps the
    /// rate from exceeding the weapon's interval.
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
    /// Spawns ONE bullet from the current weapon, injecting its movement asset
    /// and the CURRENT-LEVEL damage (Q-T3-2: the bullet carries its own damage).
    /// </summary>
    private void FireCurrentWeapon()
    {
        if (currentWeapon.BulletPrefab == null)
        {
            Debug.LogError("[PlayerController] Weapon '" + currentWeapon.Name + "' has no BulletPrefab assigned.");
            return;
        }

        var spawned = Instantiate(currentWeapon.BulletPrefab,
            transform.position + new Vector3(currentWeapon.SpawnOffset.x, currentWeapon.SpawnOffset.y, 0f),
            Quaternion.identity);

        if (spawned == null)
        {
            Debug.LogError("[PlayerController] Instantiate returned null for '" + currentWeapon.BulletPrefab.name + "'.");
            return;
        }

        Bullet bullet = spawned.GetComponent<Bullet>();
        if (bullet == null)
        {
            Debug.LogError("[PlayerController] Bullet prefab '" + spawned.name
                + "' has no Bullet component on its ROOT. The Bullet script must be on the root GameObject.");
            Destroy(spawned);
            return;
        }

        bullet.Init(currentWeapon.BulletMovement, currentWeapon.CurrentDamage);

        if (currentWeapon.ShootAudioClip != null)
        {
            AudioController.Instance.PlaySoundEffect(currentWeapon.ShootAudioClip);
        }
    }

    #endregion

    #region Weapon state + switching (Q-T3-4: global level)

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
    /// (clamped to the new weapon's max).
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

    /// <summary>Test hook (Q-T3-4): cycle to weapon N (1-based) from the WeaponSpecs list.</summary>
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

        // [1..5] → weapon index 0..4 (edge-triggered: fires once per press,
        // not every frame while held; guard each so a short list is safe)
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

    #region SpaceObject / IDamageable / Collectable hooks (Task 1)

    /// <summary>
    /// The player's own hit response. Lives are a later task — for now the
    /// player just reports the hit (log + HUD) and stays alive.
    /// </summary>
    public void TakeDamage(int amount)
    {
        Debug.Log("[Player] Took " + amount + " damage.");
        // Later task: lives, invulnerability frames, death + Despawn().
    }

    /// <summary>
    /// Collectable hook (Task 1 design): the player owns the "what do I gain"
    /// logic — the collectable only points the player at itself.
    /// Weapon/level power-up application is a later task; the hook exists now.
    /// </summary>
    public void Collect(Collectable collectable)
    {
        Debug.Log("[Player] Collected: " + collectable.GetType().Name);
        // Later task: apply the collectable's payload (weapon switch / level up).
    }

    /// <summary>
    /// What it means for the PLAYER to touch something.
    /// Enemy/Obstacle → player takes damage (own response). Bullet → the bullet
    /// handles its own side. Collectable → the collectable points the player at
    /// itself (handled there).
    /// </summary>
    protected override void OnCollision(SpaceObject other)
    {
        if (other is Enemy)
        {
            // Later task: enemy contact damage value from the enemy's own data.
            TakeDamage(1);
        }
        else if (other is Obstacle)
        {
            TakeDamage(1);
        }
    }

    #endregion
}
