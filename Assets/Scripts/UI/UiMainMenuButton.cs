using UnityEngine;

/// <summary>
/// Tag script for main menu buttons. Identifies the button's purpose so
/// the view or controller can route the click to the right action
/// (NewGame, Options, Credits, Quit).
/// </summary>
public class UiMainMenuButton : MonoBehaviour
{
    public enum MainMenuButtonType { NewGame, Options, Credits, Quit }
    public MainMenuButtonType ButtonType;
}
