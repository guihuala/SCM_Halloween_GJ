using UnityEngine;
using UnityEngine.UI;

public class TitleUIController : MonoBehaviour
{
    public Button newGameButton;
    public Button saveButton;
    public Button settingsButton;
    public Button exitButton;

    private void Awake()
    {
        if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGameButtonClicked);
        if (saveButton != null) saveButton.onClick.AddListener(OnSaveButtonClicked);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitButtonClicked);
    }

    public void OnNewGameButtonClicked()
    {
        SavePanel savePanel = UIManager.Instance.OpenPanel("SavePanel", null, UIPanelLayer.Popup) as SavePanel;
        if (savePanel != null)
        {
            savePanel.ConfigureForNewGameSelection(OnNewGameSlotSelected);
        }
    }

    private void OnNewGameSlotSelected(int slotIndex)
    {
        UIManager.Instance.ClosePanel("SavePanel");
        SceneLoader.Instance.LoadScene(GameScene.Gameplay);
    }

    public void OnSaveButtonClicked()
    {
        SavePanel savePanel = UIManager.Instance.OpenPanel("SavePanel") as SavePanel;
        if (savePanel != null)
        {
            savePanel.ConfigureNormal();
        }
    }

    public void OnSettingsButtonClicked()
    {
        UIManager.Instance.OpenPanel("SettingPanel");
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
