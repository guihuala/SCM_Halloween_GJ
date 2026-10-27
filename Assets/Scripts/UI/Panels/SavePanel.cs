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
                "开始新游戏",
                string.Format("存档位 {0} 已有数据。要覆盖并开始新游戏吗？", _selectedSlotIndex + 1),
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
            ? "存档已读取。"
            : GetLoadRecoveryMessage(meta);
        RefreshStatus(message);
    }

    private void OnSaveClicked()
    {
        if (!HasSaveManager()) return;

        bool saved = SaveManager.Instance.SaveGame(_selectedSlotIndex);
        RefreshStatus(saved ? "存档已完成。" : "存档失败。");
    }

    private void OnDeleteClicked()
    {
        if (!HasSaveManager()) return;

        SaveSlotMeta meta = SaveManager.Instance.GetSlotMeta(_selectedSlotIndex);
        if (!meta.hasData)
        {
            RefreshStatus("未找到存档文件，已创建新数据。");
            return;
        }

        ShowConfirm(
            "删除存档",
            string.Format("要删除存档位 {0} 的数据吗？", _selectedSlotIndex + 1),
            DeleteSelectedSlot);
    }

    private void DeleteSelectedSlot()
    {
        bool deleted = SaveManager.Instance.DeleteSave(_selectedSlotIndex);
        RefreshStatus(deleted ? "存档已删除。" : "删除失败。");
    }

    private void OnCloseClicked()
    {
        UIManager.Instance.ClosePanel(panelName);
    }

    private bool HasSaveManager()
    {
        if (SaveManager.Instance != null) return true;

        RefreshStatus("场景中未找到 SaveManager。");
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
                ? "选择新游戏存档"
                : "存档";
        }

        if (SaveManager.Instance == null)
        {
            SetText(statusText, message ?? "场景中未找到 SaveManager。");
            SetText(selectedSlotText, "已选：不可用");
            SetText(metadataText, "无元数据");
            return;
        }

        RefreshSlotList();
        RefreshActionButtons();

        SaveSlotMeta meta = SaveManager.Instance.GetSlotMeta(_selectedSlotIndex);
        string saveState = GetSlotStateText(meta);
        string detail = meta.hasData && meta.isValid
            ? string.Format("名称：{0}\n场景：{1}\n保存时间：{2}\n创建时间：{3}\n游玩时长：{4}",
                meta.slotName,
                SafeText(meta.sceneName),
                SafeText(meta.lastSaveTime),
                SafeText(meta.createdTime),
                FormatPlayTime(meta.playTimeSeconds))
            : meta.hasData
                ? "该存档文件已损坏或未通过验证。读取时会创建新数据。"
            : "该存档位没有数据。";

        SetText(statusText, string.IsNullOrEmpty(message) ? saveState : $"{message}\n{saveState}");
        SetText(selectedSlotText, string.Format("已选存档位：{0}", _selectedSlotIndex + 1));
        SetText(metadataText, detail);
    }

    private void RefreshSlotList()
    {
        if (slotButtons == null) return;

        for (int i = 0; i < slotButtons.Length; i++)
        {
            SaveSlotMeta meta = SaveManager.Instance.GetSlotMeta(i);
            SetText(GetText(slotTitleTexts, i), string.Format("存档位 {0}", i + 1));
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

        RefreshStatus("已创建新存档数据。");
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
            return "存档文件无效，已创建新数据。";
        }

        return "未找到存档文件，已创建新数据。";
    }

    private string GetSlotStateText(SaveSlotMeta meta)
    {
        if (meta == null || !meta.hasData)
        {
            return "空存档位";
        }

        return meta.isValid
            ? "存档文件存在"
            : "存档文件无效";
    }

    private string GetSlotListMetaText(SaveSlotMeta meta)
    {
        if (meta == null || !meta.hasData)
        {
            return "空";
        }

        return meta.isValid
            ? $"{SafeText(meta.lastSaveTime)}  {SafeText(meta.sceneName)}"
            : "无效";
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
