using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : SingletonPersistent<SaveManager>
{
    [Header("Save File")]
    [SerializeField] private string saveDirectoryName = "Saves";
    [SerializeField] private string saveFilePrefix = "save_slot_";
    [SerializeField] private int slotCount = 4;
    [SerializeField] private int currentSlotIndex = 0;
    [SerializeField] private bool loadOnAwake;

    public SaveData CurrentData { get; private set; }
    public int CurrentSlotIndex => currentSlotIndex;
    public int SlotCount => slotCount;
    public string SaveDirectory => Path.Combine(Application.persistentDataPath, saveDirectoryName);
    public string SavePath => GetSlotPath(currentSlotIndex);

    public event Action<SaveData> SaveLoaded;
    public event Action<SaveData> SaveCompleted;
    public event Action<int> SaveDeleted;
    public event Action<Exception> SaveFailed;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this) return;

        if (loadOnAwake)
        {
            LoadGame(currentSlotIndex);
        }
        else if (CurrentData == null)
        {
            CurrentData = CreateNewSaveData();
        }
    }

    public SaveData NewGame()
    {
        return NewGame(currentSlotIndex);
    }

    public SaveData NewGame(int slotIndex)
    {
        DialogueRuntimeState.Reset();
        CurrentData = CreateNewSaveData();
        SetCurrentSlot(slotIndex);
        ApplyMetadata(CurrentData, slotIndex);
        return CurrentData;
    }

    public bool LoadGame()
    {
        return LoadGame(currentSlotIndex);
    }

    public bool LoadGame(int slotIndex)
    {
        SetCurrentSlot(slotIndex);

        if (!HasSave(slotIndex))
        {
            CurrentData = CreateNewSaveData();
            ApplyMetadata(CurrentData, slotIndex);
            DialogueRuntimeState.Reset();
            return false;
        }

        try
        {
            string json = File.ReadAllText(GetSlotPath(slotIndex));
            CurrentData = JsonUtility.FromJson<SaveData>(json);

            if (CurrentData == null)
            {
                CurrentData = CreateNewSaveData();
                ApplyMetadata(CurrentData, slotIndex);
                DialogueRuntimeState.Reset();
                return false;
            }

            List<SaveValidationIssue> issues = SaveValidation.Validate(CurrentData, slotIndex);
            if (SaveValidation.HasErrors(issues))
            {
                Debug.LogWarning($"Save validation failed for slot {slotIndex + 1}:\n{SaveValidation.FormatIssues(issues)}");
                CurrentData = CreateNewSaveData();
                ApplyMetadata(CurrentData, slotIndex);
                DialogueRuntimeState.Reset();
                return false;
            }

            LogValidationWarnings(issues, slotIndex);
            ApplyMetadata(CurrentData, slotIndex, false);
            RestoreRuntimeState(CurrentData);
            SaveLoaded?.Invoke(CurrentData);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Load save failed: {exception.Message}");
            CurrentData = CreateNewSaveData();
            ApplyMetadata(CurrentData, slotIndex);
            DialogueRuntimeState.Reset();
            SaveFailed?.Invoke(exception);
            return false;
        }
    }

    public bool SaveGame()
    {
        return SaveGame(currentSlotIndex);
    }

    public bool SaveGame(int slotIndex)
    {
        SetCurrentSlot(slotIndex);

        if (CurrentData == null)
        {
            CurrentData = CreateNewSaveData();
        }

        try
        {
            CaptureRuntimeState(CurrentData);
            ApplyMetadata(CurrentData, slotIndex);
            List<SaveValidationIssue> issues = SaveValidation.Validate(CurrentData, slotIndex);
            if (SaveValidation.HasErrors(issues))
            {
                Debug.LogWarning($"Save validation failed before writing slot {slotIndex + 1}:\n{SaveValidation.FormatIssues(issues)}");
                SaveFailed?.Invoke(new InvalidDataException("Save data validation failed."));
                return false;
            }

            LogValidationWarnings(issues, slotIndex);

            string slotPath = GetSlotPath(slotIndex);
            string directory = Path.GetDirectoryName(slotPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonUtility.ToJson(CurrentData, true);
            File.WriteAllText(slotPath, json);

            SaveCompleted?.Invoke(CurrentData);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Save game failed: {exception.Message}");
            SaveFailed?.Invoke(exception);
            return false;
        }
    }

    public bool DeleteSave()
    {
        return DeleteSave(currentSlotIndex);
    }

    public bool DeleteSave(int slotIndex)
    {
        try
        {
            string slotPath = GetSlotPath(slotIndex);
            if (File.Exists(slotPath))
            {
                File.Delete(slotPath);
            }

            if (slotIndex == currentSlotIndex)
            {
                CurrentData = CreateNewSaveData();
                ApplyMetadata(CurrentData, slotIndex);
                DialogueRuntimeState.Reset();
            }

            SaveDeleted?.Invoke(slotIndex);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Delete save failed: {exception.Message}");
            SaveFailed?.Invoke(exception);
            return false;
        }
    }

    public bool HasSave()
    {
        return HasSave(currentSlotIndex);
    }

    public bool HasSave(int slotIndex)
    {
        return File.Exists(GetSlotPath(slotIndex));
    }

    public void SetCurrentData(SaveData data)
    {
        CurrentData = data ?? CreateNewSaveData();
        ApplyMetadata(CurrentData, currentSlotIndex, false);
        RestoreRuntimeState(CurrentData);
    }

    public void SetCurrentSlot(int slotIndex)
    {
        currentSlotIndex = Mathf.Clamp(slotIndex, 0, Mathf.Max(0, slotCount - 1));
    }

    public SaveSlotMeta GetSlotMeta(int slotIndex)
    {
        SaveSlotMeta meta = new SaveSlotMeta
        {
            slotIndex = slotIndex,
            slotName = $"Slot {slotIndex + 1}",
            hasData = HasSave(slotIndex),
            isValid = true
        };

        if (!meta.hasData)
        {
            return meta;
        }

        try
        {
            string json = File.ReadAllText(GetSlotPath(slotIndex));
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                meta.isValid = false;
                meta.validationSummary = SaveValidation.FormatIssues(SaveValidation.Validate(null, slotIndex));
                return meta;
            }

            List<SaveValidationIssue> issues = SaveValidation.Validate(data, slotIndex);
            if (SaveValidation.HasErrors(issues))
            {
                meta.isValid = false;
                meta.validationSummary = SaveValidation.FormatIssues(issues);
                return meta;
            }

            meta.slotName = string.IsNullOrEmpty(data.slotName) ? $"Slot {slotIndex + 1}" : data.slotName;
            meta.lastSaveTime = data.lastSaveTime;
            meta.createdTime = data.createdTime;
            meta.sceneName = data.sceneName;
            meta.playTimeSeconds = data.playTimeSeconds;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Read save metadata failed: {exception.Message}");
            meta.isValid = false;
            meta.validationSummary = exception.Message;
        }

        return meta;
    }

    public List<SaveSlotMeta> GetAllSlotMetas()
    {
        List<SaveSlotMeta> metas = new List<SaveSlotMeta>();
        for (int i = 0; i < slotCount; i++)
        {
            metas.Add(GetSlotMeta(i));
        }

        return metas;
    }

    public string GetSlotPath(int slotIndex)
    {
        int safeSlotIndex = Mathf.Clamp(slotIndex, 0, Mathf.Max(0, slotCount - 1));
        return Path.Combine(SaveDirectory, $"{saveFilePrefix}{safeSlotIndex + 1:00}.json");
    }

    private SaveData CreateNewSaveData()
    {
        return new SaveData();
    }

    private void CaptureRuntimeState(SaveData data)
    {
        if (data == null) return;
        data.dialogue = DialogueRuntimeState.Capture();
    }

    private void RestoreRuntimeState(SaveData data)
    {
        DialogueRuntimeState.Restore(data != null ? data.dialogue : null);
    }

    private void ApplyMetadata(SaveData data, int slotIndex, bool refreshSaveTime = true)
    {
        if (data == null) return;

        data.slotIndex = slotIndex;
        if (string.IsNullOrEmpty(data.slotName) || data.slotName == "New Save")
        {
            data.slotName = $"Slot {slotIndex + 1}";
        }

        if (string.IsNullOrEmpty(data.createdTime))
        {
            data.createdTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        if (refreshSaveTime)
        {
            data.lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            data.sceneName = SceneManager.GetActiveScene().name;
        }
    }

    private void LogValidationWarnings(List<SaveValidationIssue> issues, int slotIndex)
    {
        if (issues == null || issues.Count == 0) return;

        Debug.LogWarning($"Save validation warnings for slot {slotIndex + 1}:\n{SaveValidation.FormatIssues(issues)}");
    }
}
