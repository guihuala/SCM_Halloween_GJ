using System;
using UnityEngine;
using UnityEngine.UI;

public enum SavePanelMode
{
    Normal,
    NewGameSelection
}

public class SavePanel : BasePanel
{
    [Header("Text")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text statusText;
    [SerializeField] private Text selectedSlotText;
    [SerializeField] private Text metadataText;

    [Header("Slot List")]
    [SerializeField] private Button[] slotButtons;
    [SerializeField] private Text[] slotTitleTexts;
    [SerializeField] private Text[] slotMetaTexts;

    [Header("Actions")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button closeButton;

    private int _selectedSlotIndex;
    private SavePanelMode _mode = SavePanelMode.Normal;
    private Action<int> _onNewGameSelected;

    private void Start()
    {
        InitSlotButtons();

        if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGameClicked);
        if (loadButton != null) loadButton.onClick.AddListener(OnLoadClicked);
        if (saveButton != null) saveButton.onClick.AddListener(OnSaveClicked);
        if (deleteButton != null) deleteButton.onClick.AddListener(OnDeleteClicked);
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);

        RefreshStatus();
    }

    public override void OpenPanel(string name)
    {
        base.OpenPanel(name);
        RefreshStatus();
    }

    public void ConfigureForNewGameSelection(Action<int> onNewGameSelected)
    {
        _mode = SavePanelMode.NewGameSelection;
        _onNewGameSelected = onNewGameSelected;
        RefreshStatus();
    }

    public void ConfigureNormal()
    {
        _mode = SavePanelMode.Normal;
        _onNewGameSelected = null;
        RefreshStatus();
    }

    private void OnNewGameClicked()
    {
        if (!HasSaveManager()) return;

        SaveSlotMeta meta = SaveManager.Instance.GetSlotMeta(_selectedSlotIndex);
        if (meta.hasData)
        {
            ShowConfirm(
                "New Game",
                string.Format("Slot {0} has a save. Overwrite it and start a new game?", _selectedSlotIndex + 1),
                StartNewGameInSelectedSlot);
            return;
        }

        StartNewGameInSelectedSlot();
    }

    private void OnLoadClicked()
    {
        if (!HasSaveManager()) return;

        SaveSlotMeta meta = SaveManager.Instance.GetSlotMeta(_selectedSlotIndex);
        bool loaded = SaveManager.Instance.LoadGame(_selectedSlotIndex);
        string message = loaded
            ? "Save loaded."
            : GetLoadRecoveryMessage(meta);
        RefreshStatus(message);
    }

    private void OnSaveClicked()
    {
        if (!HasSaveManager()) return;

        bool saved = SaveManager.Instance.SaveGame(_selectedSlotIndex);
        RefreshStatus(saved ? "Game saved." : "Save failed.");
    }

    private void OnDeleteClicked()
    {
        if (!HasSaveManager()) return;

        SaveSlotMeta meta = SaveManager.Instance.GetSlotMeta(_selectedSlotIndex);
        if (!meta.hasData)
        {
            RefreshStatus("No save found. New data created.");
            return;
        }

        ShowConfirm(
            "Delete Save",
            string.Format("Delete the save in slot {0}?", _selectedSlotIndex + 1),
            DeleteSelectedSlot);
    }

    private void DeleteSelectedSlot()
    {
        bool deleted = SaveManager.Instance.DeleteSave(_selectedSlotIndex);
        RefreshStatus(deleted ? "Save deleted." : "Delete failed.");
    }

    private void OnCloseClicked()
    {
        UIManager.Instance.ClosePanel(panelName);
    }

    private bool HasSaveManager()
    {
        if (SaveManager.Instance != null) return true;

        RefreshStatus("Save manager is unavailable.");
        return false;
    }

