/// <summary>
/// ABSTRACT (teacher's Q1 answer): power-ups and drops that enemies/the world
/// hand to the player. Concrete collectables (weapon pickups, level-up pickups,
/// later: bombs) will extend this class — the base holds the common ritual
/// (Collect + Despawn) and the player-facing hook, each concrete type adds its
/// own payload.
///
/// Not IDamageable: you cannot shoot a power-up into pieces — you pick it up.
/// </summary>
public abstract class Collectable : SpaceObject
{
    /// <summary>
    /// The pickup ritual: apply this collectable's payload, then remove
    /// ourselves from the scene (the base Despawn ritual). Concrete subclasses
    /// add their payload before calling base.Collect().
    /// </summary>
    public virtual void Collect()
    {
        Despawn();
    }

    /// <summary>
    /// What it means for THIS collectable to touch something: if it is the
    /// PLAYER, hand the player a pointer to ourselves — the GAIN logic stays in
    /// the player (separation of responsibilities).
    /// </summary>
    protected override void OnCollision(SpaceObject other)
    {
        if (other is PlayerController player)
        {
            player.Collect(this);
        }
    }
}
