using System.Collections;
using UnityEngine;

/// <summary>
/// Base class for all enemy types (the enemy ships + the Boss that extends
/// Enemy per the course spec).
///
/// Enemy = a SpaceObject that can be damaged (IDamageable), that can move on
/// its own (Move), and that can fire (a delayed coroutine loop calling Shoot —
/// the SAME firing shape the player uses, configured per enemy type).
///
/// Firing + death:
///   - StartFiring() is a coroutine loop that waits `fireInterval` seconds,
///     calls Shoot(), and repeats while the enemy is alive.
///   - Shoot() pulls a pooled Bullet, sets owner = this enemy + damage.
///   - TakeDamage() reduces health; at 0 HP the enemy calls Die().
///   - Die() sets IsAlive=false and RETURNS the enemy to the pool — pooled
///     enemies are recycled, not Destroy'd.
///
/// The movement HOW is the defining difference between enemy types, so Move
/// is virtual: concrete enemies (EnemyVulture, EnemyFalcon, ...) implement
/// their own pattern.
/// </summary>
public class Enemy : SpaceObject, IDamageable
{
    [Header("Stats")]
    [Tooltip("Starting health (reset on each pool Init).")]
    [SerializeField] private int startHealth = 30;

    [Header("Firing")]
    [Tooltip("The weapon the enemy fires with (an SOWeaponSpecs asset — reuses the weapon system).")]
    [SerializeField] protected SOWeaponSpecs weapon;
    [Tooltip("Seconds between enemy shots.")]
    [SerializeField] protected float fireInterval = 1f;

    private int health;

    /// <summary>
    /// Initialize health from the serialized startHealth on object creation.
    /// This runs for BOTH pool-spawned enemies (Init() also sets it) and
    /// directly-instantiated enemies (the test harness). Without this, health
    /// defaults to 0 and the enemy dies on the first hit.
    /// </summary>
    protected virtual void Awake()
    {
        health = startHealth;
    }

    private bool firing;
    private Coroutine firingRoutine;

    /// <summary>Current health. Read-only from outside — only this enemy changes it.</summary>
    public virtual int Health => health;

    /// <summary>
    /// Applies damage. At 0 HP the enemy dies — Die() (virtual, the shared
    /// death ritual) which for a pooled enemy means Return-to-pool.
    /// </summary>
    public virtual void TakeDamage(int amount)
    {
        health -= amount;
        Debug.Log($"[Enemy] '{name}' took {amount} damage → HP: {health}");
        if (health <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// The enemy's death ritual: stop firing, mark dead, and RETURN to the
    /// pool — pooled objects are recycled, NOT Destroy'd.
    /// </summary>
    public override void Die()
    {
        StopFiring();
        base.Die(); // sets IsAlive = false + logs
        ApplicationController.Instance.ObjectPool.Return(this);
    }

    /// <summary>
    /// Pool "Get" hook — make this enemy ready for use.
    /// Resets health to startHealth, and (re)starts the firing loop.
    /// </summary>
    public override void Init()
    {
        base.Init();
        health = startHealth;
        StopFiring();
        StartFiring();
    }

    /// <summary>
    /// Pool "Return" hook — stop + reset per-object state.
    /// The firing coroutine must be stopped BEFORE the object is deactivated
    /// (a running coroutine on a deactivated object is a classic Unity leak).
    /// </summary>
    public override void ResetObject()
    {
        StopFiring();
        health = startHealth;
        base.ResetObject();
    }

    /// <summary>
    /// Start the delayed firing loop. Public so non-pool spawn paths (the test
    /// harness, ad-hoc scripts) can call it after Instantiate.
    /// Uses a coroutine — a standard WaitForSeconds loop that fires once every
    /// `fireInterval` seconds. Self-terminates on death / deactivation.
    /// </summary>
    public virtual void StartFiring()
    {
        if (firing) return;
        if (firingRoutine != null) return;
        if (weapon == null || weapon.BulletPrefab == null)
        {
            Debug.LogWarning("[Enemy] '" + name + "' has no weapon assigned — it will not fire.");
            return;
        }
        firing = true;
        firingRoutine = StartCoroutine(FireRoutine());
    }

    /// <summary>Stops the firing loop and clears the coroutine reference.</summary>
    private void StopFiring()
    {
        firing = false;
        if (firingRoutine != null)
        {
            StopCoroutine(firingRoutine);
            firingRoutine = null;
        }
    }

    /// <summary>
    /// The firing coroutine: wait `fireInterval`, shoot, and repeat while the
    /// enemy is still alive, active, and firing.
    /// </summary>
    private IEnumerator FireRoutine()
    {
        // Small initial delay so the first shot isn't instant on spawn.
        yield return new WaitForSeconds(fireInterval);
        while (IsAlive && IsActive && firing)
        {
            Shoot();
            yield return new WaitForSeconds(fireInterval);
        }
    }

    /// <summary>
    /// Fire ONE bullet from the pool, owned by THIS enemy.
    /// The SAME Bullet class the player uses — the OWNER is what makes it an
    /// "enemy bullet".
    /// </summary>
    public virtual void Shoot()
    {
        if (weapon == null || weapon.BulletPrefab == null) return;

        // The pool keys on the GameObject prefab and resolves the ROOT Bullet component internally.
        Bullet b = ApplicationController.Instance.ObjectPool.Get<Bullet>(weapon.BulletPrefab);
        if (b == null) return;

        // Spawn slightly offset from the enemy along the aim direction
        // (bullets fly down for enemy shots by default).
        Vector2 aim = GetAimDirection();
        Vector3 spawnPos = transform.position + (Vector3)(aim.normalized * 0.5f);
        b.transform.position = spawnPos;
        b.SetOwnerAndDamage(this, weapon.GetDamageForLevel(1)); // owner = this enemy
    }

    /// <summary>
    /// Movement hook. The base implementation is a no-op — concrete enemies
    /// implement their own pattern (via MovementPatterns + their own
    /// SOMovementParameters asset), and the boss will override decisively.
    /// </summary>
    protected virtual void Move()
    {
    }

    /// <summary>
    /// Direction this enemy aims its shots. Virtual so the boss (and any
    /// special enemy) can override; the default aims straight down.
    /// </summary>
    protected virtual Vector2 GetAimDirection()
    {
        return Vector2.down;
    }

    /// <summary>
    /// Firing hook (kept for backward compat). The REAL firing loop is
    /// StartFiring/StopFiring. Subclasses that need a one-shot Fire can still
    /// override this.
    /// </summary>
    public virtual void Fire()
    {
        Shoot();
    }

    /// <summary>
    /// What it means for THIS enemy to touch something.
    /// A BULLET hitting the enemy: the bullet handles its own side (it damages
    /// us through our public API and returns itself to the pool) — so the
    /// enemy just lets it be. Touching the PLAYER: the player handles its own
    /// hit response (react to the other, act on yourself).
    /// </summary>
    protected override void OnCollision(SpaceObject other)
    {
        // Intentionally empty: collision effects flow through each
        // object's own handler (react to the other, act on yourself).
    }

    /// <summary>Guarantees the firing coroutine is stopped if the GameObject is destroyed.</summary>
    protected virtual void OnDestroy()
    {
        StopFiring();
    }
}
