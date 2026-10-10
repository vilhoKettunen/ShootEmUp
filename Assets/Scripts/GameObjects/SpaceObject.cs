using UnityEngine;

/// <summary>
/// Base class for every object in the game world (player, enemies, bullets,
/// obstacles, collectables).
///
/// The base class owns only what is COMMON to all space objects:
///   1. The active/inactive game state (IsActive, Activate, Deactivate).
///   2. The despawn ritual (Deactivate + Destroy) — one place, no duplication.
///   3. The collision plumbing: the Unity OnTriggerEnter2D adapter, the shared
///      "both objects must be active" guard, and the virtual OnCollision hook
///      where each subclass decides what a collision means FOR ITSELF.
///
/// What is deliberately NOT here (rule: don't put in the base what derived
/// classes don't need): health, damage, firing, movement. Damage is a
/// CAPABILITY, not a category — the classes that can be damaged implement
/// IDamageable (types, not tags — rule 2.3).
///
/// Design rule applied everywhere: "react to the other, act on yourself" —
/// a collision handler inspects the OTHER object's type and applies the effect
/// to itself / through its own public API.
/// </summary>
public abstract class SpaceObject : MonoBehaviour
{
    /// <summary>
    /// Game state: is this object alive/participating? Read by the game,
    /// changed only through Activate()/Deactivate(). NOT the same as
    /// GameObject.activeSelf (an object may exist in the scene while inactive).
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Marks the object active. Called by spawners and by the object itself.
    /// Subclasses may add their own activation behavior.
    /// </summary>
    public virtual void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Marks the object inactive: it stops moving, stops reacting to collisions.
    /// Subclasses may add their own deactivation behavior.
    /// </summary>
    public virtual void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// The object's removal ritual: deactivate first (so it can no longer react),
    /// then remove it from the scene. "Object removal is NOT Destroy — it is our
    /// own method" (task rule). Subclasses may extend the ritual later
    /// (death effects, drops, scoring) without duplicating the core.
    /// </summary>
    public virtual void Despawn()
    {
        Deactivate();
        Object.Destroy(gameObject);
    }

    /// <summary>
    /// Collision hook. Subclasses override to decide what it means for THEM to
    /// touch the other object. Protected: collision is event-driven, external
    /// code must not invoke collision logic directly.
    /// </summary>
    protected virtual void OnCollision(SpaceObject other)
    {
    }

    /// <summary>
    /// Unity trigger adapter (the ONLY place a Unity message touches this class).
    /// Applies the shared guard — neither object reacts unless BOTH are active —
    /// then hands over to the virtual hook. One GetComponent per event, never
    /// per frame (rule 2.6).
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        SpaceObject otherObject = other.GetComponent<SpaceObject>();
        if (otherObject == null) return;
        if (!IsActive || !otherObject.IsActive) return;

        OnCollision(otherObject);
    }
}
