using UnityEngine;

/// <summary>
/// ABSTRACT (teacher's Q1 answer): power-ups and drops that enemies/the world
/// hand to the player. Concrete collectables (weapon pickups, level-up pickups,
/// later: bombs) will extend this class — the base holds the common ritual
/// (Collect + Despawn) and the player-facing hook, each concrete type adds its
/// own payload.
///
/// (Task 5 Q-T5-4) Collect() is "test-only" for now: it logs a DETAILED
/// message (type + what it WOULD do) and returns itself to the pool (Task 4)
/// instead of Despawn — collectables are pooled. The actual payload
/// (weapon switch / level up) is a LATER task.
///
/// Not IDamageable: you cannot shoot a power-up into pieces — you pick it up.
/// </summary>
public abstract class Collectable : SpaceObject
{
    /// <summary>
    /// (Task 5) A short human-readable description of what this collectable
    /// WOULD do if implemented (e.g. "switch to SpreadGun", "level up weapon").
    /// Subclasses override to give a specific description. The base default
    /// is the type name — enough for the test log.
    /// </summary>
    public virtual string PayloadDescription => GetType().Name;

    /// <summary>
    /// The pickup ritual (Task 5, Q-T5-4): log a DETAILED message (what this
    /// collectable is + what it WOULD do), then RETURN to the pool (Task 4) —
    /// collectables are recycled, NOT Destroy'd. Concrete subclasses add their
    /// real payload before calling base.Collect() when that arrives.
    /// </summary>
    public virtual void Collect()
    {
        Debug.Log("[Collectable] Collected: " + GetType().Name +
            " — payload (not implemented yet, test-only): " + PayloadDescription);

        // (Task 4) Recycle to the pool instead of Despawn.
        ApplicationController.Instance.ObjectPool.Return(this);
    }

    /// <summary>
    /// (Task 4) Pool "Get" hook — re-show the collectable.
    /// </summary>
    public override void Init()
    {
        base.Init();
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = true;
    }

    /// <summary>
    /// (Task 4) Pool "Return" hook — hide the sprite so the pooled instance
    /// is invisible while in the queue (SetActive(false) already hides it,
    /// but the sprite flag is reset too in case a subclass re-enabled it).
    /// </summary>
    public override void ResetObject()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = true; // reset to default state
        base.ResetObject();
    }

    /// <summary>
    /// What it means for THIS collectable to touch something: if it is the
    /// PLAYER, (1) the player logs what it gained (player.Collect — separation
    /// of responsibilities: the player owns the "what do I gain" logic), and
    /// (2) this collectable returns itself to the pool (this.Collect — the
    /// collectable owns its own lifecycle + detailed log).
    /// The PLAYER does NOT also call Collectable.Collect (that would double-Return).
    /// </summary>
    protected override void OnCollision(SpaceObject other)
    {
        if (other is PlayerController player)
        {
            player.Collect(this);
            this.Collect();
        }
    }
}
