using System;
using System.Collections.Generic;
using UnityEngine;

public enum DialogueBoxStyle
{
    Normal,
    Narration,
    InnerThought,
    System
}

[Serializable]
public class DialogueLine
{
    public DialogueCharacterProfile character;
    public string speakerName;
    [TextArea(2, 5)] public string text;
    public Sprite portraitOverride;
    public Sprite standingOverride;
    public string expression;
    public DialogueBoxStyle style = DialogueBoxStyle.Normal;
    public float autoPlayDelay = 1.2f;
    public List<DialogueOption> options = new List<DialogueOption>();
}

[Serializable]
public class DialogueOption
{
    public string text;
    public string nextDialogueId;
    public string condition;
    public string effects;
}

[Serializable]
public class DialogueSequence
{
    public List<DialogueLine> lines = new List<DialogueLine>();
}
