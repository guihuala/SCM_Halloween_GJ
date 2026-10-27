using UnityEngine;
using UnityEngine.UI;

public class HudController : MonoBehaviour
{
    public Button pauseButton;

    [Header("Save Test")]
    [SerializeField] private Text coinText;
    [SerializeField] private Text playTimeText;
    [SerializeField] private Text saveStatusText;
    [SerializeField] private Button addCoinButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;

    [Header("Dialogue Test")]
    [SerializeField] private Button dialogueTestButton;
    [SerializeField] private string dialogueTestId = "template_dialogue_test_001";

    private float refreshTimer;
    private string currentStatus = "存档测试就绪";

    private void Awake()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(OnPauseButtonClicked);
        }

        if (addCoinButton != null)
        {
            addCoinButton.onClick.AddListener(OnAddCoinButtonClicked);
        }

        if (saveButton != null)
        {
            saveButton.onClick.AddListener(OnSaveButtonClicked);
        }

        if (loadButton != null)
        {
            loadButton.onClick.AddListener(OnLoadButtonClicked);
        }

        if (dialogueTestButton != null)
        {
            dialogueTestButton.onClick.AddListener(OnDialogueTestButtonClicked);
        }
    }

    private void OnEnable()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveLoaded += OnSaveLoaded;
            SaveManager.Instance.SaveCompleted += OnSaveCompleted;
            SaveManager.Instance.SaveFailed += OnSaveFailed;
        }

        RefreshSaveTestUI("存档测试就绪");
    }

    private void OnDisable()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveLoaded -= OnSaveLoaded;
            SaveManager.Instance.SaveCompleted -= OnSaveCompleted;
            SaveManager.Instance.SaveFailed -= OnSaveFailed;
        }
    }

    private void Update()
    {
        SaveData currentData = GetCurrentData();
        if (currentData == null) return;

        if (Time.timeScale > 0f)
        {
            currentData.playTimeSeconds += Time.deltaTime;
        }

        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer >= 0.25f)
        {
            refreshTimer = 0f;
            RefreshSaveTestUI();
        }
    }

    private void OnPauseButtonClicked()
    {
        GameManager.Instance.PauseGame();
    }

    private void OnAddCoinButtonClicked()
    {
        SaveData currentData = GetCurrentData();
        if (currentData == null)
        {
            RefreshSaveTestUI("没有找到 SaveManager");
            return;
        }

        currentData.coin += 10;
        RefreshSaveTestUI("金币 +10，尚未保存");
    }

    private void OnSaveButtonClicked()
    {
        if (SaveManager.Instance == null)
        {
            RefreshSaveTestUI("没有找到 SaveManager");
            return;
        }

        bool saved = SaveManager.Instance.SaveGame();
        if (!saved)
        {
            RefreshSaveTestUI("保存失败");
        }
    }

    private void OnLoadButtonClicked()
    {
        if (SaveManager.Instance == null)
        {
            RefreshSaveTestUI("没有找到 SaveManager");
            return;
        }

        bool loaded = SaveManager.Instance.LoadGame();
        RefreshSaveTestUI(loaded ? "读取当前存档成功" : "当前槽位没有存档，已创建新数据");
    }

    private void OnDialogueTestButtonClicked()
    {
        if (DialogueManager.Instance == null)
        {
            RefreshSaveTestUI("没有找到 DialogueManager");
            return;
        }

        DialogueVariableResolver.Set("playerName", "桂花");
        if (DialogueRuntimeState.GetNumber("charm") < 5f)
        {
            DialogueRuntimeState.SetNumber("charm", 6f);
        }

        if (string.IsNullOrWhiteSpace(dialogueTestId))
        {
            RefreshSaveTestUI("没有配置测试对话 ID");
            return;
        }

        if (!DialogueManager.Instance.PlayById(dialogueTestId.Trim()))
        {
            RefreshSaveTestUI($"没有找到对话 ID：{dialogueTestId}");
            return;
        }

        RefreshSaveTestUI("打开测试对话，选择分支后保存再读取");
    }

    private SaveData GetCurrentData()
    {
        if (SaveManager.Instance == null) return null;

        if (SaveManager.Instance.CurrentData == null)
        {
            SaveManager.Instance.NewGame();
        }

        return SaveManager.Instance.CurrentData;
    }

    private void OnSaveLoaded(SaveData data)
    {
        RefreshSaveTestUI("读取当前存档成功");
    }

    private void OnSaveCompleted(SaveData data)
    {
        RefreshSaveTestUI("保存当前槽位成功");
    }

    private void OnSaveFailed(System.Exception exception)
    {
        RefreshSaveTestUI("存档操作失败");
    }

    private void RefreshSaveTestUI(string status = null)
    {
        if (!string.IsNullOrEmpty(status))
        {
            currentStatus = status;
        }

        SaveData currentData = SaveManager.Instance != null ? SaveManager.Instance.CurrentData : null;

        if (coinText != null)
        {
            coinText.text = currentData != null ? $"金币：{currentData.coin}" : "金币：--";
        }

        if (playTimeText != null)
        {
            playTimeText.text = currentData != null ? $"游玩时间：{FormatTime(currentData.playTimeSeconds)}" : "游玩时间：--:--";
        }

        if (saveStatusText != null)
        {
            int slotNumber = SaveManager.Instance != null ? SaveManager.Instance.CurrentSlotIndex + 1 : 0;
            string statusLine = slotNumber > 0 ? $"槽位 {slotNumber}：{currentStatus}" : currentStatus;
            saveStatusText.text = $"{statusLine}\n{GetDialogueStateSummary()}";
        }
    }

    private string GetDialogueStateSummary()
    {
        string questState = DialogueRuntimeState.GetQuestState("necklace");
        if (string.IsNullOrEmpty(questState))
        {
            questState = "未接取";
        }

        float favor = DialogueRuntimeState.GetNumber("favor.oldman");
        float charm = DialogueRuntimeState.GetNumber("charm");
        string necklace = DialogueRuntimeState.HasItem("necklace") ? "有" : "无";
        return $"对话状态：项链任务={questState}，项链={necklace}，村长好感={favor:0.#}，魅力={charm:0.#}";
    }

    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;
        return $"{minutes:00}:{remainingSeconds:00}";
    }
}
