using UnityEngine;
using UnityEngine.UI;

public class HudController : MonoBehaviour
{
    public Button pauseButton;

    private void Awake()
    {
        if (pauseButton != null) pauseButton.onClick.AddListener(Pause);
    }

    private void Pause()
    {
        if (GameManager.Instance != null) GameManager.Instance.PauseGame();
    }
}
