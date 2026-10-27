using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

public class DialogueExcelImporterWindow : EditorWindow
{
    private const string DefaultExcelPath = "Assets/Config/Dialogue/DialogueTable.xlsx";
    private const string DefaultAssetPath = "Assets/Resources/ConfigTables/DialogueTable.asset";
    private const string DefaultLayoutPath = "Assets/Editor/Config/DialogueEditorLayout.asset";

    private readonly string[] _requiredHeaders =
    {
        "dialogueId",
        "lineIndex",
        "speakerName",
        "localizationKey",
        "portraitResource",
        "standingResource",
        "expression",
        "style",
        "autoPlayDelay"
    };

    private readonly string[] _optionalHeaders =
    {
        "option1LocalizationKey",
        "option1NextDialogueId",
        "option1Condition",
        "option1Effects",
        "option2LocalizationKey",
        "option2NextDialogueId",
        "option2Condition",
        "option2Effects",
        "option3LocalizationKey",
        "option3NextDialogueId",
        "option3Condition",
        "option3Effects",
        "option4LocalizationKey",
        "option4NextDialogueId",
        "option4Condition",
        "option4Effects"
    };

    private string _excelPath = DefaultExcelPath;
    private string _assetPath = DefaultAssetPath;
    private string[] _sheetNames = Array.Empty<string>();
    private int _selectedSheetIndex;
    private bool _importAllSheets = true;
    private Vector2 _scrollPosition;
    private List<string> _messages = new List<string>();

    [MenuItem("Tools/Template/Dialogue/Import Dialogue Excel")]
    public static void ShowWindow()
    {
        GetWindow<DialogueExcelImporterWindow>("对话表导入");
    }

    private void OnEnable()
    {
        RefreshSheets();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("对话 Excel 导入", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("这个窗口只导入 DialogueTable，和普通游戏数据配置表分开。可用多个工作表拆分章节或场景，导入后通过 dialogueId 播放多组对话。", MessageType.Info);

        DrawSection("1. Excel 文件", () =>
        {
            DrawExcelPathField();
            DrawSheetOptions();
        });

        DrawSection("2. 输出资源", () =>
        {
            DrawAssetPathField();
        });

        DrawSection("3. 表头格式", () =>
        {
            EditorGUILayout.HelpBox("必填：" + string.Join(", ", _requiredHeaders), MessageType.None);
            EditorGUILayout.HelpBox("选项列（可选）：" + string.Join(", ", _optionalHeaders), MessageType.None);
        });

        DrawSection("4. 操作", () =>
        {
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_excelPath) || _sheetNames.Length == 0))
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("校验", GUILayout.Height(32)))
                {
                    Validate();
                }

                if (GUILayout.Button("导入", GUILayout.Height(32)))
                {
                    Import();
                }

