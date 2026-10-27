using System;
using System.Collections.Generic;
using System.Globalization;

public static class DialogueRuntimeState
{
    private static readonly Dictionary<string, float> Numbers = new Dictionary<string, float>();
    private static readonly HashSet<string> Flags = new HashSet<string>();
    private static readonly HashSet<string> Items = new HashSet<string>();
    private static readonly Dictionary<string, string> Quests = new Dictionary<string, string>();

    public static void Reset()
    {
        Numbers.Clear();
        Flags.Clear();
        Items.Clear();
        Quests.Clear();
    }

    public static DialogueSaveData Capture()
    {
        DialogueSaveData data = new DialogueSaveData();

        foreach (string flag in Flags)
        {
            if (!string.IsNullOrEmpty(flag)) data.flags.Add(flag);
        }

        foreach (string item in Items)
        {
            if (!string.IsNullOrEmpty(item)) data.items.Add(item);
        }

        foreach (KeyValuePair<string, string> quest in Quests)
        {
            if (string.IsNullOrEmpty(quest.Key)) continue;

            data.quests.Add(new DialogueQuestSaveData
            {
                questId = quest.Key,
                state = quest.Value ?? string.Empty
            });
        }

        foreach (KeyValuePair<string, float> number in Numbers)
        {
            if (string.IsNullOrEmpty(number.Key)) continue;

            data.numbers.Add(new DialogueNumberSaveData
            {
                key = number.Key,
                value = number.Value
            });
        }

        data.flags.Sort(StringComparer.Ordinal);
        data.items.Sort(StringComparer.Ordinal);
        data.quests.Sort((left, right) => string.Compare(left.questId, right.questId, StringComparison.Ordinal));
        data.numbers.Sort((left, right) => string.Compare(left.key, right.key, StringComparison.Ordinal));

        return data;
    }

    public static void Restore(DialogueSaveData data)
    {
        Reset();
        if (data == null) return;

        if (data.flags != null)
        {
            for (int i = 0; i < data.flags.Count; i++)
            {
                SetFlag(data.flags[i], true);
            }
        }

        if (data.items != null)
        {
            for (int i = 0; i < data.items.Count; i++)
            {
                SetItem(data.items[i], true);
            }
        }

        if (data.quests != null)
        {
            for (int i = 0; i < data.quests.Count; i++)
            {
                DialogueQuestSaveData quest = data.quests[i];
                if (quest != null) SetQuestState(quest.questId, quest.state);
            }
        }

        if (data.numbers != null)
        {
            for (int i = 0; i < data.numbers.Count; i++)
            {
                DialogueNumberSaveData number = data.numbers[i];
                if (number != null) SetNumber(number.key, number.value);
            }
        }
    }

    public static void SetNumber(string key, float value)
    {
        if (string.IsNullOrEmpty(key)) return;
        Numbers[key] = value;
    }

    public static float GetNumber(string key, float defaultValue = 0f)
    {
        return !string.IsNullOrEmpty(key) && Numbers.TryGetValue(key, out float value) ? value : defaultValue;
    }

