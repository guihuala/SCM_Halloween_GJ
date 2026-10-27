using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue Character", menuName = "Game Template/Dialogue/Character Profile")]
public class DialogueCharacterProfile : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private Sprite defaultPortrait;
    [SerializeField] private Sprite defaultStanding;
    [SerializeField] private List<DialogueExpressionSprite> expressions = new List<DialogueExpressionSprite>();

    public string DisplayName => displayName;
    public Sprite DefaultPortrait => defaultPortrait;
    public Sprite DefaultStanding => defaultStanding;

    public Sprite GetExpressionPortrait(string expression)
    {
        if (string.IsNullOrEmpty(expression)) return defaultPortrait;

        for (int i = 0; i < expressions.Count; i++)
        {
            if (expressions[i].expression == expression && expressions[i].portrait != null)
            {
                return expressions[i].portrait;
            }
        }

        return defaultPortrait;
    }
}

[Serializable]
public class DialogueExpressionSprite
{
    public string expression;
    public Sprite portrait;
}
