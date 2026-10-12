using UnityEngine;

/// <summary>
/// A projectile fired by the player OR by an enemy — the SAME class for both
/// (Task 5, brief: "Sama Bullet-luokka toimii sekä pelaajan että vihollisten
/// ammuksille"). A bullet's identity is its DATA (owner + movement + damage),
/// never a tag or a subclass (rule 2.3).
///
/// Extends SpaceObject (Task 1) and inherits the active state, the collision
/// plumbing, the Despawn ritual, and the pool Init/ResetObject hooks (Task 4).
///
/// Lifecycle:
///   1. ObjectPool.Get<Bullet>(prefabComponent) dequeues a pooled instance,
///      activates it and calls Bullet.Init() (no-arg, pool hook — Task 4).
///      The prefab CARRIES its SOMovementParameters (Q-T4-1 option (a)).
///   2. The spawner calls SetOwnerAndDamage(owner, damage) — RETURNS this so
///      it chains after Get (Task 5, brief: "Metodi palauttaa this").
///   3. Update moves it via the pure MovementPatterns class (ONE line).
///   4. End of life, in either order:
///        a. It touches ANY SpaceObject (enemy, player, obstacle, ...) →
///           owner-based damage routing (Task 5) then pool.Return (Task 4).
///        b. It leaves the camera view → pool.Return (Task 5 + CameraHelper).
/// </summary>
public class Bullet : SpaceObject
{
    [Header("Movement (Task 4 Q-T4-1a: the prefab CARRIES its own movement)")]
    [Tooltip("Movement identity for this bullet (pattern/direction/speed/duration). Serialized so pooled prefabs self-configure on Init().")]
    [SerializeField] private SOMovementParameters movementParameters;

    [Header("Per-shot (set by SetOwnerAndDamage, Task 5)")]
    [Tooltip("Damage this specific shot deals (overridden per shot by the shooter).")]
    [SerializeField] private int defaultDamage = 1;

    private Vector2 spawnOrigin;
    private float elapsed;
    private bool initialized;
    private SpaceObject owner; // (Task 5) who fired this — PlayerController or an Enemy

    /// <summary>Damage this specific shot deals. Set by SetOwnerAndDamage (Task 5).</summary>
    public int Damage { get; private set; }

    /// <summary>The movement asset driving this bullet (read for logging/tests).</summary>
    public SOMovementParameters MovementParameters => movementParameters;

    /// <summary>(Task 5) Who fired this bullet (PlayerController or an Enemy).</summary>
    public SpaceObject Owner => owner;

    /// <summary>
    /// (Task 5) Set owner + damage. RETURNS this so it chains after a pool Get:
    ///   Bullet b = pool.Get<Bullet>(proto);
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
    /// (Task 4) Pool "Get" hook — make this bullet ready for use.
    /// Resets the elapsed timer, caches a DEFAULT spawnOrigin (= current
    /// transform.position, which the spawner may overwrite right after Get),
    /// and flags the bullet initialized. Reads its OWN serialized
    /// movementParameters (Q-T4-1a: the prefab carries its config).
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
    /// (Task 4) Pool "Return" hook — stop + reset per-object state.
    /// Clears the owner (Task 5) so a recycled bullet never accidentally
    /// keeps its previous shooter's identity.
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
    /// (Task 5) Owner-based hit routing:
    ///   player's bullet  -> damages any IDamageable that is NOT the player (friendly-fire guard)
    ///   enemy's bullet   -> damages the player only
    /// After ANY hit the bullet is spent and RETURNED TO THE POOL (Task 4).
    /// </summary>
    protected override void OnCollision(SpaceObject other)
    {
        bool playerShot = owner is PlayerController;
        bool enemyShot  = owner is Enemy;

        if (playerShot && other is IDamageable d && !(other is PlayerController))
        {
            d.TakeDamage(Damage);
        }
        else if (enemyShot && other is PlayerController p && !(other is Enemy))
        {
            p.TakeDamage(Damage);
        }

        // Spent — return to the pool (Task 4), NOT Destroy.
        // (Task 5 friendly-fire guard: a bullet that hits a non-damageable
        // obstacle / the wrong side is still spent and recycled.)
        ApplicationController.Instance.ObjectPool.Return(this);
    }

    private void Update()
    {
        if (!initialized || !IsActive) return;

        elapsed += Time.deltaTime;

        if (movementParameters == null) return;

        // ONE line of movement — the pure class knows HOW (Straight/Sine/Target/Eased).
        transform.position = MovementPatterns.GetPosition(movementParameters, elapsed, spawnOrigin);

        // (Task 5) Recycle when the bullet leaves the camera view.
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && !CameraHelper.SpriteBoundsInCameraView(sr, Camera.main))
        {
            ApplicationController.Instance.ObjectPool.Return(this);
        }

        // Lifespan (Q-T2-3/Q-T3-3): 0 = forever; otherwise the bullet returns
        // to the pool on its own even if it never hits anything.
        else if (movementParameters.Duration > 0f && elapsed >= movementParameters.Duration)
        {
            ApplicationController.Instance.ObjectPool.Return(this);
        }
    }
}
