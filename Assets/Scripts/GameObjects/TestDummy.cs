using UnityEngine;

/// <summary>
/// TEST-ONLY enemy (Q-T3-4): a stationary target that is IMMORTAL — it never
/// dies at 0 HP — and reports its current HP to the HUD (UiViewHud) so damage
/// values are visible while testing weapons/levels.
///
/// It extends Enemy so it is a fully regular target for bullets (IDamageable,
/// collision plumbing) — but overrides the death rule: TakeDamage clamps HP at
/// 0 and never dies / despawns. Remove this class (and its prefab) when the
/// real enemy roster replaces the test enemies.
///
/// (Task 5) Also overrides Die() to a no-op (the dummy is immortal) and
/// implements the pool Init/ResetObject hooks so it can be pooled like the
/// other enemies.
/// </summary>
public class TestDummy : Enemy
{
    private const int StartHealth = 100;

    private int currentHealth = StartHealth;

    /// <summary>Current HP (never below 0, never kills the dummy).</summary>
    public override int Health => currentHealth;

    /// <summary>
    /// Damage is APPLIED (and reported to the HUD) but the dummy is immortal:
    /// it clamps at 0 and never dies / despawns.
    /// </summary>
    public override void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);
        Debug.Log("[TestDummy] Took " + amount + " damage → HP: " + currentHealth);
        UiViewHud.Instance?.UpdateHealth(currentHealth);
    }

    /// <summary>
    /// (Task 5) The dummy is IMMORTAL — Die() should not set IsAlive=false
    /// (that would stop it reacting to further hits, which breaks the
    /// "immortal target" test purpose). We just log and keep going.
    /// </summary>
    public override void Die()
    {
        // Intentionally does NOT call base.Die() — the dummy stays alive.
        Debug.Log("[TestDummy] Die() called — but the dummy is IMMORTAL, staying in place.");
    }

    /// <summary>(Task 4) Pool Init — reset HP to 100.</summary>
    public override void Init()
    {
        base.Init();
        currentHealth = StartHealth;
        UiViewHud.Instance?.UpdateHealth(currentHealth);
    }

    /// <summary>(Task 4) Pool ResetObject — reset HP to 100.</summary>
    public override void ResetObject()
    {
        currentHealth = StartHealth;
        base.ResetObject();
    }

    /// <summary>Stationary: the dummy never moves (Q-T3-4).</summary>
    protected override void Move()
    {
    }

    /// <summary>(Task 5) The dummy never fires.</summary>
    public override void StartFiring()
    {
        // No-op: the dummy is a stationary target.
    }

    /// <summary>(Task 5) The dummy never fires.</summary>
    public override void Shoot()
    {
        // No-op: the dummy is a stationary target.
    }

    private void Awake()
    {
        currentHealth = StartHealth;
        UiViewHud.Instance?.UpdateHealth(currentHealth);
    }
}