    private void InitSlotButtons()
    {
        if (slotButtons == null) return;

        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slotIndex = i;
            if (slotButtons[i] != null)
            {
                slotButtons[i].onClick.AddListener(() => SelectSlot(slotIndex));
            }
        }
    }

    private void SelectSlot(int slotIndex)
    {
        _selectedSlotIndex = slotIndex;
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SetCurrentSlot(slotIndex);
        }

        RefreshStatus();
    }

    private void RefreshStatus(string message = null)
    {
        if (titleText != null)
        {
            titleText.text = _mode == SavePanelMode.NewGameSelection
                ? "Choose a Save Slot"
                : "Save Game";
        }

        if (SaveManager.Instance == null)
        {
            SetText(statusText, message ?? "Save manager is unavailable.");
            SetText(selectedSlotText, "Selection unavailable");
            SetText(metadataText, "No metadata");
            return;
        }

        RefreshSlotList();
        RefreshActionButtons();

        SaveSlotMeta meta = SaveManager.Instance.GetSlotMeta(_selectedSlotIndex);
        string saveState = GetSlotStateText(meta);
        string detail = meta.hasData && meta.isValid
            ? string.Format("Name: {0}\nScene: {1}\nSaved: {2}\nCreated: {3}\nPlay time: {4}",
                meta.slotName,
                SafeText(meta.sceneName),
                SafeText(meta.lastSaveTime),
                SafeText(meta.createdTime),
                FormatPlayTime(meta.playTimeSeconds))
            : meta.hasData
                ? "This save is invalid. Loading will create new data."
            : "This slot is empty.";

        SetText(statusText, string.IsNullOrEmpty(message) ? saveState : $"{message}\n{saveState}");
        SetText(selectedSlotText, string.Format("Selected slot: {0}", _selectedSlotIndex + 1));
        SetText(metadataText, detail);
    }

    private void RefreshSlotList()
    {
        if (slotButtons == null) return;

        for (int i = 0; i < slotButtons.Length; i++)
        {
            SaveSlotMeta meta = SaveManager.Instance.GetSlotMeta(i);
            SetText(GetText(slotTitleTexts, i), string.Format("Slot {0}", i + 1));
            SetText(GetText(slotMetaTexts, i), GetSlotListMetaText(meta));

            if (slotButtons[i] != null)
            {
                slotButtons[i].interactable = i != _selectedSlotIndex;
            }
        }
    }

    private void RefreshActionButtons()
    {
        SetActive(loadButton, _mode == SavePanelMode.Normal);
        SetActive(saveButton, _mode == SavePanelMode.Normal);
        SetActive(deleteButton, _mode == SavePanelMode.Normal);
    }

    private Text GetText(Text[] texts, int index)
    {
        if (texts == null || index < 0 || index >= texts.Length) return null;
        return texts[index];
    }

    private void SetText(Text targetText, string content)
    {
        if (targetText != null)
        {
            targetText.text = content;
        }
    }

    private void SetActive(Button button, bool active)
    {
        if (button != null)
        {
            button.gameObject.SetActive(active);
        }
    }

    private void StartNewGameInSelectedSlot()
    {
        SaveManager.Instance.NewGame(_selectedSlotIndex);

        if (_mode == SavePanelMode.NewGameSelection)
        {
            Action<int> callback = _onNewGameSelected;
            callback?.Invoke(_selectedSlotIndex);
            return;
        }

        RefreshStatus("New save data created.");
    }

    private void ShowConfirm(string title, string message, Action onConfirm)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenConfirm(title, message, onConfirm);
        }
        else
        {
            onConfirm?.Invoke();
        }
    }

    private string GetLoadRecoveryMessage(SaveSlotMeta meta)
    {
        if (meta != null && meta.hasData && !meta.isValid)
        {
            return "Invalid save. New data created.";
        }

        return "No save found. New data created.";
    }

    private string GetSlotStateText(SaveSlotMeta meta)
    {
        if (meta == null || !meta.hasData)
        {
            return "Empty Slot";
        }

        return meta.isValid
            ? "Save exists"
            : "Invalid save";
    }

    private string GetSlotListMetaText(SaveSlotMeta meta)
    {
        if (meta == null || !meta.hasData)
        {
            return "Empty";
        }

        return meta.isValid
            ? $"{SafeText(meta.lastSaveTime)}  {SafeText(meta.sceneName)}"
            : "Invalid";
    }

    private string SafeText(string content)
    {
        return string.IsNullOrEmpty(content) ? "-" : content;
    }

    private string FormatPlayTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int minutes = totalSeconds / 60;
        int remainSeconds = totalSeconds % 60;
        return $"{minutes:00}:{remainSeconds:00}";
    }
}
