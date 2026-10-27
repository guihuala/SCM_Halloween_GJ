using UnityEngine;
using UnityEngine.UI;

public class PausePanel : BasePanel
{
    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;

    private void Start()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    private void OnResumeClicked()
    {
        GameManager.Instance.ResumeGame();
    }

    private void OnSettingsClicked()
    {
        UIManager.Instance.OpenPanel("SettingPanel", panelName, UIPanelLayer.Popup);
    }

    private void OnMainMenuClicked()
    {
        GameManager.Instance.ReturnToMainMenu();
    }
}
