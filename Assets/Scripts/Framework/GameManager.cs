using UnityEngine;

[DefaultExecutionOrder(-10)]
public class GameManager : Singleton<GameManager>
{
    public enum GameState { Playing, Paused, GameOver }
    public GameState CurrentState { get; private set; }
    private bool leavingScene;

    private void Start() => StartGame();

    private void Update()
    {
        if (leavingScene || (SceneLoader.Instance != null && SceneLoader.Instance.IsLoading())) return;
        if (UIManager.Instance != null && UIManager.Instance.IsPanelOpen("SettingPanel")) return;
        bool pausePressed = InputManager.Instance != null
            ? InputManager.Instance.GetActionDown(InputActionType.Pause)
            : Input.GetKeyDown(KeyCode.Escape);
        if (!pausePressed) return;
        if (CurrentState == GameState.Playing && NightCourier.NightCourierPrototype.Current != null &&
            NightCourier.NightCourierPrototype.Current.ClosePlanner()) return;
        if (CurrentState == GameState.Playing) PauseGame();
        else if (CurrentState == GameState.Paused) ResumeGame();
    }

    public void SetGameState(GameState state)
    {
        if (leavingScene) return;
        CurrentState = state;
        if (TimeManager.Instance != null)
        {
            if (state == GameState.Playing) TimeManager.Instance.ResumeTime();
            else TimeManager.Instance.PauseTime();
        }
        else Time.timeScale = state == GameState.Playing ? 1f : 0f;
        if (InputManager.Instance != null)
            InputManager.Instance.SetGameplayInputEnabled(state == GameState.Playing);
        if (state == GameState.Playing)
        {
            ClosePanelIfOpen("SettingPanel");
            ClosePanelIfOpen("PausePanel");
            ClosePanelIfOpen("GameResultPanel");
        }
        else if (state == GameState.Paused)
            UIManager.Instance.OpenPanel("PausePanel", null, UIPanelLayer.Popup);
        else
        {
            var result = UIManager.Instance.OpenPanel("GameResultPanel", null, UIPanelLayer.Top) as GameResultPanel;
            if (result != null) result.Configure(false);
        }
    }

    public void StartGame() => SetGameState(GameState.Playing);
    public void PauseGame()
    {
        if (CurrentState == GameState.Playing) SetGameState(GameState.Paused);
    }
    public void ResumeGame()
    {
        if (CurrentState == GameState.Paused) SetGameState(GameState.Playing);
    }

    // Default EndGame remains compatible with existing callers.
    public void EndGame() => FinishGame(false);
    public void WinGame() => FinishGame(true);
    public void LoseGame() => FinishGame(false);

    private void FinishGame(bool won)
    {
        if (leavingScene || CurrentState == GameState.GameOver) return;
        ClosePanelIfOpen("SettingPanel");
        ClosePanelIfOpen("PausePanel");
        SetGameState(GameState.GameOver);
        var panel = UIManager.Instance.GetPanel("GameResultPanel") as GameResultPanel;
        if (panel != null) panel.Configure(won);
    }

    public void RestartGame()
    {
        if (!PrepareSceneChange()) return;
        SceneLoader.Instance.ReloadCurrentScene();
    }

    public void ReturnToMainMenu()
    {
        if (!PrepareSceneChange()) return;
        SceneLoader.Instance.LoadScene(GameScene.MainMenu);
    }

    private bool PrepareSceneChange()
    {
        if (leavingScene || SceneLoader.Instance == null || SceneLoader.Instance.IsLoading()) return false;
        leavingScene = true;
        return true;
    }

    private void ClosePanelIfOpen(string name)
    {
        if (UIManager.Instance != null && UIManager.Instance.IsPanelOpen(name))
            UIManager.Instance.ClosePanel(name);
    }
}
