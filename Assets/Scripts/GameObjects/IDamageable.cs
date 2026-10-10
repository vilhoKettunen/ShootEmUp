/// <summary>
/// Capability interface: "this object CAN be damaged".
///
/// Inheritance answers "is-a" (an Enemy is a SpaceObject); an interface
/// answers "can-do" (an Enemy can be damaged, an Obstacle cannot). Anything
/// that deals damage does a type check — `if (other is IDamageable d)` —
/// which is a TYPE, not a tag (rule 2.3), and keeps damage out of the base
/// class where Obstacle/Collectable don't need it. Plain C#, no Unity
/// dependency (rule 2.4 portability).
/// </summary>
public interface IDamageable
{
    /// <summary>Applies <paramref name="amount"/> damage to the object. The object decides its own hit response.</summary>
    void TakeDamage(int amount);
}
