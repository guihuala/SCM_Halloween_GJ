using UnityEngine;
using UnityEngine.UI;

public class TitleUIController : MonoBehaviour
{
    public Button newGameButton;
    public Button settingsButton;
    public Button exitButton;
    public Button creditsButton;
    private bool enteringGame;

    private void OnEnable() => enteringGame = false;

    private void Awake()
    {
        NightCourier.CourierTheme.Apply(gameObject);
        if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGameButtonClicked);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        if (creditsButton != null) creditsButton.onClick.AddListener(OnCreditsButtonClicked);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitButtonClicked);
    }

    public void OnNewGameButtonClicked()
    {
        if (enteringGame || SceneLoader.Instance == null || SceneLoader.Instance.IsLoading()) return;
        enteringGame = true;
        var motion = GetComponent<MainMenuMotion>();
        if (motion != null && motion.isActiveAndEnabled)
            motion.BeginExit(() => SceneLoader.Instance.LoadScene(GameScene.Gameplay));
        else
            SceneLoader.Instance.LoadScene(GameScene.Gameplay);
    }

    public void OnSettingsButtonClicked() => UIManager.Instance.OpenPanel("SettingPanel");

    public void OnCreditsButtonClicked()
    {
        if (UIManager.Instance.IsPanelOpen("CreditPanel")) return;
        if (creditsButton != null) creditsButton.Select();
        UIManager.Instance.OpenPanel("CreditPanel", null, UIPanelLayer.Popup);
    }

    public void OnExitButtonClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
