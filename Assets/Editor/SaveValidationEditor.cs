using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SaveValidationEditor
{
    private const string SaveDirectoryName = "Saves";
    private const string SaveFileSearchPattern = "save_slot_*.json";

    [MenuItem("Tools/Template/Save/Validate Current Save Data")]
    public static void ValidateCurrentSaveData()
    {
        if (!Application.isPlaying || SaveManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Save Validate", "Enter Play Mode before validating current save data.", "OK");
            return;
        }

        SaveData data = SaveManager.Instance.CurrentData;
        List<SaveValidationIssue> issues = SaveValidation.Validate(data, SaveManager.Instance.CurrentSlotIndex);
        string message = SaveValidation.FormatIssues(issues);

        if (issues.Count == 0)
        {
            Debug.Log("Current save data is valid.");
            EditorUtility.DisplayDialog("Save Validate", "Current save data is valid.", "OK");
            return;
        }

        Debug.LogWarning("Current save data validation result:\n" + message);
        EditorUtility.DisplayDialog("Save Validate", $"Found {issues.Count} issue(s). Check Console for details.", "OK");
    }

    [MenuItem("Tools/Template/Save/Validate Local Save Files")]
    public static void ValidateLocalSaveFiles()
    {
        string saveDirectory = Path.Combine(Application.persistentDataPath, SaveDirectoryName);
        if (!Directory.Exists(saveDirectory))
        {
            EditorUtility.DisplayDialog("Save Validate", $"Save directory does not exist:\n{saveDirectory}", "OK");
            Debug.Log($"Save directory does not exist: {saveDirectory}");
            return;
        }

        string[] saveFiles = Directory.GetFiles(saveDirectory, SaveFileSearchPattern, SearchOption.TopDirectoryOnly);
        Array.Sort(saveFiles, StringComparer.OrdinalIgnoreCase);

        if (saveFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("Save Validate", $"No save files found in:\n{saveDirectory}", "OK");
            Debug.Log($"No save files found in: {saveDirectory}");
            return;
        }

        int checkedCount = 0;
        int issueCount = 0;
        List<string> summaries = new List<string>();

        foreach (string saveFile in saveFiles)
        {
            checkedCount++;
            int expectedSlot = ParseSlotIndex(saveFile);

            try
            {
                string json = File.ReadAllText(saveFile);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                List<SaveValidationIssue> issues = SaveValidation.Validate(data, expectedSlot);

                if (issues.Count == 0)
                {
                    Debug.Log($"Save file valid: {saveFile}");
                    continue;
                }

                issueCount += issues.Count;
                string summary = $"{Path.GetFileName(saveFile)}: {issues.Count} issue(s)";
                summaries.Add(summary);
                Debug.LogWarning($"{summary}\n{SaveValidation.FormatIssues(issues)}\nPath: {saveFile}");
            }
            catch (Exception exception)
            {
                issueCount++;
                summaries.Add($"{Path.GetFileName(saveFile)}: unreadable JSON");
                Debug.LogError($"Save file cannot be read: {saveFile}\n{exception}");
            }
        }

        if (issueCount == 0)
        {
            EditorUtility.DisplayDialog("Save Validate", $"Checked {checkedCount} save file(s). All valid.", "OK");
            return;
        }

        Debug.LogWarning("Save validation summary:\n" + string.Join("\n", summaries));
        EditorUtility.DisplayDialog("Save Validate", $"Checked {checkedCount} save file(s), found {issueCount} issue(s). Check Console for details.", "OK");
    }

    private static int ParseSlotIndex(string saveFile)
    {
        string fileName = Path.GetFileNameWithoutExtension(saveFile);
        const string prefix = "save_slot_";

        if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return -1;
        }

        string numberText = fileName.Substring(prefix.Length);
        int slotNumber;
        if (!int.TryParse(numberText, out slotNumber))
        {
            return -1;
        }

        return Mathf.Max(0, slotNumber - 1);
    }
}