                EditorGUILayout.EndHorizontal();
            }
        });

        DrawMessages();
    }

    private void DrawSection(string title, Action drawContent)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.Space(2);
        drawContent?.Invoke();
        EditorGUILayout.EndVertical();
    }

    private void DrawExcelPathField()
    {
        EditorGUILayout.BeginHorizontal();
        string nextPath = EditorGUILayout.TextField("对话 Excel", _excelPath);
        if (nextPath != _excelPath)
        {
            _excelPath = nextPath;
            RefreshSheets();
        }

        if (GUILayout.Button("浏览", GUILayout.Width(70)))
        {
            string selected = EditorUtility.OpenFilePanel("选择对话 Excel", Application.dataPath, "xlsx");
            if (!string.IsNullOrEmpty(selected))
            {
                _excelPath = ToAssetPath(selected);
                RefreshSheets();
            }
        }

        if (GUILayout.Button("刷新", GUILayout.Width(70)))
        {
            RefreshSheets();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSheetOptions()
    {
        if (_sheetNames.Length == 0)
        {
            EditorGUILayout.HelpBox("尚未读取到工作表。请确认 Excel 路径正确。", MessageType.Warning);
            return;
        }

        _importAllSheets = EditorGUILayout.Toggle("导入全部工作表", _importAllSheets);

        using (new EditorGUI.DisabledScope(_importAllSheets))
        {
            _selectedSheetIndex = EditorGUILayout.Popup("指定工作表", _selectedSheetIndex, _sheetNames);
        }
    }

    private void DrawAssetPathField()
    {
        EditorGUILayout.BeginHorizontal();
        _assetPath = EditorGUILayout.TextField("DialogueTable SO", _assetPath);

        if (GUILayout.Button("选择", GUILayout.Width(70)))
        {
            string selected = EditorUtility.SaveFilePanelInProject(
                "选择 DialogueTable 保存路径",
                "DialogueTable",
                "asset",
                "请选择 DialogueTable 保存路径。",
                "Assets/Resources/ConfigTables");

            if (!string.IsNullOrEmpty(selected))
            {
                _assetPath = selected;
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawMessages()
    {
        if (_messages.Count == 0) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("执行结果", EditorStyles.boldLabel);
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(180));

        foreach (string message in _messages)
        {
            MessageType messageType = message.StartsWith("错误", StringComparison.Ordinal) ? MessageType.Error : MessageType.Info;
            EditorGUILayout.HelpBox(message, messageType);
        }

        EditorGUILayout.EndScrollView();
    }

    private void RefreshSheets()
    {
        _selectedSheetIndex = 0;
        _sheetNames = Array.Empty<string>();

        if (string.IsNullOrEmpty(_excelPath) || !File.Exists(_excelPath))
        {
            return;
        }

        try
        {
            List<ExcelSheetInfo> sheets = ExcelTableReader.GetSheets(_excelPath);
            List<string> names = new List<string>();
            foreach (ExcelSheetInfo sheet in sheets)
            {
                names.Add(sheet.Name);
            }

            _sheetNames = names.ToArray();
        }
        catch (Exception exception)
        {
            _messages = new List<string> { "错误：读取工作表失败。" + exception.Message };
            Debug.LogException(exception);
        }
    }

    private void Validate()
    {
        _messages = new List<string>();

        try
        {
            List<DialogueTableRow> rows = ReadRows(out List<string> errors, out int sheetCount);
            if (errors.Count > 0)
            {
                AddErrors(errors);
                return;
            }

            _messages.Add($"校验通过。工作表：{sheetCount}，台词行：{rows.Count}，对话组：{CountDialogueIds(rows)}");
        }
        catch (Exception exception)
        {
            _messages.Add("错误：" + exception.Message);
            Debug.LogException(exception);
        }
    }

    private void Import()
    {
        _messages = new List<string>();

        try
        {
            List<DialogueTableRow> rows = ReadRows(out List<string> errors, out int sheetCount);
            if (errors.Count > 0)
            {
                AddErrors(errors);
                return;
            }

            DialogueTable table = GetOrCreateDialogueTable();
            table.lines = rows;
            EditorUtility.SetDirty(table);
            RecordImportMetadata(_excelPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _messages.Add($"导入完成。工作表：{sheetCount}，台词行：{rows.Count}，对话组：{CountDialogueIds(rows)}，目标：{_assetPath}。节点布局已按 dialogueId 保留。");
        }
        catch (Exception exception)
        {
            _messages.Add("错误：" + exception.Message);
            Debug.LogException(exception);
        }
    }

    private List<DialogueTableRow> ReadRows(out List<string> errors, out int sheetCount)
    {
        errors = new List<string>();
        List<DialogueTableRow> rows = new List<DialogueTableRow>();
        List<ExcelSheet> sheets = ReadSelectedSheets();
        sheetCount = sheets.Count;

        HashSet<string> lineKeys = new HashSet<string>();

        foreach (ExcelSheet sheet in sheets)
        {
            if (sheet.Rows.Count == 0)
            {
                errors.Add($"工作表 {sheet.Name} 为空。");
                continue;
            }

            Dictionary<string, int> headerMap = BuildHeaderMap(sheet, errors);
            if (headerMap == null)
            {
                continue;
            }

            for (int rowIndex = 1; rowIndex < sheet.Rows.Count; rowIndex++)
            {
                List<string> row = sheet.Rows[rowIndex];
                if (IsEmptyRow(row)) continue;

                try
                {
                    DialogueTableRow dialogueRow = CreateDialogueRow(sheet.Name, rowIndex + 1, row, headerMap);
                    string lineKey = $"{dialogueRow.dialogueId}:{dialogueRow.lineIndex}";
                    if (!lineKeys.Add(lineKey))
                    {
                        errors.Add($"工作表 {sheet.Name} 第 {rowIndex + 1} 行：dialogueId + lineIndex 重复：{lineKey}");
                    }

                    rows.Add(dialogueRow);
                }
                catch (Exception exception)
                {
                    errors.Add($"工作表 {sheet.Name} 第 {rowIndex + 1} 行：{exception.Message}");
                }
            }
        }

        rows.Sort((left, right) =>
        {
            int idCompare = string.Compare(left.dialogueId, right.dialogueId, StringComparison.Ordinal);
            return idCompare != 0 ? idCompare : left.lineIndex.CompareTo(right.lineIndex);
        });

        return rows;
    }

    private List<ExcelSheet> ReadSelectedSheets()
    {
        if (!File.Exists(_excelPath))
        {
            throw new FileNotFoundException("未找到对话 Excel 文件。", _excelPath);
        }

        List<ExcelSheet> sheets = new List<ExcelSheet>();

        if (_importAllSheets)
        {
            foreach (string sheetName in _sheetNames)
            {
                sheets.Add(ExcelTableReader.ReadSheet(_excelPath, sheetName));
            }
        }
        else
        {
            string sheetName = _sheetNames[Mathf.Clamp(_selectedSheetIndex, 0, _sheetNames.Length - 1)];
            sheets.Add(ExcelTableReader.ReadSheet(_excelPath, sheetName));
        }

        return sheets;
    }

    private Dictionary<string, int> BuildHeaderMap(ExcelSheet sheet, List<string> errors)
    {
        Dictionary<string, int> headerMap = new Dictionary<string, int>();
        List<string> headerRow = sheet.Rows[0];

        for (int i = 0; i < headerRow.Count; i++)
        {
            string header = headerRow[i].Trim();
            if (string.IsNullOrEmpty(header)) continue;

            if (headerMap.ContainsKey(header))
            {
                errors.Add($"工作表 {sheet.Name}：表头重复：{header}");
            }
            else
            {
                headerMap.Add(header, i);
            }
        }

        foreach (string requiredHeader in _requiredHeaders)
        {
            if (!headerMap.ContainsKey(requiredHeader))
            {
                errors.Add($"工作表 {sheet.Name}：缺少表头：{requiredHeader}");
            }
        }

        return errors.Count > 0 ? null : headerMap;
    }

    private DialogueTableRow CreateDialogueRow(string sheetName, int rowNumber, List<string> row, Dictionary<string, int> headerMap)
    {
        DialogueTableRow dialogueRow = new DialogueTableRow
        {
            dialogueId = GetCell(row, headerMap, "dialogueId").Trim(),
            lineIndex = ParseInt(GetCell(row, headerMap, "lineIndex"), "lineIndex"),
            speakerName = GetCell(row, headerMap, "speakerName"),
            localizationKey = GetCell(row, headerMap, "localizationKey"),
            portraitResource = GetCell(row, headerMap, "portraitResource"),
            standingResource = GetCell(row, headerMap, "standingResource"),
            expression = GetCell(row, headerMap, "expression"),
            style = ParseStyle(GetCell(row, headerMap, "style")),
            autoPlayDelay = ParseFloat(GetCell(row, headerMap, "autoPlayDelay"), 1.2f)
        };

        dialogueRow.options.AddRange(CreateOptionRows(row, headerMap));

        if (string.IsNullOrEmpty(dialogueRow.dialogueId))
        {
            throw new InvalidDataException("dialogueId 不能为空。");
        }

        if (dialogueRow.lineIndex < 0)
        {
            throw new InvalidDataException("lineIndex 不能小于 0。");
        }

        if (string.IsNullOrEmpty(dialogueRow.localizationKey))
        {
            throw new InvalidDataException("localizationKey 不能为空。");
        }

        return dialogueRow;
    }

    private List<DialogueTableOptionRow> CreateOptionRows(List<string> row, Dictionary<string, int> headerMap)
    {
        List<DialogueTableOptionRow> options = new List<DialogueTableOptionRow>();
        AddOptionRow(options, row, headerMap, 1);
        AddOptionRow(options, row, headerMap, 2);
        AddOptionRow(options, row, headerMap, 3);
        AddOptionRow(options, row, headerMap, 4);
        return options;
    }

    private void AddOptionRow(List<DialogueTableOptionRow> options, List<string> row, Dictionary<string, int> headerMap, int index)
    {
        string prefix = $"option{index}";
        string localizationKey = GetOptionalCell(row, headerMap, $"{prefix}LocalizationKey");
        string nextDialogueId = GetOptionalCell(row, headerMap, $"{prefix}NextDialogueId");
        string condition = GetOptionalCell(row, headerMap, $"{prefix}Condition");
        string effects = GetOptionalCell(row, headerMap, $"{prefix}Effects");

        if (string.IsNullOrEmpty(localizationKey) &&
            string.IsNullOrEmpty(nextDialogueId) &&
            string.IsNullOrEmpty(condition) &&
            string.IsNullOrEmpty(effects))
        {
            return;
        }

        options.Add(new DialogueTableOptionRow
        {
            localizationKey = localizationKey,
            nextDialogueId = nextDialogueId,
            condition = condition,
            effects = effects
        });
    }

    private DialogueTable GetOrCreateDialogueTable()
    {
        if (string.IsNullOrEmpty(_assetPath))
        {
            _assetPath = DefaultAssetPath;
        }

        if (!_assetPath.StartsWith("Assets/", StringComparison.Ordinal) || !_assetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("保存路径必须位于 Assets 下，并以 .asset 结尾。");
        }

        DialogueTable existing = AssetDatabase.LoadAssetAtPath<DialogueTable>(_assetPath);
        if (existing != null)
        {
            return existing;
        }

        string directory = Path.GetDirectoryName(_assetPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        DialogueTable table = CreateInstance<DialogueTable>();
        AssetDatabase.CreateAsset(table, _assetPath);
        AssetDatabase.ImportAsset(_assetPath);
        return table;
    }

    private void RecordImportMetadata(string excelPath)
    {
        DialogueEditorLayout layout = AssetDatabase.LoadAssetAtPath<DialogueEditorLayout>(DefaultLayoutPath);
        if (layout == null)
        {
            layout = CreateInstance<DialogueEditorLayout>();
            AssetDatabase.CreateAsset(layout, DefaultLayoutPath);
        }

        layout.lastImportedExcelPath = excelPath;
        layout.lastImportedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        EditorUtility.SetDirty(layout);
    }

    private void AddErrors(List<string> errors)
    {
        foreach (string error in errors)
        {
            _messages.Add("错误：" + error);
        }
    }

    private int CountDialogueIds(List<DialogueTableRow> rows)
    {
        HashSet<string> ids = new HashSet<string>();
        foreach (DialogueTableRow row in rows)
        {
            if (!string.IsNullOrEmpty(row.dialogueId))
            {
                ids.Add(row.dialogueId);
            }
        }

        return ids.Count;
    }

    private string GetCell(List<string> row, Dictionary<string, int> headerMap, string header)
    {
        int index = headerMap[header];
        return row != null && index >= 0 && index < row.Count ? row[index] : string.Empty;
    }

    private string GetOptionalCell(List<string> row, Dictionary<string, int> headerMap, string header)
    {
        if (!headerMap.TryGetValue(header, out int index))
        {
            return string.Empty;
        }

        return row != null && index >= 0 && index < row.Count ? row[index] : string.Empty;
    }

    private int ParseInt(string value, string fieldName)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
        {
            return result;
        }

        throw new FormatException($"{fieldName} 不是有效整数：{value}");
    }

    private float ParseFloat(string value, float defaultValue)
    {
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
        {
            return result;
        }

        throw new FormatException($"autoPlayDelay 不是有效数字：{value}");
    }

    private DialogueBoxStyle ParseStyle(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return DialogueBoxStyle.Normal;
        }

        if (int.TryParse(value, out int styleIndex))
        {
            return (DialogueBoxStyle)styleIndex;
        }

        return (DialogueBoxStyle)Enum.Parse(typeof(DialogueBoxStyle), value, true);
    }

    private bool IsEmptyRow(List<string> row)
    {
        if (row == null) return true;

        foreach (string cell in row)
        {
            if (!string.IsNullOrWhiteSpace(cell))
            {
                return false;
            }
        }

        return true;
    }

    private string ToAssetPath(string absolutePath)
    {
        absolutePath = absolutePath.Replace("\\", "/");
        string dataPath = Application.dataPath.Replace("\\", "/");

        if (absolutePath.StartsWith(dataPath, StringComparison.Ordinal))
        {
            return "Assets" + absolutePath.Substring(dataPath.Length);
        }

        return absolutePath;
    }
}
