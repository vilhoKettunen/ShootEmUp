using UnityEngine;

/// <summary>
/// Enemy ship #1 (test roster — Task 2): a small VULTURE fighter that drifts
/// down the screen with a SINE wiggle (its SOMovementParameters asset:
/// pattern Sine). One of the 7 small enemy ships (kenney spaceShips_001..008).
///
/// The class is THIN on purpose: all movement tuning lives in the injected
/// SOMovementParameters asset (data-driven, rule 2.8), and the movement MATH
/// lives in the pure MovementPatterns class (rule 2.4/2.7). The only thing
/// this class adds is "I am an enemy that moves via my injected SO asset" —
/// the exact same shape EnemyFalcon and the rest of the 7-ship roster will use.
///
/// Health/damage/death come from Enemy (Task 1); the bullet that kills it
/// flows through IDamageable (types, not tags).
/// </summary>
public class EnemyVulture : Enemy
{
    private SOMovementParameters movementParameters;
    private Vector2 spawnOrigin;
    private float elapsed;
    private bool initialized;

    /// <summary>Injected at spawn (same pattern as Bullet — Q-T2-2).</summary>
    public void Init(SOMovementParameters parameters)
    {
        movementParameters = parameters;
        spawnOrigin = transform.position;
        initialized = true;
    }

    /// <summary>Runs the shared pattern math (this enemy's flavor: Sine via its SO asset).</summary>
    protected override void Move()
    {
        if (!initialized || !IsActive) return;

        elapsed += Time.deltaTime;
        transform.position = MovementPatterns.GetPosition(movementParameters, elapsed, spawnOrigin);
    }

    private void Update()
    {
        Move();
    }
}
