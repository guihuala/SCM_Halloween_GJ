using System;
using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : SingletonPersistent<DialogueManager>
{
    [SerializeField] private string dialoguePanelName = "DialoguePanel";
    [SerializeField] private UIPanelLayer panelLayer = UIPanelLayer.Top;
    [SerializeField] private string dialogueTableResourcePath = "ConfigTables/DialogueTable";

    public bool IsPlaying { get; private set; }
    public event Action DialogueStarted;
    public event Action DialogueFinished;

    public DialoguePanel CurrentPanel { get; private set; }

    private DialogueTable _dialogueTable;

    public void Play(DialogueSequence sequence)
    {
        if (sequence == null)
        {
            Debug.LogWarning("Dialogue sequence is null.");
            return;
        }

        Play(sequence.lines);
    }

    public void Play(DialogueSequenceAsset sequenceAsset)
    {
        if (sequenceAsset == null)
        {
            Debug.LogWarning("Dialogue sequence asset is null.");
            return;
        }

        Play(sequenceAsset.Lines);
    }

    public void Play(IList<DialogueLine> lines)
    {
        if (lines == null || lines.Count == 0)
        {
            Debug.LogWarning("Dialogue lines are empty.");
            return;
        }

        BasePanel openedPanel = UIManager.Instance != null
            ? UIManager.Instance.OpenPanel(dialoguePanelName, null, panelLayer)
            : null;

        CurrentPanel = openedPanel as DialoguePanel;
        if (CurrentPanel == null)
        {
            Debug.LogWarning($"Dialogue panel {dialoguePanelName} is missing or does not inherit DialoguePanel.");
            return;
        }

        IsPlaying = true;
        DialogueStarted?.Invoke();
        CurrentPanel.Play(lines, FinishDialogue);
    }

    public bool PlayById(string dialogueId)
    {
        List<DialogueLine> lines = GetLinesById(dialogueId);
        if (lines.Count == 0)
        {
            Debug.LogWarning($"Dialogue id not found: {dialogueId}");
            return false;
        }

        Play(lines);
        return true;
    }

    public List<DialogueLine> GetLinesById(string dialogueId)
    {
        List<DialogueLine> lines = new List<DialogueLine>();
        if (string.IsNullOrEmpty(dialogueId)) return lines;

        EnsureDialogueTableLoaded();
        if (_dialogueTable == null) return lines;

        List<DialogueTableRow> matchedRows = new List<DialogueTableRow>();
        foreach (DialogueTableRow row in _dialogueTable.lines)
        {
            if (row != null && row.dialogueId == dialogueId)
            {
                matchedRows.Add(row);
            }
        }

        matchedRows.Sort((left, right) => left.lineIndex.CompareTo(right.lineIndex));

        foreach (DialogueTableRow row in matchedRows)
        {
            lines.Add(CreateLine(row));
        }

        return lines;
    }

    public void Stop()
    {
        FinishDialogue();
    }

    private void FinishDialogue()
    {
        if (!IsPlaying) return;

        IsPlaying = false;
        CurrentPanel = null;

        if (UIManager.Instance != null && UIManager.Instance.IsPanelOpen(dialoguePanelName))
        {
            UIManager.Instance.ClosePanel(dialoguePanelName);
        }

        DialogueFinished?.Invoke();
    }

    private void EnsureDialogueTableLoaded()
    {
        if (_dialogueTable != null) return;

        _dialogueTable = Resources.Load<DialogueTable>(dialogueTableResourcePath);
        if (_dialogueTable == null)
        {
            Debug.LogWarning($"Dialogue table not found at Resources/{dialogueTableResourcePath}");
        }
    }

    private DialogueLine CreateLine(DialogueTableRow row)
    {
        return new DialogueLine
        {
            speakerName = row.speakerName,
            text = GetLocalizedText(row),
            portraitOverride = LoadSprite(row.portraitResource),
            standingOverride = LoadSprite(row.standingResource),
            expression = row.expression,
            style = row.style,
            autoPlayDelay = row.autoPlayDelay,
            options = CreateOptions(row)
        };
    }

    private string GetLocalizedText(DialogueTableRow row)
    {
        return GetLocalizedText(row.localizationKey);
    }

    private Sprite LoadSprite(string resourcePath)
    {
        return string.IsNullOrEmpty(resourcePath) ? null : Resources.Load<Sprite>(resourcePath);
    }

    private List<DialogueOption> CreateOptions(DialogueTableRow row)
    {
        List<DialogueOption> options = new List<DialogueOption>();

        if (row.options == null) return options;

        foreach (DialogueTableOptionRow optionRow in row.options)
        {
            if (optionRow == null) continue;
            AddOption(options, optionRow.localizationKey, optionRow.nextDialogueId, optionRow.condition, optionRow.effects);
        }

        return options;
    }

    private void AddOption(List<DialogueOption> options, string localizationKey, string nextDialogueId, string condition, string effects)
    {
        string text = GetLocalizedText(localizationKey);
        if (string.IsNullOrEmpty(text)) return;

        options.Add(new DialogueOption
        {
            text = text,
            nextDialogueId = nextDialogueId,
            condition = condition,
            effects = effects
        });
    }

    private string GetLocalizedText(string localizationKey)
    {
        if (string.IsNullOrEmpty(localizationKey))
        {
            return string.Empty;
        }

        string localizedText = LocalizationManager.Get(localizationKey);
        return localizedText == localizationKey ? string.Empty : localizedText;
    }
}