    public static void SetFlag(string key, bool value)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (value) Flags.Add(key);
        else Flags.Remove(key);
    }

    public static bool HasFlag(string key)
    {
        return !string.IsNullOrEmpty(key) && Flags.Contains(key);
    }

    public static void SetItem(string key, bool value)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (value) Items.Add(key);
        else Items.Remove(key);
    }

    public static bool HasItem(string key)
    {
        return !string.IsNullOrEmpty(key) && Items.Contains(key);
    }

    public static void SetQuestState(string questId, string state)
    {
        if (string.IsNullOrEmpty(questId)) return;
        Quests[questId] = state ?? string.Empty;
    }

    public static string GetQuestState(string questId)
    {
        return !string.IsNullOrEmpty(questId) && Quests.TryGetValue(questId, out string state) ? state : string.Empty;
    }

    public static bool EvaluateCondition(string condition)
    {
        if (string.IsNullOrWhiteSpace(condition)) return true;

        string[] parts = condition.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string part in parts)
        {
            if (!EvaluateSingleCondition(part.Trim()))
            {
                return false;
            }
        }

        return true;
    }

    public static void ApplyEffects(string effects)
    {
        if (string.IsNullOrWhiteSpace(effects)) return;

        string[] parts = effects.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string part in parts)
        {
            ApplySingleEffect(part.Trim());
        }
    }

    private static bool EvaluateSingleCondition(string condition)
    {
        if (string.IsNullOrWhiteSpace(condition)) return true;

        if (condition.StartsWith("!", StringComparison.Ordinal))
        {
            return !EvaluateSingleCondition(condition.Substring(1));
        }

        if (condition.StartsWith("item:", StringComparison.Ordinal))
        {
            return HasItem(condition.Substring("item:".Length));
        }

        if (condition.StartsWith("flag:", StringComparison.Ordinal))
        {
            return HasFlag(condition.Substring("flag:".Length));
        }

        if (condition.StartsWith("quest:", StringComparison.Ordinal))
        {
            string questExpression = condition.Substring("quest:".Length);
            string[] pieces = questExpression.Split('=');
            return pieces.Length == 2 && GetQuestState(pieces[0]) == pieces[1];
        }

        return EvaluateNumberCondition(condition);
    }

    private static bool EvaluateNumberCondition(string condition)
    {
        string[] operators = { ">=", "<=", "==", "!=", ">", "<" };
        foreach (string op in operators)
        {
            int index = condition.IndexOf(op, StringComparison.Ordinal);
            if (index < 0) continue;

            string key = condition.Substring(0, index).Trim();
            string rawValue = condition.Substring(index + op.Length).Trim();
            float left = GetNumber(key);
            float right = ParseFloat(rawValue);

            switch (op)
            {
                case ">=": return left >= right;
                case "<=": return left <= right;
                case "==": return Math.Abs(left - right) < 0.0001f;
                case "!=": return Math.Abs(left - right) >= 0.0001f;
                case ">": return left > right;
                case "<": return left < right;
            }
        }

        return HasFlag(condition);
    }

    private static void ApplySingleEffect(string effect)
    {
        if (string.IsNullOrWhiteSpace(effect)) return;

        if (effect.StartsWith("set:", StringComparison.Ordinal))
        {
            string[] pieces = effect.Substring("set:".Length).Split('=');
            if (pieces.Length == 2) SetNumber(pieces[0], ParseFloat(pieces[1]));
            return;
        }

        if (effect.StartsWith("add:", StringComparison.Ordinal))
        {
            string[] pieces = effect.Substring("add:".Length).Split('=');
            if (pieces.Length == 2) SetNumber(pieces[0], GetNumber(pieces[0]) + ParseFloat(pieces[1]));
            return;
        }

        if (effect.StartsWith("flag:", StringComparison.Ordinal))
        {
            SetFlag(effect.Substring("flag:".Length), true);
            return;
        }

        if (effect.StartsWith("unflag:", StringComparison.Ordinal))
        {
            SetFlag(effect.Substring("unflag:".Length), false);
            return;
        }

        if (effect.StartsWith("item:", StringComparison.Ordinal))
        {
            SetItem(effect.Substring("item:".Length), true);
            return;
        }

        if (effect.StartsWith("removeItem:", StringComparison.Ordinal))
        {
            SetItem(effect.Substring("removeItem:".Length), false);
            return;
        }

        if (effect.StartsWith("quest:", StringComparison.Ordinal))
        {
            string[] pieces = effect.Substring("quest:".Length).Split('=');
            if (pieces.Length == 2) SetQuestState(pieces[0], pieces[1]);
        }
    }

    private static float ParseFloat(string value)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : 0f;
    }
}
