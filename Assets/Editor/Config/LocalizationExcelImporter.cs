using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class LocalizationExcelImporter
{
    private const string ExcelPath = "Assets/Config/Localization.xlsx";
    private const string AssetPath = "Assets/Resources/Data/LocalizationTable.asset";

    [MenuItem("Tools/Template/Localization/Import Excel")]
    public static void ImportExcel()
    {
        try
        {
            if (!File.Exists(ExcelPath))
            {
                EditorUtility.DisplayDialog("Localization Import", $"Excel file not found:\n{ExcelPath}", "OK");
                return;
            }

            ExcelSheet sheet = ExcelTableReader.ReadFirstSheet(ExcelPath);
            LocalizationTable table = LoadOrCreateTable();

            ApplySheetToTable(sheet, table);

            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Localization Import", $"Imported {table.languages.Count} languages from:\n{ExcelPath}", "OK");
        }
        catch (System.Exception exception)
        {
            Debug.LogError(exception);
            EditorUtility.DisplayDialog("Localization Import", "Import failed. Check Console for details.", "OK");
        }
    }

    [MenuItem("Tools/Template/Localization/Validate Excel")]
    public static void ValidateExcel()
    {
        try
        {
            if (!File.Exists(ExcelPath))
            {
                EditorUtility.DisplayDialog("Localization Validate", $"Excel file not found:\n{ExcelPath}", "OK");
                return;
            }

            ExcelSheet sheet = ExcelTableReader.ReadFirstSheet(ExcelPath);
            List<string> errors = ValidateSheet(sheet);

            if (errors.Count == 0)
            {
                EditorUtility.DisplayDialog("Localization Validate", "Localization Excel is valid.", "OK");
                return;
            }

            Debug.LogError("Localization Excel validation failed:\n" + string.Join("\n", errors));
            EditorUtility.DisplayDialog("Localization Validate", $"Found {errors.Count} issue(s). Check Console for details.", "OK");
        }
        catch (System.Exception exception)
        {
            Debug.LogError(exception);
            EditorUtility.DisplayDialog("Localization Validate", "Validate failed. Check Console for details.", "OK");
        }
    }

    private static LocalizationTable LoadOrCreateTable()
    {
        LocalizationTable table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(AssetPath);

        if (table != null)
        {
            return table;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
        table = ScriptableObject.CreateInstance<LocalizationTable>();
        AssetDatabase.CreateAsset(table, AssetPath);
        return table;
    }

    private static void ApplySheetToTable(ExcelSheet sheet, LocalizationTable table)
    {
        List<string> errors = ValidateSheet(sheet);
        if (errors.Count > 0)
        {
            throw new InvalidDataException("Localization Excel validation failed:\n" + string.Join("\n", errors));
        }

        List<string> languages = GetLanguages(sheet.Rows[0]);
        Dictionary<string, LocalizationLanguage> languageMap = new Dictionary<string, LocalizationLanguage>();

        foreach (string languageCode in languages)
        {
            languageMap[languageCode] = new LocalizationLanguage
            {
                languageCode = languageCode,
                entries = new List<LocalizationEntry>()
            };
        }

        for (int rowIndex = 1; rowIndex < sheet.Rows.Count; rowIndex++)
        {
            List<string> row = sheet.Rows[rowIndex];
            string key = GetCell(row, 0).Trim();
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            for (int languageIndex = 0; languageIndex < languages.Count; languageIndex++)
            {
                string languageCode = languages[languageIndex];
                string value = GetCell(row, languageIndex + 1);

                languageMap[languageCode].entries.Add(new LocalizationEntry
                {
                    key = key,
                    value = value
                });
            }
        }

        table.languages = new List<LocalizationLanguage>();
        foreach (string languageCode in languages)
        {
            table.languages.Add(languageMap[languageCode]);
        }
    }

    private static List<string> ValidateSheet(ExcelSheet sheet)
    {
        List<string> errors = new List<string>();

        if (sheet.Rows.Count == 0)
        {
            errors.Add("Sheet is empty.");
            return errors;
        }

        List<string> header = sheet.Rows[0];
        if (GetCell(header, 0).Trim() != "key")
        {
            errors.Add("Cell A1 must be 'key'.");
        }

        List<string> languages = GetLanguages(header);
        if (languages.Count == 0)
        {
            errors.Add("At least one language column is required.");
        }

        HashSet<string> languageSet = new HashSet<string>();
        foreach (string language in languages)
        {
            if (!languageSet.Add(language))
            {
                errors.Add($"Duplicate language column: {language}");
            }
        }

        HashSet<string> keys = new HashSet<string>();
        for (int rowIndex = 1; rowIndex < sheet.Rows.Count; rowIndex++)
        {
            List<string> row = sheet.Rows[rowIndex];
            string key = GetCell(row, 0).Trim();
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            if (!keys.Add(key))
            {
                errors.Add($"Duplicate key at row {rowIndex + 1}: {key}");
            }

            for (int languageIndex = 0; languageIndex < languages.Count; languageIndex++)
            {
                if (string.IsNullOrEmpty(GetCell(row, languageIndex + 1)))
                {
                    errors.Add($"Missing value at row {rowIndex + 1}, language {languages[languageIndex]}, key {key}");
                }
            }
        }

        return errors;
    }

    private static List<string> GetLanguages(List<string> header)
    {
        List<string> languages = new List<string>();

        for (int i = 1; i < header.Count; i++)
        {
            string language = header[i].Trim();
            if (!string.IsNullOrEmpty(language))
            {
                languages.Add(language);
            }
        }

        return languages;
    }

    private static string GetCell(List<string> row, int index)
    {
        return row != null && index >= 0 && index < row.Count ? row[index] : string.Empty;
    }
}
