using System;
using System.Collections.Generic;
using System.Globalization;

public enum SaveValidationSeverity
{
    Info,
    Warning,
    Error
}

[Serializable]
public class SaveValidationIssue
{
    public SaveValidationSeverity severity;
    public string fieldName;
    public string message;

    public SaveValidationIssue(SaveValidationSeverity severity, string fieldName, string message)
    {
        this.severity = severity;
        this.fieldName = fieldName;
        this.message = message;
    }

    public override string ToString()
    {
        return $"[{severity}] {fieldName}: {message}";
    }
}

public static class SaveValidation
{
    public const int CurrentVersion = 1;
    public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    public static List<SaveValidationIssue> Validate(SaveData data, int expectedSlotIndex = -1)
    {
        List<SaveValidationIssue> issues = new List<SaveValidationIssue>();

        if (data == null)
        {
            issues.Add(new SaveValidationIssue(SaveValidationSeverity.Error, "SaveData", "Save data is null."));
            return issues;
        }

        if (data.version <= 0)
        {
            issues.Add(new SaveValidationIssue(SaveValidationSeverity.Error, nameof(data.version), "Version must be greater than 0."));
        }
        else if (data.version > CurrentVersion)
        {
            issues.Add(new SaveValidationIssue(SaveValidationSeverity.Warning, nameof(data.version), $"Save version {data.version} is newer than supported version {CurrentVersion}."));
        }

        if (expectedSlotIndex >= 0 && data.slotIndex != expectedSlotIndex)
        {
            issues.Add(new SaveValidationIssue(SaveValidationSeverity.Warning, nameof(data.slotIndex), $"Slot index is {data.slotIndex}, expected {expectedSlotIndex}."));
        }

        if (data.slotIndex < 0)
        {
            issues.Add(new SaveValidationIssue(SaveValidationSeverity.Warning, nameof(data.slotIndex), "Slot index has not been assigned."));
        }

        if (string.IsNullOrWhiteSpace(data.slotName))
        {
            issues.Add(new SaveValidationIssue(SaveValidationSeverity.Warning, nameof(data.slotName), "Slot name is empty."));
        }

        DateTime createdTime;
        DateTime lastSaveTime;
        bool hasCreatedTime = TryParseSaveTime(data.createdTime, out createdTime);
        bool hasLastSaveTime = TryParseSaveTime(data.lastSaveTime, out lastSaveTime);

        if (!hasCreatedTime)
        {
            issues.Add(new SaveValidationIssue(SaveValidationSeverity.Warning, nameof(data.createdTime), $"Created time should use format {DateTimeFormat}."));
        }

        if (!hasLastSaveTime)
        {
            issues.Add(new SaveValidationIssue(SaveValidationSeverity.Warning, nameof(data.lastSaveTime), $"Last save time should use format {DateTimeFormat}."));
        }

        if (hasCreatedTime && hasLastSaveTime && lastSaveTime < createdTime)
        {
            issues.Add(new SaveValidationIssue(SaveValidationSeverity.Warning, nameof(data.lastSaveTime), "Last save time is earlier than created time."));
        }

        if (data.playTimeSeconds < 0f)
        {
            issues.Add(new SaveValidationIssue(SaveValidationSeverity.Error, nameof(data.playTimeSeconds), "Play time cannot be negative."));
        }

        if (data.coin < 0)
        {
            issues.Add(new SaveValidationIssue(SaveValidationSeverity.Error, nameof(data.coin), "Coin count cannot be negative."));
        }

        if (data.currentLevel < 0)
        {
            issues.Add(new SaveValidationIssue(SaveValidationSeverity.Error, nameof(data.currentLevel), "Current level cannot be negative."));
        }

        if (data.unlockedLevels == null)
        {
            issues.Add(new SaveValidationIssue(SaveValidationSeverity.Warning, nameof(data.unlockedLevels), "Unlocked level list is null."));
        }
        else
        {
            HashSet<string> levels = new HashSet<string>();
            for (int i = 0; i < data.unlockedLevels.Count; i++)
            {
                string level = data.unlockedLevels[i];
                if (string.IsNullOrWhiteSpace(level))
                {
                    issues.Add(new SaveValidationIssue(SaveValidationSeverity.Warning, nameof(data.unlockedLevels), $"Unlocked level at index {i} is empty."));
                    continue;
                }

                if (!levels.Add(level))
                {
                    issues.Add(new SaveValidationIssue(SaveValidationSeverity.Warning, nameof(data.unlockedLevels), $"Duplicate unlocked level: {level}."));
                }
            }
        }

        return issues;
    }

    public static bool HasErrors(List<SaveValidationIssue> issues)
    {
        if (issues == null) return false;

        for (int i = 0; i < issues.Count; i++)
        {
            if (issues[i] != null && issues[i].severity == SaveValidationSeverity.Error)
            {
                return true;
            }
        }

        return false;
    }

    public static string FormatIssues(List<SaveValidationIssue> issues)
    {
        if (issues == null || issues.Count == 0)
        {
            return "No save validation issues.";
        }

        List<string> lines = new List<string>();
        for (int i = 0; i < issues.Count; i++)
        {
            if (issues[i] != null)
            {
                lines.Add(issues[i].ToString());
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static bool TryParseSaveTime(string value, out DateTime time)
    {
        return DateTime.TryParseExact(value, DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
    }
}
