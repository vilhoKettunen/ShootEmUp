using UnityEngine;

/// <summary>
/// TEST-ONLY enemy (Q-T3-4): a stationary target that is IMMORTAL — it never
/// dies at 0 HP — and reports its current HP to the HUD (UiViewHud) so damage
/// values are visible while testing weapons/levels.
///
/// It extends Enemy so it is a fully regular target for bullets (IDamageable,
/// collision plumbing) — but overrides the death rule: TakeDamage clamps HP at
/// 0 and never Despawns. Remove this class (and its prefab) when the real
/// enemy roster replaces the test enemies.
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

    /// <summary>Stationary: the dummy never moves (Q-T3-4).</summary>
    protected override void Move()
    {
    }

    private void Awake()
    {
        currentHealth = StartHealth;
        UiViewHud.Instance?.UpdateHealth(currentHealth);
    }
}
