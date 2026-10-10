using UnityEngine;
using TMPro;

/// <summary>
/// In-game HUD view (the test-phase HUD): shows the current weapon name, the
/// player's weapon LEVEL, and the TestDummy's HP so damage values are visible
/// while testing (Q-T3-4).
///
/// It plugs into the template's UiView system: a Canvas + Text GameObject with
/// this script, registered in the UI prefab's UiViewsList (LayerHUD), default
/// state VISIBLE. Game code pushes strings in (UpdateWeapon/UpdateLevel/
/// UpdateHealth) — the HUD never polls game objects.
/// </summary>
public class UiViewHud : UiView
{
    [SerializeField] private TextMeshProUGUI weaponText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI healthText;

    private static UiViewHud instance;

    /// <summary>Safe accessor — null when the HUD view isn't in the scene (e.g. main menu).</summary>
    public static UiViewHud Instance => instance;

    protected override void Awake()
    {
        base.Awake();
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>Push the current weapon name into the HUD.</summary>
    public void UpdateWeapon(string weaponName)
    {
        if (weaponText != null)
        {
            weaponText.text = "Weapon: " + weaponName;
        }
    }

    /// <summary>Push the player's weapon level into the HUD.</summary>
    public void UpdateLevel(int level)
    {
        if (levelText != null)
        {
            levelText.text = "Level: " + level;
        }
    }

    /// <summary>Push the TestDummy's HP into the HUD (test dummy is immortal).</summary>
    public void UpdateHealth(int health)
    {
        if (healthText != null)
        {
            healthText.text = "Dummy HP: " + health;
        }
    }
}
