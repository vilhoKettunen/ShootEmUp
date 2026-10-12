using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The main menu view. Wires its buttons to the ApplicationController:
///   - New Game → hide this menu + start a new game session.
///   - Quit     → quit the application.
/// </summary>
public class UiViewMainMenu : UiView
{
    [Header("Main Menu Buttons")]
    public Button ButtonNewGame;
    public Button ButtonQuit;

    protected override void Awake()
    {
        base.Awake();

        ButtonNewGame.onClick.AddListener(HandleNewGameButton);
        ButtonQuit.onClick.AddListener(HandleQuitButton);
    }

    /// <summary>Hides the menu and tells the controller to start a new game.</summary>
    private void HandleNewGameButton()
    {
        Debug.Log("Start New Game");
        Hide();
        ApplicationController.Instance.StartNewGame();
    }

    /// <summary>Tells the controller to quit the application.</summary>
    private void HandleQuitButton()
    {
        Debug.Log("Quit Game");
        ApplicationController.Instance.QuitGame();
    }
}
