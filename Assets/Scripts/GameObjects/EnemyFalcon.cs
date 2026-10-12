using UnityEngine;

/// <summary>
/// Enemy ship: a small FALCON fighter that moves down the screen with an
/// EASED approach (its SOMovementParameters asset: pattern Eased).
///
/// The prefab CARRIES its SOMovementParameters — the serialized field is the
/// default. A spawner can still override it by calling
/// Init(SOMovementParameters) after pool Get.
///
/// Deliberately the SAME thin shape as EnemyVulture — the ONLY difference is
/// which SOMovementParameters asset is on the prefab. That is the data-driven
/// design: behavior variation lives in DATA, not in code branches.
/// </summary>
public class EnemyFalcon : Enemy
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
            Debug.LogWarning("[EnemyFalcon] '" + name + "' has no SOMovementParameters assigned — it will not move.");
        }
    }

    /// <summary>Pool "Return" hook — reset movement state.</summary>
    public override void ResetObject()
    {
        initialized = false;
        elapsed = 0f;
        base.ResetObject();
    }

    /// <summary>Runs the shared pattern math (this enemy's flavor: Eased via its SO asset).</summary>
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
