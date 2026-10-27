using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LocalizationEntry
{
    public string key;
    [TextArea] public string value;
}

[Serializable]
public class LocalizationLanguage
{
    public string languageCode;
    public List<LocalizationEntry> entries = new List<LocalizationEntry>();
}

[CreateAssetMenu(fileName = "LocalizationTable", menuName = "Template/Localization/Table")]
public class LocalizationTable : ScriptableObject
{
    public List<LocalizationLanguage> languages = new List<LocalizationLanguage>();
}
