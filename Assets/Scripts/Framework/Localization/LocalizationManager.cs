using System;
using System.Collections.Generic;
using UnityEngine;

public static class LocalizationManager
{
    private const string TablePath = "Data/LocalizationTable";

    private static LocalizationTable _table;
    private static Dictionary<string, string> _currentEntries;
    private static string _currentLanguage;

    public static event Action LanguageChanged;

    public static string CurrentLanguage
    {
        get
        {
            EnsureInitialized();
            return _currentLanguage;
        }
    }

    public static void SetLanguage(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode))
        {
            languageCode = AppConfigProvider.DefaultLanguage;
        }

        EnsureTableLoaded();
        _currentLanguage = languageCode;
        _currentEntries = BuildLanguageDictionary(languageCode);

        AppConfig config = AppConfigProvider.Config;
        string prefsKey = config != null && !string.IsNullOrEmpty(config.languagePlayerPrefsKey)
            ? config.languagePlayerPrefsKey
            : "template.language";
        PlayerPrefs.SetString(prefsKey, languageCode);
        PlayerPrefs.Save();

        LanguageChanged?.Invoke();
    }

    public static string Get(string key)
    {
        EnsureInitialized();

        if (string.IsNullOrEmpty(key))
        {
            return string.Empty;
        }

        if (_currentEntries != null && _currentEntries.TryGetValue(key, out string value))
        {
            return value;
        }

        return key;
    }

    public static string Format(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }

    private static void EnsureInitialized()
    {
        if (_currentEntries != null)
        {
            return;
        }

        AppConfig config = AppConfigProvider.Config;
        string prefsKey = config != null && !string.IsNullOrEmpty(config.languagePlayerPrefsKey)
            ? config.languagePlayerPrefsKey
            : "template.language";
        string language = PlayerPrefs.GetString(prefsKey, AppConfigProvider.DefaultLanguage);
        SetLanguage(language);
    }

    private static void EnsureTableLoaded()
    {
        if (_table == null)
        {
            _table = Resources.Load<LocalizationTable>(TablePath);
        }
    }

    private static Dictionary<string, string> BuildLanguageDictionary(string languageCode)
    {
        Dictionary<string, string> entries = new Dictionary<string, string>();

        if (_table == null)
        {
            Debug.LogWarning($"Localization table not found at Resources/{TablePath}");
            return entries;
        }

        LocalizationLanguage language = FindLanguage(languageCode) ?? FindLanguage(AppConfigProvider.DefaultLanguage);

        if (language == null)
        {
            return entries;
        }

        foreach (LocalizationEntry entry in language.entries)
        {
            if (entry == null || string.IsNullOrEmpty(entry.key))
            {
                continue;
            }

            entries[entry.key] = entry.value;
        }

        return entries;
    }

    private static LocalizationLanguage FindLanguage(string languageCode)
    {
        if (_table == null || string.IsNullOrEmpty(languageCode))
        {
            return null;
        }

        foreach (LocalizationLanguage language in _table.languages)
        {
            if (language != null && language.languageCode == languageCode)
            {
                return language;
            }
        }

        return null;
    }
}
