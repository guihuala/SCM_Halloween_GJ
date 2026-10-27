using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LocalizedSpriteEntry
{
    public string key;
    public string languageCode;
    public Sprite sprite;
}

[CreateAssetMenu(fileName = "LocalizationAssetTable", menuName = "Template/Localization/Asset Table")]
public class LocalizationAssetTable : ScriptableObject
{
    public List<LocalizedSpriteEntry> sprites = new List<LocalizedSpriteEntry>();

    public Sprite GetSprite(string key, string languageCode)
    {
        Sprite fallback = null;

        foreach (LocalizedSpriteEntry entry in sprites)
        {
            if (entry == null || entry.key != key)
            {
                continue;
            }

            if (entry.languageCode == languageCode)
            {
                return entry.sprite;
            }

            if (entry.languageCode == AppConfigProvider.DefaultLanguage)
            {
                fallback = entry.sprite;
            }
        }

        return fallback;
    }
}
