using System;
using System.Collections.Generic;
using UnityEngine;

public class DialogueEditorLayout : ScriptableObject
{
    public List<DialogueNodeLayout> nodes = new List<DialogueNodeLayout>();
    public string lastImportedExcelPath;
    public string lastImportedAt;
    public string lastExportedExcelPath;
    public string lastExportedAt;
    public string lastEditedInEditorAt;

    public bool TryGetPosition(string dialogueId, out Vector2 position)
    {
        if (nodes == null)
        {
            nodes = new List<DialogueNodeLayout>();
        }

        foreach (DialogueNodeLayout node in nodes)
        {
            if (node != null && node.dialogueId == dialogueId)
            {
                position = node.position;
                return true;
            }
        }

        position = Vector2.zero;
        return false;
    }

    public void SetPosition(string dialogueId, Vector2 position)
    {
        if (string.IsNullOrEmpty(dialogueId)) return;
        if (nodes == null)
        {
            nodes = new List<DialogueNodeLayout>();
        }

        foreach (DialogueNodeLayout node in nodes)
        {
            if (node != null && node.dialogueId == dialogueId)
            {
                node.position = position;
                return;
            }
        }

        nodes.Add(new DialogueNodeLayout
        {
            dialogueId = dialogueId,
            position = position
        });
    }

    public void RemoveMissingNodes(HashSet<string> dialogueIds)
    {
        if (nodes == null)
        {
            nodes = new List<DialogueNodeLayout>();
            return;
        }

        nodes.RemoveAll(node => node == null || !dialogueIds.Contains(node.dialogueId));
    }
}

[Serializable]
public class DialogueNodeLayout
{
    public string dialogueId;
    public Vector2 position;
}
