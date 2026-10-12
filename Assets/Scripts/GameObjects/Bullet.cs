using UnityEngine;

/// <summary>
/// A projectile fired by the player OR by an enemy — the SAME class for both.
/// A bullet's identity is its DATA (owner + movement + damage), never a tag or
/// a subclass.
///
/// Extends SpaceObject and inherits the active state, the collision plumbing,
/// the Despawn ritual, and the pool Init/ResetObject hooks.
///
/// Lifecycle:
///   1. ObjectPool.Get&lt;Bullet&gt;(prefabComponent) dequeues a pooled instance,
///      activates it and calls Bullet.Init() (no-arg, pool hook).
///      The prefab CARRIES its SOMovementParameters.
///   2. The spawner calls SetOwnerAndDamage(owner, damage) — RETURNS this so
///      it chains after Get.
///   3. Update moves it via the pure MovementPatterns class (ONE line).
///   4. End of life, in either order:
///        a. It touches ANY SpaceObject → owner-based damage routing then pool.Return.
///        b. It leaves the camera view → pool.Return.
///        c. Its movement duration elapses → pool.Return.
/// </summary>
public class Bullet : SpaceObject
{
    [Header("Movement (the prefab CARRIES its own movement)")]
    [Tooltip("Movement identity for this bullet (pattern/direction/speed/duration). Serialized so pooled prefabs self-configure on Init().")]
    [SerializeField] private SOMovementParameters movementParameters;

    [Header("Per-shot (set by SetOwnerAndDamage)")]
    [Tooltip("Damage this specific shot deals (overridden per shot by the shooter).")]
    [SerializeField] private int defaultDamage = 1;

    private Vector2 spawnOrigin;
    private float elapsed;
    private bool initialized;
    private SpaceObject owner; // who fired this — PlayerController or an Enemy

    /// <summary>Damage this specific shot deals. Set by SetOwnerAndDamage.</summary>
    public int Damage { get; private set; }

    /// <summary>The movement asset driving this bullet (read for logging/tests).</summary>
    public SOMovementParameters MovementParameters => movementParameters;

    /// <summary>Who fired this bullet (PlayerController or an Enemy).</summary>
    public SpaceObject Owner => owner;

    /// <summary>
    /// Set owner + damage. RETURNS this so it chains after a pool Get:
    ///   Bullet b = pool.Get&lt;Bullet&gt;(proto);
    ///   b.transform.position = desiredSpawnPos;
    ///   b.SetOwnerAndDamage(shooter, damage);   // caches spawnOrigin HERE
    ///
    /// Re-caches spawnOrigin = the CURRENT transform.position, so the spawner
    /// MUST set the position BEFORE calling SetOwnerAndDamage (the standard
    /// pool usage: Get → set position → SetOwnerAndDamage).
    /// </summary>
    public Bullet SetOwnerAndDamage(SpaceObject owner, int damage)
    {
        this.owner = owner;
        Damage = damage;
        spawnOrigin = transform.position;
        return this;
    }

    /// <summary>
    /// Pool "Get" hook — make this bullet ready for use.
    /// Resets the elapsed timer, caches a DEFAULT spawnOrigin (= current
    /// transform.position, which the spawner may overwrite right after Get),
    /// and flags the bullet initialized. Reads its OWN serialized
    /// movementParameters (the prefab carries its config).
    ///
    /// If the spawner calls SetOwnerAndDamage AFTER setting position (the
    /// standard flow), SetOwnerAndDamage RE-CACHES spawnOrigin to the final
    /// spawn point — so the cached origin is always the spawner's intent.
    /// </summary>
    public override void Init()
    {
        base.Init();
        elapsed = 0f;
        spawnOrigin = transform.position;
        if (movementParameters == null)
        {
            Debug.LogWarning("[Bullet] '" + name + "' has no SOMovementParameters assigned — it will not move.");
        }
        // Damage defaults to the prefab's value until SetOwnerAndDamage overwrites it.
        Damage = defaultDamage;
        initialized = true;
    }

    /// <summary>
    /// Pool "Return" hook — stop + reset per-object state.
    /// Clears the owner so a recycled bullet never accidentally keeps its
    /// previous shooter's identity.
    /// </summary>
    public override void ResetObject()
    {
        owner = null;
        initialized = false;
        elapsed = 0f;
        Damage = defaultDamage;
        base.ResetObject();
    }

    /// <summary>
    /// Owner-based hit routing:
    ///   player's bullet  -> damages any IDamageable that is NOT the player (friendly-fire guard)
    ///   enemy's bullet   -> damages the player only
    /// After ANY valid hit the bullet is spent and RETURNED TO THE POOL.
    /// </summary>
    protected override void OnCollision(SpaceObject other)
    {
        // Ignore collisions with the owner — prevents self-hit at spawn time
        // (a bullet spawns near its shooter and would otherwise instantly
        // overlap the shooter's collider on the first physics step).
        if (other == owner) return;

        bool playerShot = owner is PlayerController;
        bool enemyShot  = owner is Enemy;

        bool hitValidTarget = false;

        if (playerShot && other is IDamageable d && !(other is PlayerController))
        {
            Debug.Log($"[Bullet] Player shot hits '{other.name}' for {Damage} damage");
            d.TakeDamage(Damage);
            hitValidTarget = true;
        }
        else if (enemyShot && other is PlayerController p && !(other is Enemy))
        {
            Debug.Log($"[Bullet] Enemy shot hits '{other.name}' for {Damage} damage");
            p.TakeDamage(Damage);
            hitValidTarget = true;
        }

        // Only consume the bullet when it actually hit a valid target.
        // Bullets that overlap a non-target (their own shooter, a friendly,
        // an obstacle) simply pass through and keep flying.
        if (hitValidTarget)
        {
            ApplicationController.Instance.ObjectPool.Return(this);
        }
    }

    /// <summary>
    /// Per-frame movement + end-of-life check:
    ///   1. Advance the elapsed time.
    ///   2. Move via the pure MovementPatterns class (one line).
    ///   3. Recycle the bullet when it leaves the camera view, or when its
    ///      movement duration has elapsed (whichever comes first).
    /// </summary>
    private void Update()
    {
        if (!initialized || !IsActive) return;

        elapsed += Time.deltaTime;

        if (movementParameters == null) return;

        // ONE line of movement — the pure class knows HOW (Straight/Sine/Target/Eased).
        transform.position = MovementPatterns.GetPosition(movementParameters, elapsed, spawnOrigin);

        // Recycle when the bullet leaves the camera view.
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && !CameraHelper.SpriteBoundsInCameraView(sr, Camera.main))
        {
            ApplicationController.Instance.ObjectPool.Return(this);
            return;
        }

        // Lifespan: 0 = forever; otherwise the bullet returns to the pool on
        // its own even if it never hits anything.
        if (movementParameters.Duration > 0f && elapsed >= movementParameters.Duration)
        {
            ApplicationController.Instance.ObjectPool.Return(this);
        }
    }
}
