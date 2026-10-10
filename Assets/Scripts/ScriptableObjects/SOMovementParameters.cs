using UnityEngine;

/// <summary>
/// Data-only movement definition. ONE ASSET PER MOVEMENT VARIANT
/// (e.g. "VultureMovement", "FalconMovement", "PlayerBullet_MG").
///
/// The asset is injected into the owning object (Bullet / Enemy subclass) at
/// spawn time — teacher's Q-T2-2 answer — so the SAME script can drive any
/// behavior: the variation lives in DATA (rule 2.8), not in code branches.
///
/// The CONSUMER is the pure C# MovementPatterns class (no MonoBehaviour, no
/// engine services): the SO is data, the math is portable (rule 2.4/2.7).
/// </summary>
[CreateAssetMenu(fileName = "MovementParameters",
                 menuName = "Game/Movement Parameters", order = 1)]
public class SOMovementParameters : ScriptableObject
{
    /// <summary>Which pure function in MovementPatterns drives this object.</summary>
    public enum MovePattern
    {
        Straight, // constant velocity along Direction
        Sine,     // Straight + a perpendicular sine offset (wiggle)
        Target,   // travel from spawn position to (spawn + TargetOffset), then hold
        Eased     // like Target, but progress is shaped by an easing curve (Kryzarel set)
    }

    [SerializeField] private MovePattern pattern = MovePattern.Straight;
    [SerializeField] private Vector2 direction = Vector2.down;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float amplitude = 0f;    // Sine only — world units perpendicular to Direction
    [SerializeField] private float frequency = 0f;    // Sine only — full sine cycles per second
    [SerializeField] private float duration = 0f;     // 0 = move forever; otherwise despawn after N seconds
    [SerializeField] private Vector2 targetOffset = Vector2.zero; // Target only — destination relative to spawn

    public MovePattern Pattern => pattern;
    public Vector2 Direction => direction;
    public float Speed => speed;
    public float Amplitude => amplitude;
    public float Frequency => frequency;
    public float Duration => duration;
    public Vector2 TargetOffset => targetOffset;
}
