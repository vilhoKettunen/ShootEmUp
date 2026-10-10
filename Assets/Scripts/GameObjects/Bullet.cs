using UnityEngine;

/// <summary>
/// A projectile fired by the player (later: by enemies too — the SAME class,
/// different SOMovementParameters + Damage, per the "types not tags" rule:
/// a bullet's identity is its DATA, not a tag or a subclass).
///
/// Extends SpaceObject (task brief): it inherits the active state, the
/// collision plumbing and the Despawn ritual. It does NOT extend Enemy —
/// bullets are not enemies; it only gets the COMMON plumbing.
///
/// Lifecycle:
///   1. Spawner Instantiate()s the bullet prefab.
///   2. Spawner calls Init(movementParameters, damage) — INJECTS the movement
///      identity (Q-T2-2: at spawn time, not serialized per prefab) and the
///      damage for THIS shot (from the player's current weapon level).
///   3. Update moves it via the pure MovementPatterns class (ONE line).
///   4. End of life, in either order:
///        a. Duration elapses (SO field; 0 = forever) → Despawn itself.
///        b. It touches ANY SpaceObject (enemy, player, obstacle, ...) →
///           deal damage if the other can take it, then Despawn itself
///           (Q-T3-2/Q-T3-3: a bullet that hits a wall is spent too).
/// </summary>
public class Bullet : SpaceObject
{
    private SOMovementParameters movementParameters;
    private Vector2 spawnOrigin;
    private float elapsed;
    private bool initialized;

    /// <summary>Damage this specific shot deals. Set at spawn (Init), read by whoever it hits.</summary>
    public int Damage { get; private set; }

    /// <summary>The movement asset driving this bullet (read for logging/tests).</summary>
    public SOMovementParameters MovementParameters => movementParameters;

    /// <summary>
    /// Spawner injection point — call ONCE after Instantiate, before the first
    /// meaningful frame. Movement identity + damage come from the caller
    /// (PlayerController's current weapon / later: the enemy's), so this class
    /// never hard-codes any per-weapon values (rule 2.8).
    /// </summary>
    public void Init(SOMovementParameters parameters, int damage)
    {
        movementParameters = parameters;
        Damage = damage;
        spawnOrigin = transform.position;
        initialized = true;
    }

    /// <summary>
    /// What it means for THIS bullet to touch something: the bullet is the
    /// ACTIVE side — if the other object can be damaged, deal our Damage; then
    /// the bullet is spent, whatever it hit (Q-T3-3).
    /// </summary>
    protected override void OnCollision(SpaceObject other)
    {
        if (other is IDamageable damageable)
        {
            damageable.TakeDamage(Damage);
        }

        // Spent — despawn regardless of what we hit.
        Despawn();
    }

    private void Update()
    {
        if (!initialized || !IsActive) return;

        elapsed += Time.deltaTime;

        // ONE line of movement — the pure class knows HOW (Straight/Sine/Target/Eased).
        transform.position = MovementPatterns.GetPosition(movementParameters, elapsed, spawnOrigin);

        // Lifespan (Q-T2-3/Q-T3-3): 0 = forever; otherwise the bullet despawns
        // on its own even if it never hits anything.
        if (movementParameters.Duration > 0f && elapsed >= movementParameters.Duration)
        {
            Despawn();
        }
    }
}
