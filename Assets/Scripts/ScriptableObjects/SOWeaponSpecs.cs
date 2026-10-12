using UnityEngine;

/// <summary>
/// Data-only weapon definition. ONE ASSET PER WEAPON TYPE
/// (e.g. "MachineGunWeaponSpecs", "SpreadGunWeaponSpecs").
///
/// Holds everything that makes a weapon a WEAPON: its name, its bullet prefab,
/// its bullet movement identity (a SOMovementParameters reference — the
/// weapons feel different partly through different bullet motion), its shoot
/// sound, its fire interval (fireRate stays the player's responsibility — the
/// player owns the fireTimer, this asset just supplies the interval number),
/// and the per-level damage table (baseDamage + damagePerLevel, defaults set
/// so the weapons scale to roughly the same DPS).
///
/// The state (current level) does NOT live here — it lives in the plain C#
/// Weapon class (data asset vs. runtime state separation).
/// </summary>
[CreateAssetMenu(fileName = "WeaponSpecs",
                 menuName = "Game/Weapon Specs", order = 2)]
public class SOWeaponSpecs : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string weaponName = "Weapon";
    [SerializeField] private AudioClip shootAudioClip;

    [Header("Projectile")]
    [Tooltip("The bullet prefab this weapon spawns (carries the Bullet script). Typed as GameObject so the Inspector object picker accepts prefabs.")]
    [SerializeField] private GameObject bulletPrefab;
    [Tooltip("Movement identity for every bullet this weapon fires (pattern/direction/speed/duration).")]
    [SerializeField] private SOMovementParameters bulletMovement;
    [Tooltip("World-space offset from the shooter where the bullet spawns (e.g. above the ship).")]
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 1f);

    [Header("Firing")]
    [Tooltip("Seconds between shots. The player (PlayerController) owns the actual fire timer.")]
    [SerializeField] private float fireInterval = 0.2f;
    [SerializeField] private int maxLevel = 5;

    [Header("Damage")]
    [Tooltip("Damage of the first shot before any level-up.")]
    [SerializeField] private int baseDamage = 10;
    [Tooltip("Damage table — index 0..4 = level 1..5. Length should match maxLevel.")]
    [SerializeField] private int[] damagePerLevel = { 10, 14, 19, 25, 32 };

    /// <summary>The weapon's display name.</summary>
    public string WeaponName => weaponName;
    /// <summary>The sound played when this weapon fires.</summary>
    public AudioClip ShootAudioClip => shootAudioClip;
    /// <summary>The bullet prefab this weapon spawns.</summary>
    public GameObject BulletPrefab => bulletPrefab;
    /// <summary>Movement identity for the bullets this weapon fires.</summary>
    public SOMovementParameters BulletMovement => bulletMovement;
    /// <summary>World-space offset from the shooter where the bullet spawns.</summary>
    public Vector2 SpawnOffset => spawnOffset;
    /// <summary>Seconds between shots.</summary>
    public float FireInterval => fireInterval;
    /// <summary>The highest level this weapon can reach.</summary>
    public int MaxLevel => maxLevel;
    /// <summary>Damage of the first shot before any level-up.</summary>
    public int BaseDamage => baseDamage;
    /// <summary>Per-level damage table (index 0 = level 1).</summary>
    public int[] DamagePerLevel => damagePerLevel;

    /// <summary>
    /// Damage for a given level (1-based). Levels outside the table clamp to
    /// the table ends — data and code never fight about edge cases.
    /// </summary>
    public int GetDamageForLevel(int level)
    {
        int[] table = DamagePerLevel;
        if (table == null || table.Length == 0)
        {
            return BaseDamage;
        }

        int index = Mathf.Clamp(level, 1, table.Length) - 1;
        return table[index];
    }
}
