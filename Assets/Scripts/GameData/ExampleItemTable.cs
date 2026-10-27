using System;
using System.Collections.Generic;
using UnityEngine;

public enum ExampleItemRarity
{
    Common,
    Rare,
    Epic
}

[Serializable]
public class ExampleItemData
{
    public int id;
    public string itemName;
    public ExampleItemRarity rarity;
    public int maxStack;
    public float weight;
    public bool sellable;
    [TextArea] public string description;
}

[CreateAssetMenu(fileName = "ExampleItemTable", menuName = "Template/Config Table/Example Item Table")]
[ConfigTableAsset]
public class ExampleItemTable : ScriptableObject
{
    public List<ExampleItemData> items = new List<ExampleItemData>();
}
