using UnityEngine;
using UnityEngine.UI;
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

    private void HandleNewGameButton()
    {
        Debug.Log("Start New Game");
        Hide();
        ApplicationController.Instance.StartNewGame();
    }

    private void HandleQuitButton()
    {
        Debug.Log("Quit Game");
        ApplicationController.Instance.QuitGame();
    }
}
