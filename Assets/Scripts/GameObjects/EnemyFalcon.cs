using UnityEngine;

/// <summary>
/// Enemy ship #2 (test roster — Task 2): a small FALCON fighter that moves
/// down the screen with an EASED approach (its SOMovementParameters asset:
/// pattern Eased — the first real consumer of the template's Kryzarel easing
/// set, Q-T2-1). One of the 7 small enemy ships.
///
/// Deliberately the SAME thin shape as EnemyVulture — the ONLY difference is
/// which SOMovementParameters asset is injected at spawn. That is the
/// data-driven design working: behavior variation lives in DATA, not in code
/// branches (rule 2.8), and adding the rest of the 7-ship roster is a new SO
/// asset + a new thin subclass (or reuse of one), never new math code.
/// </summary>
public class EnemyFalcon : Enemy
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

    /// <summary>Runs the shared pattern math (this enemy's flavor: Eased via its SO asset).</summary>
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
