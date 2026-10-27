using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueTableRow
{
    public string dialogueId;
    public int lineIndex;
    public string speakerName;
    [TextArea] public string text;
    public string portraitResource;
    public string standingResource;
    public string expression;
    public DialogueBoxStyle style = DialogueBoxStyle.Normal;
    public float autoPlayDelay = 1.2f;
    public List<DialogueTableOptionRow> options = new List<DialogueTableOptionRow>();
}

[Serializable]
public class DialogueTableOptionRow
{
    [TextArea] public string text;
    public string nextDialogueId;
    public string condition;
    public string effects;
}

[CreateAssetMenu(fileName = "DialogueTable", menuName = "Template/Config Table/Dialogue Table")]
[ConfigTableAsset]
public class DialogueTable : ScriptableObject
{
    public List<DialogueTableRow> lines = new List<DialogueTableRow>();
}
