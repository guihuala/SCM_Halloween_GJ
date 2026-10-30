using UnityEngine;
using UnityEngine.UI;

public class GameResultPanel : BasePanel
{
    [SerializeField] private Text titleText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    protected override void Awake()
    {
        base.Awake();
        restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
        mainMenuButton.onClick.AddListener(() => GameManager.Instance.ReturnToMainMenu());
    }

    public void Configure(bool won)
    {
        titleText.text = won ? "Delivery Complete" : "Shift Over";
        restartButton.Select();
    }
}
