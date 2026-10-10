using UnityEngine;

/// <summary>
/// WEAPON STATE (plain C# — NO MonoBehaviour, rule 2.4): wraps a
/// SOWeaponSpecs data asset with the player's current LEVEL and exposes the
/// read-only values the firing code needs.
///
/// Data vs. state separation:
///   SOWeaponSpecs (ScriptableObject) = WHAT the weapon is (assets, designer-tuned)
///   Weapon            (plain class)  = WHERE the player is on that weapon (level)
///
/// The level is GLOBAL across weapons (Q-T3-4): PlayerController owns the
/// level and hands it to every Weapon it creates, so switching weapons keeps
/// the player's level. The Weapon class itself is stateless beyond the level —
/// it is created/destroyed cheaply on every switch.
/// </summary>
public class Weapon
{
    public const int MinLevel = 1;

    private readonly SOWeaponSpecs specs;
    private readonly int level;

    /// <summary>Creates a weapon at the given (global) level, clamped to the weapon's 1..MaxLevel.</summary>
    public Weapon(SOWeaponSpecs specs, int level = MinLevel)
    {
        this.specs = specs;
        this.level = Mathf.Clamp(level, MinLevel, specs.MaxLevel);
    }

    /// <summary>The underlying data asset (read for any weapon detail not exposed here).</summary>
    public SOWeaponSpecs Specs => specs;

    public string Name => specs.WeaponName;
    public int MaxLevel => specs.MaxLevel;
    public int Level => level;
    public float FireInterval => specs.FireInterval;
    public AudioClip ShootAudioClip => specs.ShootAudioClip;
    public GameObject BulletPrefab => specs.BulletPrefab;
    public SOMovementParameters BulletMovement => specs.BulletMovement;
    public Vector2 SpawnOffset => specs.SpawnOffset;

    /// <summary>Damage of the NEXT shot, looked up in the weapon's level table.</summary>
    public int CurrentDamage => specs.GetDamageForLevel(level);

    /// <summary>Returns a weapon one level higher (clamped to MaxLevel). Levels are values, so this is a fresh instance at the new level.</summary>
    public Weapon IncreaseLevel()
    {
        return new Weapon(specs, level + 1);
    }

    /// <summary>Returns a weapon reset to level 1.</summary>
    public Weapon ResetLevel()
    {
        return new Weapon(specs, MinLevel);
    }
}
