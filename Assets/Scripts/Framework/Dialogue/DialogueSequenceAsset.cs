using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue Sequence", menuName = "Game Template/Dialogue/Dialogue Sequence")]
public class DialogueSequenceAsset : ScriptableObject
{
    [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();

    public IList<DialogueLine> Lines => lines;
}
