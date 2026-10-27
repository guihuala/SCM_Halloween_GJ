using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class ConfigTableExcelImporterWindow : EditorWindow
{
    private string _excelPath = "Assets/Config/GameData/ExampleItems.xlsx";
    private ScriptableObject _targetAsset;
    private string _createAssetPath = "Assets/Resources/ConfigTables/ExampleItemTable.asset";
    private Type[] _supportedAssetTypes = Array.Empty<Type>();
    private string[] _supportedAssetTypeNames = Array.Empty<string>();
    private int _selectedAssetTypeIndex;
    private string[] _sheetNames = Array.Empty<string>();
    private int _selectedSheetIndex;
    private string[] _listFieldNames = Array.Empty<string>();
    private int _selectedListFieldIndex;
    private Vector2 _scrollPosition;
    private List<string> _lastMessages = new List<string>();

    [MenuItem("Tools/Template/Config Tables/Import Excel To ScriptableObject")]
    public static void ShowWindow()
    {
        GetWindow<ConfigTableExcelImporterWindow>("配置表导入");
    }

    private void OnEnable()
    {
        RefreshSupportedAssetTypes();
        RefreshSheets();
        RefreshListFields();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Excel 导入 ScriptableObject", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("第一行必须是表头，字段名需要和列表元素类型里的字段名一致。只有标记了 [ConfigTableAsset] 的 SO 类型会出现在自动创建列表里。", MessageType.Info);

        DrawSection("1. Excel 来源", () =>
        {
            DrawExcelPathField();
            DrawSheetPopup();
        });

        DrawSection("2. 目标资源", () =>
        {
            DrawTargetField();
            DrawCreateAssetFields();
        });

        DrawSection("3. 字段映射", DrawListFieldPopup);

        DrawSection("4. 操作", () =>
        {
            using (new EditorGUI.DisabledScope(!HasTargetOrCreatableType() || _listFieldNames.Length == 0 || _sheetNames.Length == 0 || string.IsNullOrEmpty(_excelPath)))
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
        string nextPath = EditorGUILayout.TextField("Excel 文件", _excelPath);
        if (nextPath != _excelPath)
        {
            _excelPath = nextPath;
            RefreshSheets();
        }

        if (GUILayout.Button("浏览", GUILayout.Width(70)))
        {
            string selected = EditorUtility.OpenFilePanel("选择 Excel 文件", Application.dataPath, "xlsx");
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

    private void DrawSheetPopup()
    {
        if (_sheetNames.Length == 0)
        {
            EditorGUILayout.HelpBox("尚未读取到工作表。请确认 Excel 路径正确，然后点击刷新。", MessageType.Warning);
            return;
        }

        _selectedSheetIndex = EditorGUILayout.Popup("工作表", _selectedSheetIndex, _sheetNames);
    }

    private void DrawTargetField()
    {
        ScriptableObject nextTarget = (ScriptableObject)EditorGUILayout.ObjectField(
            "目标 SO（可选）",
            _targetAsset,
            typeof(ScriptableObject),
            false);

        if (nextTarget != _targetAsset)
        {
            _targetAsset = nextTarget;
            RefreshListFields();
        }
    }

    private void DrawCreateAssetFields()
    {
        if (_targetAsset != null)
        {
            EditorGUILayout.HelpBox("已选择目标 SO，将导入到该资源。清空该字段后，可选择类型并自动创建 SO。", MessageType.Info);
            return;
        }

        if (_supportedAssetTypes.Length == 0)
        {
            EditorGUILayout.HelpBox("没有找到标记 [ConfigTableAsset] 且包含 List<T> 字段的 ScriptableObject 类型。", MessageType.Warning);
            return;
        }

        int nextIndex = EditorGUILayout.Popup("新建 SO 类型", _selectedAssetTypeIndex, _supportedAssetTypeNames);
        if (nextIndex != _selectedAssetTypeIndex)
        {
            _selectedAssetTypeIndex = nextIndex;
            _createAssetPath = GetDefaultAssetPath(GetSelectedAssetType());
            RefreshListFields();
        }

        EditorGUILayout.BeginHorizontal();
        _createAssetPath = EditorGUILayout.TextField("保存路径", _createAssetPath);

        if (GUILayout.Button("选择", GUILayout.Width(70)))
        {
            string selected = EditorUtility.SaveFilePanelInProject(
                "选择 SO 保存路径",
                GetSelectedAssetType()?.Name ?? "ConfigTable",
                "asset",
                "请选择导入后生成的 ScriptableObject 保存路径。",
                "Assets/Resources/ConfigTables");

            if (!string.IsNullOrEmpty(selected))
            {
                _createAssetPath = selected;
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox("未选择目标 SO 时，点击导入会按这里的类型和路径自动创建 SO。", MessageType.None);
    }

    private void DrawListFieldPopup()
    {
        if (_targetAsset == null)
        {
            if (_supportedAssetTypes.Length == 0)
            {
                EditorGUILayout.HelpBox("请选择目标 SO，或先创建一个标记 [ConfigTableAsset] 且带 List<T> 字段的 SO 类型。", MessageType.Warning);
                return;
            }
        }

        if (!HasTargetOrCreatableType())
        {
            EditorGUILayout.HelpBox("请选择目标 SO 或新建 SO 类型。", MessageType.Warning);
            return;
        }

        if (_listFieldNames.Length == 0)
        {
            EditorGUILayout.HelpBox("目标 SO 没有可导入的 List<T> 字段。", MessageType.Warning);
            return;
        }

        _selectedListFieldIndex = EditorGUILayout.Popup("列表字段", _selectedListFieldIndex, _listFieldNames);
    }

    private void DrawMessages()
    {
        if (_lastMessages.Count == 0)
        {
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("执行结果", EditorStyles.boldLabel);
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(180));

        foreach (string message in _lastMessages)
        {
            EditorGUILayout.HelpBox(message, message.StartsWith("错误", StringComparison.Ordinal) ? MessageType.Error : MessageType.Info);
        }

        EditorGUILayout.EndScrollView();
    }

    private void RefreshListFields()
    {
        _selectedListFieldIndex = 0;
        _listFieldNames = Array.Empty<string>();

        Type targetType = GetContextAssetType();

        if (targetType == null)
        {
            return;
        }

        List<string> names = new List<string>();
        foreach (FieldInfo field in GetSerializableFields(targetType))
        {
            if (GetListElementType(field.FieldType) != null)
            {
                names.Add(field.Name);
            }
        }

        _listFieldNames = names.ToArray();
    }

    private void RefreshSupportedAssetTypes()
    {
        List<Type> types = new List<Type>();

        foreach (Type type in TypeCache.GetTypesDerivedFrom<ScriptableObject>())
        {
            if (type.IsAbstract || type.IsGenericType || !IsConfigTableAssetType(type))
            {
                continue;
            }

            foreach (FieldInfo field in GetSerializableFields(type))
            {
                if (GetListElementType(field.FieldType) != null)
                {
                    types.Add(type);
                    break;
                }
            }
        }

        types.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.Ordinal));

        _supportedAssetTypes = types.ToArray();
        List<string> names = new List<string>();
        foreach (Type type in _supportedAssetTypes)
        {
            names.Add(type.Name);
        }

        _supportedAssetTypeNames = names.ToArray();

        for (int i = 0; i < _supportedAssetTypes.Length; i++)
        {
            if (_supportedAssetTypes[i].Name == "ExampleItemTable")
            {
                _selectedAssetTypeIndex = i;
                _createAssetPath = GetDefaultAssetPath(_supportedAssetTypes[i]);
                break;
            }
        }
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
            _lastMessages = new List<string> { "错误：读取工作表失败。" + exception.Message };
            Debug.LogException(exception);
        }
    }

    private void Validate()
    {
        _lastMessages = new List<string>();

        try
        {
            ImportContext context = CreateContext();
            List<string> errors = ValidateSheet(context);

            if (errors.Count == 0)
            {
                _lastMessages.Add($"校验通过。工作表：{context.Sheet.Name}，数据行：{Mathf.Max(0, context.Sheet.Rows.Count - 1)}，元素类型：{context.ElementType.Name}");
            }
            else
            {
                foreach (string error in errors)
                {
                    _lastMessages.Add("错误：" + error);
                }
            }
        }
        catch (Exception exception)
        {
            _lastMessages.Add("错误：" + exception.Message);
            Debug.LogException(exception);
        }
    }

    private void Import()
    {
        _lastMessages = new List<string>();

        try
        {
            ImportContext context = CreateContext();
            List<string> errors = ValidateSheet(context);

            if (errors.Count > 0)
            {
                foreach (string error in errors)
                {
                    _lastMessages.Add("错误：" + error);
                }
                return;
            }

            IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(context.ElementType));

            for (int rowIndex = 1; rowIndex < context.Sheet.Rows.Count; rowIndex++)
            {
                List<string> row = context.Sheet.Rows[rowIndex];
                if (IsEmptyRow(row))
                {
                    continue;
                }

                object item = Activator.CreateInstance(context.ElementType);

                for (int columnIndex = 0; columnIndex < context.Headers.Count; columnIndex++)
                {
                    string header = context.Headers[columnIndex];
                    if (!context.FieldMap.TryGetValue(header, out FieldInfo field))
                    {
                        continue;
                    }

                    string rawValue = GetCell(row, columnIndex);
                    object convertedValue = ConvertValue(rawValue, field.FieldType);
                    field.SetValue(item, convertedValue);
                }

                list.Add(item);
            }

            ScriptableObject targetAsset = GetOrCreateTargetAsset();
            context.ListField.SetValue(targetAsset, list);
            EditorUtility.SetDirty(targetAsset);
            AssetDatabase.SaveAssets();

            _targetAsset = targetAsset;
            RefreshListFields();

            _lastMessages.Add($"导入完成。工作表：{context.Sheet.Name}，行数：{list.Count}，目标：{targetAsset.name}.{context.ListField.Name}");
        }
        catch (Exception exception)
        {
            _lastMessages.Add("错误：" + exception.Message);
            Debug.LogException(exception);
        }
    }

    private ImportContext CreateContext()
    {
        if (!File.Exists(_excelPath))
        {
            throw new FileNotFoundException("未找到 Excel 文件。", _excelPath);
        }

        Type targetAssetType = GetContextAssetType();
        if (targetAssetType == null)
        {
            throw new InvalidOperationException("没有指定目标 ScriptableObject，也没有选择可新建的 SO 类型。");
        }

        FieldInfo listField = GetSerializableField(targetAssetType, _listFieldNames[_selectedListFieldIndex]);
        if (listField == null)
        {
            throw new InvalidOperationException("没有找到列表字段。");
        }

        Type elementType = GetListElementType(listField.FieldType);
        if (elementType == null)
        {
            throw new InvalidOperationException("选择的字段不是 List<T>。");
        }

        if (_sheetNames.Length == 0)
        {
            RefreshSheets();
        }

        string sheetName = _sheetNames.Length > 0 ? _sheetNames[Mathf.Clamp(_selectedSheetIndex, 0, _sheetNames.Length - 1)] : null;
        ExcelSheet sheet = ExcelTableReader.ReadSheet(_excelPath, sheetName);
        if (sheet.Rows.Count == 0)
        {
            throw new InvalidDataException("工作表为空。");
        }

        List<string> headers = new List<string>();
        foreach (string cell in sheet.Rows[0])
        {
            headers.Add(cell.Trim());
        }

        Dictionary<string, FieldInfo> fieldMap = new Dictionary<string, FieldInfo>();
        foreach (FieldInfo field in GetSerializableFields(elementType))
        {
            fieldMap[field.Name] = field;
        }

        return new ImportContext
        {
            Sheet = sheet,
            Headers = headers,
            ListField = listField,
            ElementType = elementType,
            FieldMap = fieldMap
        };
    }

    private List<string> ValidateSheet(ImportContext context)
    {
        List<string> errors = new List<string>();
        HashSet<string> headers = new HashSet<string>();

        foreach (string header in context.Headers)
        {
            if (string.IsNullOrEmpty(header))
            {
                continue;
            }

            if (!headers.Add(header))
            {
                errors.Add($"表头重复：{header}");
            }

            if (!context.FieldMap.ContainsKey(header))
            {
                errors.Add($"表头没有匹配字段：{header}，元素类型：{context.ElementType.Name}");
            }
        }

        foreach (FieldInfo field in context.FieldMap.Values)
        {
            if (!headers.Contains(field.Name))
            {
                errors.Add($"缺少字段列：{field.Name}");
            }
        }

        for (int rowIndex = 1; rowIndex < context.Sheet.Rows.Count; rowIndex++)
        {
            List<string> row = context.Sheet.Rows[rowIndex];
            if (IsEmptyRow(row))
            {
                continue;
            }

            for (int columnIndex = 0; columnIndex < context.Headers.Count; columnIndex++)
            {
                string header = context.Headers[columnIndex];
                if (!context.FieldMap.TryGetValue(header, out FieldInfo field))
                {
                    continue;
                }

                string rawValue = GetCell(row, columnIndex);
                try
                {
                    ConvertValue(rawValue, field.FieldType);
                }
                catch (Exception exception)
                {
                    errors.Add($"第 {rowIndex + 1} 行，列 {header}：{exception.Message}");
                }
            }
        }

        return errors;
    }

    private static object ConvertValue(string rawValue, Type targetType)
    {
        if (targetType == typeof(string))
        {
            return rawValue ?? string.Empty;
        }

        if (string.IsNullOrEmpty(rawValue))
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        if (targetType == typeof(int))
        {
            return int.Parse(rawValue, CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(float))
        {
            return float.Parse(rawValue, CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(double))
        {
            return double.Parse(rawValue, CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(bool))
        {
            if (rawValue == "1") return true;
            if (rawValue == "0") return false;
            return bool.Parse(rawValue);
        }

        if (targetType.IsEnum)
        {
            if (int.TryParse(rawValue, out int enumIndex))
            {
                return Enum.ToObject(targetType, enumIndex);
            }

            return Enum.Parse(targetType, rawValue, true);
        }

        throw new NotSupportedException($"不支持的字段类型：{targetType.Name}");
    }

    private static IEnumerable<FieldInfo> GetSerializableFields(Type type)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (FieldInfo field in type.GetFields(flags))
        {
            if (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
            {
                if (!field.IsInitOnly && !field.IsStatic)
                {
                    yield return field;
                }
            }
        }
    }

    private static FieldInfo GetSerializableField(Type type, string fieldName)
    {
        foreach (FieldInfo field in GetSerializableFields(type))
        {
            if (field.Name == fieldName)
            {
                return field;
            }
        }

        return null;
    }

    private static Type GetListElementType(Type fieldType)
    {
        if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
        {
            return fieldType.GetGenericArguments()[0];
        }

        return null;
    }

    private static string GetCell(List<string> row, int index)
    {
        return row != null && index >= 0 && index < row.Count ? row[index] : string.Empty;
    }

    private static bool IsEmptyRow(List<string> row)
    {
        if (row == null)
        {
            return true;
        }

        foreach (string cell in row)
        {
            if (!string.IsNullOrWhiteSpace(cell))
            {
                return false;
            }
        }

        return true;
    }

    private static string ToAssetPath(string absolutePath)
    {
        absolutePath = absolutePath.Replace("\\", "/");
        string dataPath = Application.dataPath.Replace("\\", "/");

        if (absolutePath.StartsWith(dataPath, StringComparison.Ordinal))
        {
            return "Assets" + absolutePath.Substring(dataPath.Length);
        }

        return absolutePath;
    }

    private bool HasTargetOrCreatableType()
    {
        return _targetAsset != null || GetSelectedAssetType() != null;
    }

    private Type GetContextAssetType()
    {
        return _targetAsset != null ? _targetAsset.GetType() : GetSelectedAssetType();
    }

    private Type GetSelectedAssetType()
    {
        if (_supportedAssetTypes.Length == 0)
        {
            return null;
        }

        return _supportedAssetTypes[Mathf.Clamp(_selectedAssetTypeIndex, 0, _supportedAssetTypes.Length - 1)];
    }

    private ScriptableObject GetOrCreateTargetAsset()
    {
        if (_targetAsset != null)
        {
            return _targetAsset;
        }

        Type assetType = GetSelectedAssetType();
        if (assetType == null)
        {
            throw new InvalidOperationException("没有可创建的 SO 类型。");
        }

        string assetPath = _createAssetPath;
        if (string.IsNullOrEmpty(assetPath))
        {
            assetPath = GetDefaultAssetPath(assetType);
        }

        if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal) || !assetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("保存路径必须位于 Assets 下，并以 .asset 结尾。");
        }

        ScriptableObject existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
        if (existing != null)
        {
            if (existing.GetType() != assetType)
            {
                throw new InvalidOperationException($"保存路径已存在其他类型资源：{existing.GetType().Name}");
            }

            return existing;
        }

        string directory = Path.GetDirectoryName(assetPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        ScriptableObject asset = CreateInstance(assetType);
        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.ImportAsset(assetPath);
        return asset;
    }

    private static string GetDefaultAssetPath(Type assetType)
    {
        string name = assetType != null ? assetType.Name : "ConfigTable";
        return $"Assets/Resources/ConfigTables/{name}.asset";
    }

    private static bool IsConfigTableAssetType(Type type)
    {
        return type.GetCustomAttribute<ConfigTableAssetAttribute>() != null;
    }

    private class ImportContext
    {
        public ExcelSheet Sheet;
        public List<string> Headers;
        public FieldInfo ListField;
        public Type ElementType;
        public Dictionary<string, FieldInfo> FieldMap;
    }
}
