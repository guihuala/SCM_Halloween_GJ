using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
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
        // Menu illustrations already contain their colors; preserve the original white tint.
        foreach(var image in GetComponentsInChildren<Image>(true))
            if(image.sprite!=null)image.color=new Color(1,1,1,image.color.a);
        foreach(var button in GetComponentsInChildren<Button>(true))
        {
            var colors=button.colors;colors.normalColor=colors.selectedColor=colors.highlightedColor=Color.white;button.colors=colors;
        }
        foreach(var animation in GetComponentsInChildren<ButtonHoverAnimation>(true))
        {animation.hoverColor=Color.white;animation.clickColor=new Color(.9f,.9f,.9f,1);}
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
