using UnityEngine;

/// <summary>
/// Base class for all enemy types (the 5+ enemy ships + the BossEnemy that the
/// course spec says must extend Enemy).
///
/// Enemy = a SpaceObject that can be damaged (IDamageable), that can move on
/// its own (Move), and that can fire (Fire — the SAME firing method shape the
/// player uses, configured per enemy type; the actual weapon/level wiring is a
/// later task, the virtual hook is here now).
///
/// The movement HOW is the defining difference between enemy types, so Move is
/// virtual: concrete enemies (EnemyVulture, EnemyFalcon, ...) implement their own
/// motion, the boss can override it decisively.
/// </summary>
public class Enemy : SpaceObject, IDamageable
{
    private int health = 10;

    /// <summary>Current health. Read-only from outside — only this enemy changes it.</summary>
    public virtual int Health => health;

    /// <summary>
    /// Applies damage. At 0 HP the enemy dies: Deactivate + Despawn (the base
    /// removal ritual). Drops/scoring are later tasks. Virtual so special
    /// enemies (e.g. the immortal TestDummy) can change the rule.
    /// </summary>
    public virtual void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
        {
            Despawn();
        }
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
    /// Firing hook — the same method shape the player's firing uses, configured
    /// individually per enemy type (teacher's Q2 answer). Actual weapon/level
    /// wiring (SOWeaponSpecs + Weapon) is a later task; the hook is here now so
    /// every enemy type gets it for free.
    /// </summary>
    public virtual void Fire()
    {
    }

    /// <summary>
    /// What it means for THIS enemy to touch something.
    /// A BULLET hitting the enemy: the bullet handles its own side (it damages
    /// us through our public API and despawns itself) — so the enemy just lets
    /// it be. Touching the PLAYER: the player handles its own hit response.
    /// </summary>
    protected override void OnCollision(SpaceObject other)
    {
        // Intentionally empty for now: collision effects flow through each
        // object's own handler (react to the other, act on yourself).
        // Enemy-vs-player damage + enemy shooting arrive with the later
        // enemy-weapon task.
    }
}
