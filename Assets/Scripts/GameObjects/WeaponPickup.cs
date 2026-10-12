using UnityEngine;

/// <summary>
/// A concrete collectable that represents a WEAPON PICKUP dropped by the world
/// or enemies. It carries a reference to a specific SOWeaponSpecs asset
/// (e.g. "MachineGun", "Plasma", "Spread") — that IS the pickup's payload.
///
/// Behaviour when the player collects it (logged for now, implemented later):
///
///   • SAME weapon as the player's current weapon
///       → LEVEL UP the current weapon by 1 (clamped to that weapon's max).
///       The player's global level increases and is carried to any future
///       weapon switch.
///
///   • DIFFERENT weapon than the player's current one
///       → SWITCH to the picked-up weapon, keeping the player's GLOBAL level
///         (clamped to the new weapon's max). The level does NOT reset.
///
/// This class is "test-only" for now: Collect() logs what WOULD happen and
/// recycles itself to the pool. The actual weapon-switch / level-up logic is a
/// later feature — the logs are the verification that the data flow is correct.
///
/// Prefab: attach this script to a GameObject (with a SpriteRenderer +
/// BoxCollider2D set as a trigger) and save it as a Prefab. Then add that
/// prefab to the SOPoolSpaceObjects asset so it can be recycled.
/// </summary>
public class WeaponPickup : Collectable
{
    [Header("Payload")]
    [Tooltip("The weapon this pickup represents. The player switches to or levels up this specific weapon when collected.")]
    [SerializeField] private SOWeaponSpecs weaponSpecs;

    /// <summary>The weapon asset this pickup grants (read for logging / future implementation).</summary>
    public SOWeaponSpecs WeaponSpecs => weaponSpecs;

    /// <summary>
    /// Human-readable description of what this pickup does.
    /// Used in the player-side log (PlayerController.Collect) and in the
    /// collectable-side log (Collect below).
    /// </summary>
    public override string PayloadDescription
    {
        get
        {
            if (weaponSpecs == null)
            {
                return "NO WEAPON ASSIGNED";
            }

            string weaponName = weaponSpecs.WeaponName;

            // Compare with the player's current weapon to describe the outcome.
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null && player.CurrentWeapon != null)
            {
                string currentWeaponName = player.CurrentWeapon.Name;
                int currentLevel = player.CurrentLevel;

                if (weaponName == currentWeaponName)
                {
                    int nextLevel = Mathf.Min(currentLevel + 1, weaponSpecs.MaxLevel);
                    return "LEVEL UP " + weaponName + " from Lv." + currentLevel + " to Lv." + nextLevel;
                }
                else
                {
                    int clampedLevel = Mathf.Clamp(currentLevel, 1, weaponSpecs.MaxLevel);
                    return "SWITCH to " + weaponName + " at Lv." + clampedLevel + " (keeping global level " + currentLevel + ")";
                }
            }

            return "PICKUP: " + weaponName;
        }
    }

    /// <summary>
    /// The pickup ritual for a weapon pickup:
    ///   1. Log a DETAILED description of what WOULD happen
    ///      (level-up if same weapon, switch if different).
    ///   2. Recycle this collectable back to the pool.
    ///
    /// The PLAYER does NOT call this method — the collectable calls it on
    /// itself via its own OnCollision (see Collectable.OnCollision). The
    /// player's Collect(WeaponPickup) is called separately for the
    /// player-side log.
    /// </summary>
    public override void Collect()
    {
        string weaponName = weaponSpecs != null ? weaponSpecs.WeaponName : "UNKNOWN";

        PlayerController player = FindObjectOfType<PlayerController>();

        Debug.Log("[WeaponPickup] ════════════════════════════════════════════");
        Debug.Log("[WeaponPickup] Collected weapon pickup: '" + weaponName + "'");

        if (weaponSpecs == null)
        {
            Debug.LogWarning("[WeaponPickup] No SOWeaponSpecs assigned on this pickup — nothing to grant.");
            Debug.Log("[WeaponPickup] ════════════════════════════════════════════");
            ApplicationController.Instance.ObjectPool.Return(this);
            return;
        }

        if (player != null && player.CurrentWeapon != null)
        {
            string currentWeaponName = player.CurrentWeapon.Name;
            int currentLevel = player.CurrentLevel;

            if (weaponName == currentWeaponName)
            {
                // ── SAME WEAPON: would level up ──────────────────────────
                int nextLevel = Mathf.Min(currentLevel + 1, weaponSpecs.MaxLevel);
                int newDamage = weaponSpecs.GetDamageForLevel(nextLevel);
                int oldDamage = weaponSpecs.GetDamageForLevel(currentLevel);

                Debug.Log("[WeaponPickup] Same weapon as current → would LEVEL UP");
                Debug.Log("[WeaponPickup]   Weapon:   " + weaponName);
                Debug.Log("[WeaponPickup]   Level:    " + currentLevel + " → " + nextLevel
                    + (nextLevel >= weaponSpecs.MaxLevel ? "  (MAX — clamped)" : ""));
                Debug.Log("[WeaponPickup]   Damage:   " + oldDamage + " → " + newDamage);
                Debug.Log("[WeaponPickup]   (Future: player.IncreaseLevel() or player.SetLevel(" + nextLevel + "))");
            }
            else
            {
                // ── DIFFERENT WEAPON: would switch ───────────────────────
                int clampedLevel = Mathf.Clamp(currentLevel, 1, weaponSpecs.MaxLevel);
                int newDamage = weaponSpecs.GetDamageForLevel(clampedLevel);

                Debug.Log("[WeaponPickup] Different weapon → would SWITCH (keep global level)");
                Debug.Log("[WeaponPickup]   Current:  " + currentWeaponName + " Lv." + currentLevel);
                Debug.Log("[WeaponPickup]   New:      " + weaponName + " Lv." + clampedLevel
                    + (currentLevel > weaponSpecs.MaxLevel ? "  (clamped from " + currentLevel + ")" : ""));
                Debug.Log("[WeaponPickup]   Damage:   " + newDamage);
                Debug.Log("[WeaponPickup]   Fire interval: " + weaponSpecs.FireInterval + "s");
                Debug.Log("[WeaponPickup]   (Future: player.SetWeapon(weaponSpecs) — level is kept automatically)");
            }
        }
        else
        {
            Debug.Log("[WeaponPickup] Player not found — would switch to " + weaponName + " at Lv.1");
        }

        Debug.Log("[WeaponPickup] Recycling pickup to pool.");
        Debug.Log("[WeaponPickup] ════════════════════════════════════════════");

        // Recycle to the pool (collectables are pooled, not destroyed).
        ApplicationController.Instance.ObjectPool.Return(this);
    }
}
