using UnityEngine;

/// <summary>
/// Enemy ship: a small VULTURE fighter that drifts down the screen with a
/// SINE wiggle (its SOMovementParameters asset: pattern Sine).
///
/// The prefab CARRIES its SOMovementParameters — the serialized field is the
/// default. A spawner can still override it by calling
/// Init(SOMovementParameters) after pool Get.
///
/// The class is THIN on purpose: all movement tuning lives in the
/// SOMovementParameters asset (data-driven), and the movement MATH lives in
/// the pure MovementPatterns class.
/// </summary>
public class EnemyVulture : Enemy
{
    [Header("Movement (the prefab CARRIES its own movement SO)")]
    [SerializeField] private SOMovementParameters movementParameters;

    private Vector2 spawnOrigin;
    private float elapsed;
    private bool initialized;

    /// <summary>
    /// (Optional) Spawner injection — overrides the prefab's serialized
    /// movementParameters. Call AFTER pool Get if you want a different
    /// movement asset than the prefab's default.
    /// </summary>
    public void Init(SOMovementParameters parameters)
    {
        if (parameters != null) movementParameters = parameters;
        spawnOrigin = transform.position;
        initialized = movementParameters != null;
    }

    /// <summary>Pool "Get" hook — re-arm movement state.</summary>
    public override void Init()
    {
        base.Init();
        elapsed = 0f;
        spawnOrigin = transform.position;
        initialized = movementParameters != null;
        if (!initialized)
        {
            Debug.LogWarning("[EnemyVulture] '" + name + "' has no SOMovementParameters assigned — it will not move.");
        }
    }

    /// <summary>Pool "Return" hook — reset movement state.</summary>
    public override void ResetObject()
    {
        initialized = false;
        elapsed = 0f;
        base.ResetObject();
    }

    /// <summary>Runs the shared pattern math (this enemy's flavor: Sine via its SO asset).</summary>
    protected override void Move()
    {
        if (!initialized || !IsActive) return;

        elapsed += Time.deltaTime;
        transform.position = MovementPatterns.GetPosition(movementParameters, elapsed, spawnOrigin);
    }

    /// <summary>Per-frame: advance the movement.</summary>
    private void Update()
    {
        Move();
    }
}
