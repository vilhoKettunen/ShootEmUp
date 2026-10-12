/// <summary>
/// Indestructible hazards (asteroids, debris...).
///
/// Deliberately NOT IDamageable: bullets pass by / are spent on them, but the
/// obstacle itself can never be damaged or destroyed by damage. This class is
/// the proof that the interface approach (instead of a TakeDamage in the base
/// class) was the right call — SpaceObject stays clean of damage members.
///
/// Scrolling motion + "bullet hits obstacle → bullet despawns" behavior is
/// wired through the bullet's own handler (the bullet sees an Obstacle, it is
/// not IDamageable, and simply passes through). Obstacle-specific motion
/// arrives with the scrolling-hazards feature.
/// </summary>
public class Obstacle : SpaceObject
{
    /// <summary>
    /// Scrolling motion hook. No-op for now — implemented when obstacles get
    /// their movement pattern (the same MovementPatterns + SO machinery the
    /// other objects use).
    /// </summary>
    protected virtual void Move()
    {
    }

    /// <summary>
    /// What it means for THIS obstacle to touch something: nothing to command —
    /// the player's own handler decides it got hit, and the bullet's own handler
    /// decides it was spent. Obstacles are passive (react to the other, act on
    /// yourself).
    /// </summary>
    protected override void OnCollision(SpaceObject other)
    {
        // Passive by design — see class summary.
    }
}
