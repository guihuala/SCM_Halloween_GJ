using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CreditPanel : BasePanel
{
    [SerializeField] private Text creditsText;
    [SerializeField] private TextAsset audioCredits;
    [SerializeField] private Button backButton;
    private GameObject previousSelection;

    protected override void Awake()
    {
        base.Awake();
        if (creditsText != null && audioCredits != null)
            creditsText.text += "\n\nAUDIO CREDITS\n" + audioCredits.text;
        backButton.onClick.AddListener(Back);
        // Keep directional navigation inside this single-button modal.
        backButton.navigation = new Navigation { mode = Navigation.Mode.None };
    }

    public override void OpenPanel(string name)
    {
        previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        base.OpenPanel(name);
        backButton.Select();
    }

    private void Update()
    {
        if (!hasRemoved && Input.GetKeyDown(KeyCode.Escape)) Back();
    }

    public void Back()
    {
        if (hasRemoved) return;
        UIManager.Instance.ClosePanel(panelName);
        if (EventSystem.current != null && previousSelection != null && previousSelection.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(previousSelection);
    }
}
