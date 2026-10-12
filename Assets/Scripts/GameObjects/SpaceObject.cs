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
///   4. (Task 4) The POOL lifecycle hooks: Init() + ResetObject() — called by
///      ObjectPool on Get/Return, so the pool never reaches into subclass state.
///   5. (Task 5) The shared death state + ritual: IsAlive + virtual Die().
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
    /// (Task 5) Is this object still ALIVE (not dead)? Distinct from IsActive:
    /// IsActive = "in play / allowed to react", IsAlive = "not dead".
    /// Set false by Die(); a pooled recycled object is inactive-but-alive.
    /// </summary>
    public bool IsAlive { get; private set; } = true;

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

    // ---- Task 4 additions: the pool's two lifecycle hooks ----

    /// <summary>
    /// (Task 4) Pool "Get" hook: make this object ready-for-use and active again.
    /// Default just flips the shared state; subclasses override to also re-arm
    /// their OWN per-object state (reset elapsed, re-cache spawnOrigin, reset
    /// Damage/HP, (re)start firing, reposition).
    /// Called by ObjectPool.Get, never by game code directly.
    /// </summary>
    public virtual void Init()
    {
        IsActive = true;
    }

    /// <summary>
    /// (Task 4) Pool "Return" hook: stop + reset per-object behaviour BEFORE the
    /// pool deactivates it (stop firing loops, reset timers/elapsed, clear
    /// one-shot state). Called by ObjectPool.Return, never by game code directly.
    /// </summary>
    public virtual void ResetObject()
    {
        // default: no object-specific reset
    }

    // ---- Task 5 additions: shared death state + ritual ----

    /// <summary>
    /// (Task 5) The shared death ritual: mark dead, then (default) nothing —
    /// subclasses override to add their own death behaviour (recycle to pool for
    /// pooled objects, log + stop for the player, etc.).
    /// Virtual so Player/Enemy can add death effects, drops, scoring, respawn.
    /// </summary>
    public virtual void Die()
    {
        IsAlive = false;
        Debug.Log("[SpaceObject] " + name + " died.");
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
        if (!IsAlive || !otherObject.IsAlive) return; // (Task 5) dead objects don't collide

        OnCollision(otherObject);
    }
}
