using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

public class DialogueTableEditorWindow : EditorWindow
{
    private const string DefaultDialogueTablePath = "Assets/Resources/ConfigTables/DialogueTable.asset";
    private const string DefaultLocalizationTablePath = "Assets/Resources/Data/LocalizationTable.asset";
    private const string DefaultLayoutPath = "Assets/Editor/Config/DialogueEditorLayout.asset";
    private const float NodeWidth = 260f;
    private const float NodeHeight = 132f;
    private const float CanvasWidth = 2200f;
    private const float CanvasHeight = 1400f;

    private DialogueTable _dialogueTable;
    private LocalizationTable _localizationTable;
    private DialogueEditorLayout _layout;
    private Vector2 _groupScroll;
    private Vector2 _lineScroll;
    private Vector2 _detailScroll;
    private Vector2 _graphScroll;
    private Vector2 _validationScroll;
    private string _selectedDialogueId;
    private string _draggingDialogueId;
    private int _selectedLineIndex;
    private string _newDialogueId = "new_dialogue_001";
    private int _viewMode;
    private Vector2 _dragOffset;
    private Vector2 _lastGraphMousePosition;
    private Vector2 _boxSelectStart;
    private Vector2 _boxSelectEnd;
    private Vector2 _lastGraphViewportSize;
    private bool _isBoxSelecting;
    private bool _isPanningGraph;
    private float _graphZoom = 1f;
    private HashSet<string> _selectedDialogueIds = new HashSet<string>();
    private Dictionary<string, Vector2> _dragStartPositions = new Dictionary<string, Vector2>();
    private List<DialogueValidationIssue> _validationIssues = new List<DialogueValidationIssue>();
    private bool _showValidationPanel = true;

    [MenuItem("Tools/Template/Dialogue/Dialogue Editor")]
    public static void ShowWindow()
    {
        GetWindow<DialogueTableEditorWindow>("对话编辑器");
    }

    private void OnEnable()
    {
        LoadDefaultAssets();
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (_dialogueTable == null)
        {
            EditorGUILayout.HelpBox("请选择 DialogueTable 资源。", MessageType.Warning);
            return;
        }

        _viewMode = GUILayout.Toolbar(_viewMode, new[] { "节点视图", "列表视图" }, GUILayout.Height(26));

        if (_viewMode == 0)
        {
            DrawNodeView();
        }
        else
        {
            DrawListView();
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("对话编辑器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("用于直观编辑 DialogueTable。对白正文仍然保存到 LocalizationTable，对话表只保存流程和 key。", MessageType.Info);

        EditorGUI.BeginChangeCheck();
        _dialogueTable = (DialogueTable)EditorGUILayout.ObjectField("DialogueTable", _dialogueTable, typeof(DialogueTable), false);
        _localizationTable = (LocalizationTable)EditorGUILayout.ObjectField("LocalizationTable", _localizationTable, typeof(LocalizationTable), false);
        _layout = (DialogueEditorLayout)EditorGUILayout.ObjectField("节点布局", _layout, typeof(DialogueEditorLayout), false);
        if (EditorGUI.EndChangeCheck())
        {
            EnsureSelection();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("加载默认资源", GUILayout.Height(24)))
        {
            LoadDefaultAssets();
        }

        using (new EditorGUI.DisabledScope(_dialogueTable == null))
        {
            if (GUILayout.Button("保存", GUILayout.Height(24)))
            {
                SaveAssets();
            }

            if (GUILayout.Button("导出 Excel", GUILayout.Height(24)))
            {
                ExportDialogueExcel();
            }

            if (GUILayout.Button("校验", GUILayout.Height(24)))
            {
                ValidateDialogue();
            }
        }

        _showValidationPanel = GUILayout.Toggle(_showValidationPanel, "显示校验", "Button", GUILayout.Height(24), GUILayout.Width(80));

        EditorGUILayout.EndHorizontal();
        DrawExcelSyncStatus();
        EditorGUILayout.EndVertical();
    }

    private void DrawExcelSyncStatus()
    {
        if (_layout == null) return;

        string importText = string.IsNullOrEmpty(_layout.lastImportedExcelPath)
            ? "最近导入：无记录"
            : $"最近导入：{_layout.lastImportedExcelPath}  {_layout.lastImportedAt}";
        string exportText = string.IsNullOrEmpty(_layout.lastExportedExcelPath)
            ? "最近导出：无记录"
            : $"最近导出：{_layout.lastExportedExcelPath}  {_layout.lastExportedAt}";
        string editText = string.IsNullOrEmpty(_layout.lastEditedInEditorAt)
            ? "编辑器修改：无记录"
            : $"编辑器修改：{_layout.lastEditedInEditorAt}";

        EditorGUILayout.LabelField(importText, EditorStyles.miniLabel);
        EditorGUILayout.LabelField(exportText, EditorStyles.miniLabel);
        EditorGUILayout.LabelField(editText, EditorStyles.miniLabel);

        DateTime importedAt;
        DateTime exportedAt;
        DateTime editedAt;
        bool hasImportTime = DateTime.TryParse(_layout.lastImportedAt, out importedAt);
        bool hasExportTime = DateTime.TryParse(_layout.lastExportedAt, out exportedAt);
        bool hasEditTime = DateTime.TryParse(_layout.lastEditedInEditorAt, out editedAt);

        if (hasEditTime && (!hasExportTime || editedAt > exportedAt))
        {
            EditorGUILayout.HelpBox("编辑器中的对话数据有修改尚未导出到 Excel。继续从旧 Excel 导入可能会覆盖这些修改。", MessageType.Warning);
        }
        else if (hasImportTime && (!hasEditTime || importedAt > editedAt))
        {
            EditorGUILayout.HelpBox("当前对话表最近一次来源是 Excel 导入。", MessageType.Info);
        }
    }

    private void DrawListView()
    {
        DrawValidationPanel();
        EditorGUILayout.BeginHorizontal();
        DrawDialogueGroups();
        DrawDialogueLines();
        DrawLineDetail();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawNodeView()
    {
        DrawGraphCanvas();
        DrawValidationPanel();

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        DrawDialogueLines();
        DrawLineDetail();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawGraphCanvas()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Height(430));
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("对话节点", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("整理布局", GUILayout.Width(76)))
        {
            AutoArrangeLayout();
        }
        if (GUILayout.Button("回到选中", GUILayout.Width(76)))
        {
            FocusSelectedNode();
        }
        EditorGUILayout.LabelField("缩放", GUILayout.Width(32));
        _graphZoom = EditorGUILayout.Slider(_graphZoom, 0.5f, 1.6f, GUILayout.Width(150));
        _newDialogueId = EditorGUILayout.TextField(_newDialogueId, GUILayout.Width(190));
        if (GUILayout.Button("新增节点", GUILayout.Width(76)))
        {
            AddDialogueGroup();
        }
        EditorGUILayout.EndHorizontal();

        Rect viewport = GUILayoutUtility.GetRect(10, 380, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        _lastGraphViewportSize = viewport.size;
        Rect canvas = new Rect(0, 0, CanvasWidth * _graphZoom, CanvasHeight * _graphZoom);
        _graphScroll = GUI.BeginScrollView(viewport, _graphScroll, canvas);
        _lastGraphMousePosition = ViewToWorld(Event.current.mousePosition);

        DrawGrid(canvas, 20 * _graphZoom, new Color(0.22f, 0.22f, 0.22f, 0.55f));
        DrawGrid(canvas, 100 * _graphZoom, new Color(0.28f, 0.28f, 0.28f, 0.7f));

        List<string> dialogueIds = GetDialogueIds();
        EnsureLayout(dialogueIds);
        Dictionary<string, Rect> nodeRects = BuildNodeRects(dialogueIds);
        DrawNodeConnections(nodeRects);
        DrawNodes(dialogueIds, nodeRects);
        DrawSelectionBox();
        HandleGraphContextMenu(new Rect(0, 0, CanvasWidth, CanvasHeight));
        HandleGraphZoomAndPan(viewport);
        HandleBoxSelection(nodeRects);
        HandleNodeDragging(nodeRects);

        GUI.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawGrid(Rect canvas, float spacing, Color color)
    {
        Handles.BeginGUI();
        Handles.color = color;

        for (float x = 0; x < canvas.width; x += spacing)
        {
            Handles.DrawLine(new Vector3(x, 0), new Vector3(x, canvas.height));
        }

        for (float y = 0; y < canvas.height; y += spacing)
        {
            Handles.DrawLine(new Vector3(0, y), new Vector3(canvas.width, y));
        }

        Handles.color = Color.white;
        Handles.EndGUI();
    }

    private Dictionary<string, Rect> BuildNodeRects(List<string> dialogueIds)
    {
        Dictionary<string, Rect> rects = new Dictionary<string, Rect>();

        for (int i = 0; i < dialogueIds.Count; i++)
        {
            Vector2 position = GetNodePosition(dialogueIds[i], i);
            rects[dialogueIds[i]] = new Rect(position.x, position.y, NodeWidth, NodeHeight);
        }

        return rects;
    }

    private void DrawNodeConnections(Dictionary<string, Rect> nodeRects)
    {
        if (_dialogueTable == null || _dialogueTable.lines == null) return;

        Handles.BeginGUI();
        foreach (DialogueTableRow row in _dialogueTable.lines)
        {
            if (row == null || row.options == null || !nodeRects.TryGetValue(row.dialogueId, out Rect fromRect)) continue;

            foreach (DialogueTableOptionRow option in row.options)
            {
                if (option == null || string.IsNullOrEmpty(option.nextDialogueId)) continue;
                if (!nodeRects.TryGetValue(option.nextDialogueId, out Rect toRect)) continue;

                Rect scaledFromRect = WorldToViewRect(fromRect);
                Rect scaledToRect = WorldToViewRect(toRect);
                Vector3 start = new Vector3(scaledFromRect.xMax, scaledFromRect.center.y);
                Vector3 end = new Vector3(scaledToRect.xMin, scaledToRect.center.y);
                Vector3 startTangent = start + Vector3.right * 90f;
                Vector3 endTangent = end + Vector3.left * 90f;
                Handles.DrawBezier(start, end, startTangent, endTangent, new Color(0.78f, 0.86f, 1f, 0.9f), null, 3f);
            }
        }

        Handles.EndGUI();
    }

    private void DrawNodes(List<string> dialogueIds, Dictionary<string, Rect> nodeRects)
    {
        foreach (string dialogueId in dialogueIds)
        {
            Rect rect = nodeRects[dialogueId];
            Rect viewRect = WorldToViewRect(rect);
            bool selected = dialogueId == _selectedDialogueId || _selectedDialogueIds.Contains(dialogueId);
            Color previousColor = GUI.color;
            GUI.color = GetNodeColor(dialogueId, selected);
            GUI.Box(viewRect, GUIContent.none);
            GUI.color = previousColor;

            Rect headerRect = new Rect(viewRect.x + 8, viewRect.y + 6, viewRect.width - 16, 22);
            GUI.Label(headerRect, dialogueId, EditorStyles.boldLabel);

            DialogueTableRow firstRow = GetFirstRow(dialogueId);
            string preview = firstRow != null ? GetLocalizedPreview(firstRow.localizationKey) : string.Empty;
            GUI.Label(new Rect(viewRect.x + 8, viewRect.y + 34, viewRect.width - 16, 38), preview, EditorStyles.wordWrappedMiniLabel);
            GUI.Label(new Rect(viewRect.x + 8, viewRect.y + 78, viewRect.width - 16, 18), $"台词：{CountLines(dialogueId)}    分支：{CountOptions(dialogueId)}", EditorStyles.miniLabel);

            if (GUI.Button(new Rect(viewRect.x + 8, viewRect.yMax - 30, viewRect.width - 16, 22), "编辑"))
            {
                SelectDialogue(dialogueId, Event.current.control || Event.current.command || Event.current.shift);
                Repaint();
            }
        }
    }

    private void DrawValidationPanel()
    {
        if (!_showValidationPanel) return;

        if (_validationIssues == null)
        {
            _validationIssues = new List<DialogueValidationIssue>();
        }

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"校验结果：{CountValidationIssues(DialogueValidationSeverity.Error)} 个错误，{CountValidationIssues(DialogueValidationSeverity.Warning)} 个警告", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("重新校验", GUILayout.Width(86)))
        {
            ValidateDialogue();
        }
        EditorGUILayout.EndHorizontal();

        if (_validationIssues.Count == 0)
        {
            EditorGUILayout.HelpBox("暂无问题。", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        _validationScroll = EditorGUILayout.BeginScrollView(_validationScroll, GUILayout.Height(130));
        foreach (DialogueValidationIssue issue in _validationIssues)
        {
            EditorGUILayout.BeginHorizontal();
            MessageType messageType = issue.severity == DialogueValidationSeverity.Error ? MessageType.Error : MessageType.Warning;
            EditorGUILayout.HelpBox(issue.message, messageType);
            if (!string.IsNullOrEmpty(issue.dialogueId) && GUILayout.Button("定位", GUILayout.Width(54), GUILayout.Height(38)))
            {
                SelectIssue(issue);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void HandleNodeDragging(Dictionary<string, Rect> nodeRects)
    {
        Event current = Event.current;
        if (current == null) return;

        if (current.type == EventType.MouseDown && current.button == 0)
        {
            Vector2 worldMouse = ViewToWorld(current.mousePosition);
            foreach (KeyValuePair<string, Rect> pair in nodeRects)
            {
                if (!pair.Value.Contains(worldMouse)) continue;
                Rect editButtonRect = new Rect(pair.Value.x + 8, pair.Value.yMax - 30, pair.Value.width - 16, 22);
                if (editButtonRect.Contains(worldMouse)) continue;

                _draggingDialogueId = pair.Key;
                _dragOffset = worldMouse - pair.Value.position;
                SelectDialogue(pair.Key, current.control || current.command || current.shift);
                CaptureDragStartPositions();
                current.Use();
                Repaint();
                return;
            }
        }

        if (current.type == EventType.MouseDrag && current.button == 0 && !string.IsNullOrEmpty(_draggingDialogueId))
        {
            EnsureLayoutAsset();
            Vector2 nextPosition = ViewToWorld(current.mousePosition) - _dragOffset;
            Vector2 originalPosition = _dragStartPositions.ContainsKey(_draggingDialogueId)
                ? _dragStartPositions[_draggingDialogueId]
                : nextPosition;
            Vector2 delta = nextPosition - originalPosition;
            MoveSelectedNodes(delta);
            EditorUtility.SetDirty(_layout);
            current.Use();
            Repaint();
        }

        if ((current.type == EventType.MouseUp || current.rawType == EventType.MouseUp) && !string.IsNullOrEmpty(_draggingDialogueId))
        {
            _draggingDialogueId = string.Empty;
            _dragStartPositions.Clear();
            SaveLayoutAsset();
            current.Use();
        }
    }

    private void HandleGraphContextMenu(Rect canvas)
    {
        Event current = Event.current;
        if (current == null || current.type != EventType.ContextClick || !canvas.Contains(ViewToWorld(current.mousePosition))) return;

        _lastGraphMousePosition = ViewToWorld(current.mousePosition);
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("新增对话节点"), false, AddDialogueGroupAtMousePosition);
        if (!string.IsNullOrEmpty(_selectedDialogueId))
        {
            menu.AddItem(new GUIContent("删除选中对话组"), false, DeleteSelectedDialogueGroup);
        }
        menu.ShowAsContext();
        current.Use();
    }

    private void DrawSelectionBox()
    {
        if (!_isBoxSelecting) return;

        Rect worldRect = GetSelectionRect();
        Rect viewRect = WorldToViewRect(worldRect);
        Color previousColor = GUI.color;
        GUI.color = new Color(0.45f, 0.68f, 1f, 0.28f);
        GUI.Box(viewRect, GUIContent.none);
        GUI.color = previousColor;
    }

    private void HandleGraphZoomAndPan(Rect viewport)
    {
        Event current = Event.current;
        if (current == null) return;

        if (current.type == EventType.ScrollWheel)
        {
            float previousZoom = _graphZoom;
            Vector2 mouseWorldBeforeZoom = ViewToWorld(current.mousePosition);
            _graphZoom = Mathf.Clamp(_graphZoom - current.delta.y * 0.04f, 0.5f, 1.6f);
            Vector2 mouseViewAfterZoom = WorldToView(mouseWorldBeforeZoom);
            _graphScroll += mouseViewAfterZoom - current.mousePosition;
            _graphScroll.x = Mathf.Max(0f, _graphScroll.x);
            _graphScroll.y = Mathf.Max(0f, _graphScroll.y);
            current.Use();
            if (Math.Abs(previousZoom - _graphZoom) > 0.0001f)
            {
                Repaint();
            }
        }

        if (current.type == EventType.MouseDown && (current.button == 2 || current.button == 1))
        {
            _isPanningGraph = true;
            current.Use();
        }

        if (current.type == EventType.MouseDrag && _isPanningGraph && (current.button == 2 || current.button == 1))
        {
            _graphScroll -= current.delta;
            _graphScroll.x = Mathf.Max(0f, _graphScroll.x);
            _graphScroll.y = Mathf.Max(0f, _graphScroll.y);
            current.Use();
            Repaint();
        }

        if ((current.type == EventType.MouseUp || current.rawType == EventType.MouseUp) && _isPanningGraph)
        {
            _isPanningGraph = false;
            current.Use();
        }
    }

    private void HandleBoxSelection(Dictionary<string, Rect> nodeRects)
    {
        Event current = Event.current;
        if (current == null) return;

        if (current.type == EventType.MouseDown && current.button == 0 && IsMouseOverAnyNode(nodeRects, ViewToWorld(current.mousePosition)) == false)
        {
            _isBoxSelecting = true;
            _boxSelectStart = ViewToWorld(current.mousePosition);
            _boxSelectEnd = _boxSelectStart;
            if (!current.shift && !current.control && !current.command)
            {
                _selectedDialogueIds.Clear();
            }
            current.Use();
        }

        if (current.type == EventType.MouseDrag && current.button == 0 && _isBoxSelecting)
        {
            _boxSelectEnd = ViewToWorld(current.mousePosition);
            current.Use();
            Repaint();
        }

        if ((current.type == EventType.MouseUp || current.rawType == EventType.MouseUp) && _isBoxSelecting)
        {
            Rect selectionRect = GetSelectionRect();
            foreach (KeyValuePair<string, Rect> pair in nodeRects)
            {
                if (selectionRect.Overlaps(pair.Value))
                {
                    _selectedDialogueIds.Add(pair.Key);
                    _selectedDialogueId = pair.Key;
                    _selectedLineIndex = 0;
                }
            }

            _isBoxSelecting = false;
            current.Use();
            Repaint();
        }
    }

    private bool IsMouseOverAnyNode(Dictionary<string, Rect> nodeRects, Vector2 worldMouse)
    {
        foreach (KeyValuePair<string, Rect> pair in nodeRects)
        {
            if (pair.Value.Contains(worldMouse))
            {
                return true;
            }
        }

        return false;
    }

    private Rect GetSelectionRect()
    {
        float xMin = Mathf.Min(_boxSelectStart.x, _boxSelectEnd.x);
        float yMin = Mathf.Min(_boxSelectStart.y, _boxSelectEnd.y);
        float xMax = Mathf.Max(_boxSelectStart.x, _boxSelectEnd.x);
        float yMax = Mathf.Max(_boxSelectStart.y, _boxSelectEnd.y);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private void DrawDialogueGroups()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(230));
        EditorGUILayout.LabelField("对话组", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        _newDialogueId = EditorGUILayout.TextField(_newDialogueId);
        if (GUILayout.Button("新增", GUILayout.Width(56)))
        {
            AddDialogueGroup();
        }
        EditorGUILayout.EndHorizontal();

        _groupScroll = EditorGUILayout.BeginScrollView(_groupScroll);
        foreach (string dialogueId in GetDialogueIds())
        {
            GUIStyle style = dialogueId == _selectedDialogueId ? EditorStyles.toolbarButton : GUI.skin.button;
            if (GUILayout.Button($"{dialogueId} ({CountLines(dialogueId)})", style))
            {
                SelectDialogue(dialogueId, false);
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawDialogueLines()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(260));
        EditorGUILayout.LabelField("台词行", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_selectedDialogueId)))
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("新增行"))
            {
                AddLine();
            }

            if (GUILayout.Button("删除行"))
            {
                DeleteSelectedLine();
            }
            EditorGUILayout.EndHorizontal();
        }

        List<DialogueTableRow> rows = GetSelectedRows();
        _lineScroll = EditorGUILayout.BeginScrollView(_lineScroll);
        for (int i = 0; i < rows.Count; i++)
        {
            DialogueTableRow row = rows[i];
            string textPreview = GetLocalizedPreview(row.localizationKey);
            string label = $"{row.lineIndex}. {SafeText(row.speakerName)} {textPreview}";
            GUIStyle style = i == _selectedLineIndex ? EditorStyles.toolbarButton : GUI.skin.button;
            if (GUILayout.Button(label, style, GUILayout.Height(34)))
            {
                _selectedLineIndex = i;
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawLineDetail()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("行内容", EditorStyles.boldLabel);

        DialogueTableRow row = GetSelectedRow();
        if (row == null)
        {
            EditorGUILayout.HelpBox("请选择或新增一行台词。", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
        EditorGUI.BeginChangeCheck();

        row.dialogueId = EditorGUILayout.TextField("对话 ID", row.dialogueId);
        row.lineIndex = EditorGUILayout.IntField("行号", row.lineIndex);
        row.speakerName = EditorGUILayout.TextField("角色名", row.speakerName);
        row.localizationKey = EditorGUILayout.TextField("文本 Key", row.localizationKey);
        DrawLocalizationEditor(row.localizationKey);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("表现", EditorStyles.boldLabel);
        row.portraitResource = EditorGUILayout.TextField("头像 Resources 路径", row.portraitResource);
        row.standingResource = EditorGUILayout.TextField("立绘 Resources 路径", row.standingResource);
        row.expression = EditorGUILayout.TextField("表情", row.expression);
        row.style = (DialogueBoxStyle)EditorGUILayout.EnumPopup("对话框样式", row.style);
        row.autoPlayDelay = EditorGUILayout.FloatField("自动播放延迟", row.autoPlayDelay);

        DrawOptions(row);

        if (EditorGUI.EndChangeCheck())
        {
            RecordEditorEdit();
            MarkDirty();
            SortLines(row.dialogueId);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawLocalizationEditor(string localizationKey)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("本地化文本", EditorStyles.boldLabel);

        if (_localizationTable == null)
        {
            EditorGUILayout.HelpBox("未选择 LocalizationTable，只能编辑 key，不能直接编辑文本。", MessageType.Warning);
            return;
        }

        if (string.IsNullOrEmpty(localizationKey))
        {
            EditorGUILayout.HelpBox("填写文本 Key 后可在这里编辑各语言文本。", MessageType.Info);
            return;
        }

        foreach (LocalizationLanguage language in _localizationTable.languages)
        {
            if (language == null) continue;
            LocalizationEntry entry = GetOrCreateLocalizationEntry(language, localizationKey);
            EditorGUILayout.LabelField(language.languageCode, EditorStyles.miniBoldLabel);
            entry.value = EditorGUILayout.TextArea(entry.value, GUILayout.MinHeight(46));
            EditorGUILayout.Space(4);
        }
    }

    private void DrawOptions(DialogueTableRow row)
    {
        if (row.options == null)
        {
            row.options = new List<DialogueTableOptionRow>();
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("分支选项", EditorStyles.boldLabel);
        if (GUILayout.Button("添加选项", GUILayout.Width(90)))
        {
            row.options.Add(new DialogueTableOptionRow());
            RecordEditorEdit();
            MarkDirty();
        }
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < row.options.Count; i++)
        {
            DialogueTableOptionRow option = row.options[i];
            if (option == null)
            {
                option = new DialogueTableOptionRow();
                row.options[i] = option;
            }

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"选项 {i + 1}", EditorStyles.boldLabel);
            if (GUILayout.Button("上移", GUILayout.Width(48)) && i > 0)
            {
                Swap(row.options, i, i - 1);
                RecordEditorEdit();
                MarkDirty();
            }
            if (GUILayout.Button("下移", GUILayout.Width(48)) && i < row.options.Count - 1)
            {
                Swap(row.options, i, i + 1);
                RecordEditorEdit();
                MarkDirty();
            }
            if (GUILayout.Button("删除", GUILayout.Width(48)))
            {
                row.options.RemoveAt(i);
                RecordEditorEdit();
                MarkDirty();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            option.localizationKey = EditorGUILayout.TextField("选项文本 Key", option.localizationKey);
            DrawLocalizationEditor(option.localizationKey);
            option.nextDialogueId = EditorGUILayout.TextField("跳转对话 ID", option.nextDialogueId);
            option.condition = EditorGUILayout.TextField("显示条件", option.condition);
            option.effects = EditorGUILayout.TextField("执行效果", option.effects);
            EditorGUILayout.EndVertical();
        }
    }

    private void LoadDefaultAssets()
    {
        _dialogueTable = AssetDatabase.LoadAssetAtPath<DialogueTable>(DefaultDialogueTablePath);
        _localizationTable = AssetDatabase.LoadAssetAtPath<LocalizationTable>(DefaultLocalizationTablePath);
        _layout = AssetDatabase.LoadAssetAtPath<DialogueEditorLayout>(DefaultLayoutPath);
        if (_layout == null)
        {
            EnsureLayoutAsset();
        }

        EnsureSelection();
    }

    private void AddDialogueGroup()
    {
        AddDialogueGroupAtPosition(GetDefaultNewNodePosition());
    }

    private void AddDialogueGroupAtMousePosition()
    {
        AddDialogueGroupAtPosition(_lastGraphMousePosition);
    }

    private void AddDialogueGroupAtPosition(Vector2 nodePosition)
    {
        if (_dialogueTable == null || string.IsNullOrEmpty(_newDialogueId)) return;
        if (_dialogueTable.lines == null)
        {
            _dialogueTable.lines = new List<DialogueTableRow>();
        }

        if (GetDialogueIds().Contains(_newDialogueId))
        {
            _selectedDialogueId = _newDialogueId;
            _selectedLineIndex = 0;
            EnsureLayoutAsset();
            _layout.SetPosition(_newDialogueId, nodePosition);
            SaveLayoutAsset();
            Repaint();
            return;
        }

        Undo.RecordObject(_dialogueTable, "Add Dialogue Group");
        _dialogueTable.lines.Add(new DialogueTableRow
        {
            dialogueId = _newDialogueId,
            lineIndex = 0,
            localizationKey = $"dialogue.{_newDialogueId}.0"
        });

        _selectedDialogueId = _newDialogueId;
        _selectedLineIndex = 0;
        EnsureLayoutAsset();
        _layout.SetPosition(_newDialogueId, nodePosition);
        MarkDirty();
        RecordEditorEdit();
        SaveLayoutAsset();
    }

    private void AddLine()
    {
        if (_dialogueTable == null || string.IsNullOrEmpty(_selectedDialogueId)) return;
        if (_dialogueTable.lines == null)
        {
            _dialogueTable.lines = new List<DialogueTableRow>();
        }

        int nextIndex = GetNextLineIndex(_selectedDialogueId);
        Undo.RecordObject(_dialogueTable, "Add Dialogue Line");
        _dialogueTable.lines.Add(new DialogueTableRow
        {
            dialogueId = _selectedDialogueId,
            lineIndex = nextIndex,
            localizationKey = $"dialogue.{_selectedDialogueId}.{nextIndex}"
        });

        SortLines(_selectedDialogueId);
        _selectedLineIndex = Mathf.Max(0, GetSelectedRows().Count - 1);
        MarkDirty();
        RecordEditorEdit();
    }

    private void DeleteSelectedLine()
    {
        DialogueTableRow row = GetSelectedRow();
        if (row == null) return;

        if (!EditorUtility.DisplayDialog("删除台词行", "确定删除当前台词行吗？本地化文本不会自动删除。", "删除", "取消"))
        {
            return;
        }

        Undo.RecordObject(_dialogueTable, "Delete Dialogue Line");
        _dialogueTable.lines.Remove(row);
        _selectedLineIndex = Mathf.Clamp(_selectedLineIndex, 0, Mathf.Max(0, GetSelectedRows().Count - 1));
        MarkDirty();
        RecordEditorEdit();
    }

    private void DeleteSelectedDialogueGroup()
    {
        if (_dialogueTable == null || string.IsNullOrEmpty(_selectedDialogueId)) return;

        if (!EditorUtility.DisplayDialog("删除对话组", $"确定删除对话组 {_selectedDialogueId} 的所有台词行吗？本地化文本不会自动删除。", "删除", "取消"))
        {
            return;
        }

        Undo.RecordObject(_dialogueTable, "Delete Dialogue Group");
        string deletedDialogueId = _selectedDialogueId;
        _dialogueTable.lines.RemoveAll(row => row != null && row.dialogueId == deletedDialogueId);

        if (_layout != null)
        {
            _layout.nodes.RemoveAll(node => node != null && node.dialogueId == deletedDialogueId);
            EditorUtility.SetDirty(_layout);
            SaveLayoutAsset();
        }

        _selectedDialogueId = string.Empty;
        _selectedDialogueIds.Remove(deletedDialogueId);
        EnsureSelection();
        MarkDirty();
        RecordEditorEdit();
    }

    private void RecordEditorEdit()
    {
        EnsureLayoutAsset();
        _layout.lastEditedInEditorAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        EditorUtility.SetDirty(_layout);
    }

    private void SelectDialogue(string dialogueId, bool additive)
    {
        if (string.IsNullOrEmpty(dialogueId)) return;

        if (!additive)
        {
            _selectedDialogueIds.Clear();
        }

        if (additive && _selectedDialogueIds.Contains(dialogueId))
        {
            _selectedDialogueIds.Remove(dialogueId);
        }
        else
        {
            _selectedDialogueIds.Add(dialogueId);
        }

        _selectedDialogueId = dialogueId;
        _selectedLineIndex = 0;
    }

    private void CaptureDragStartPositions()
    {
        _dragStartPositions.Clear();
        EnsureLayoutAsset();

        if (!_selectedDialogueIds.Contains(_draggingDialogueId))
        {
            _selectedDialogueIds.Clear();
            _selectedDialogueIds.Add(_draggingDialogueId);
        }

        foreach (string dialogueId in _selectedDialogueIds)
        {
            if (_layout.TryGetPosition(dialogueId, out Vector2 position))
            {
                _dragStartPositions[dialogueId] = position;
            }
        }
    }

    private void MoveSelectedNodes(Vector2 delta)
    {
        EnsureLayoutAsset();

        foreach (KeyValuePair<string, Vector2> pair in _dragStartPositions)
        {
            Vector2 nextPosition = pair.Value + delta;
            nextPosition.x = Mathf.Max(16f, nextPosition.x);
            nextPosition.y = Mathf.Max(16f, nextPosition.y);
            _layout.SetPosition(pair.Key, nextPosition);
        }
    }

    private void AutoArrangeLayout()
    {
        EnsureLayoutAsset();
        List<string> dialogueIds = GetDialogueIds();

        for (int i = 0; i < dialogueIds.Count; i++)
        {
            _layout.SetPosition(dialogueIds[i], GetAutoNodePosition(i));
        }

        EditorUtility.SetDirty(_layout);
        SaveLayoutAsset();
        Repaint();
    }

    private void FocusSelectedNode()
    {
        if (string.IsNullOrEmpty(_selectedDialogueId)) return;

        Vector2 position = GetNodePosition(_selectedDialogueId, 0);
        Vector2 nodeCenter = WorldToView(position + new Vector2(NodeWidth * 0.5f, NodeHeight * 0.5f));
        _graphScroll = nodeCenter - _lastGraphViewportSize * 0.5f;
        _graphScroll.x = Mathf.Max(0f, _graphScroll.x);
        _graphScroll.y = Mathf.Max(0f, _graphScroll.y);
        Repaint();
    }

    private void ValidateDialogue()
    {
        _validationIssues = new List<DialogueValidationIssue>();
        if (_dialogueTable == null || _dialogueTable.lines == null)
        {
            AddIssue(DialogueValidationSeverity.Error, string.Empty, -1, "DialogueTable 为空。");
            return;
        }

        HashSet<string> dialogueIds = new HashSet<string>();
        HashSet<string> lineKeys = new HashSet<string>();

        foreach (DialogueTableRow row in _dialogueTable.lines)
        {
            if (row == null)
            {
                AddIssue(DialogueValidationSeverity.Warning, string.Empty, -1, "存在空台词行。");
                continue;
            }

            if (string.IsNullOrEmpty(row.dialogueId))
            {
                AddIssue(DialogueValidationSeverity.Error, string.Empty, row.lineIndex, "存在未填写 dialogueId 的台词行。");
                continue;
            }

            dialogueIds.Add(row.dialogueId);

            string lineKey = $"{row.dialogueId}:{row.lineIndex}";
            if (!lineKeys.Add(lineKey))
            {
                AddIssue(DialogueValidationSeverity.Error, row.dialogueId, row.lineIndex, $"重复的台词行：{row.dialogueId} / lineIndex {row.lineIndex}");
            }

            if (row.lineIndex < 0)
            {
                AddIssue(DialogueValidationSeverity.Error, row.dialogueId, row.lineIndex, $"lineIndex 不能小于 0：{row.dialogueId}");
            }

            ValidateLocalizationKey(row.dialogueId, row.lineIndex, row.localizationKey, "台词文本 key");
            ValidateOptions(row, dialogueIds);
        }

        ValidateOptionTargets(dialogueIds);
        Repaint();
    }

    private void ValidateOptions(DialogueTableRow row, HashSet<string> knownDialogueIds)
    {
        if (row.options == null) return;

        for (int i = 0; i < row.options.Count; i++)
        {
            DialogueTableOptionRow option = row.options[i];
            if (option == null)
            {
                AddIssue(DialogueValidationSeverity.Warning, row.dialogueId, row.lineIndex, $"选项 {i + 1} 为空。");
                continue;
            }

            string optionLabel = $"{row.dialogueId} / line {row.lineIndex} / 选项 {i + 1}";
            ValidateLocalizationKey(row.dialogueId, row.lineIndex, option.localizationKey, $"{optionLabel} 文本 key");
            ValidateConditionSyntax(row.dialogueId, row.lineIndex, option.condition, optionLabel);
            ValidateEffectsSyntax(row.dialogueId, row.lineIndex, option.effects, optionLabel);
        }
    }

    private void ValidateOptionTargets(HashSet<string> dialogueIds)
    {
        foreach (DialogueTableRow row in _dialogueTable.lines)
        {
            if (row == null || row.options == null) continue;

            for (int i = 0; i < row.options.Count; i++)
            {
                DialogueTableOptionRow option = row.options[i];
                if (option == null || string.IsNullOrEmpty(option.nextDialogueId)) continue;
                if (dialogueIds.Contains(option.nextDialogueId)) continue;

                AddIssue(DialogueValidationSeverity.Error, row.dialogueId, row.lineIndex, $"{row.dialogueId} / line {row.lineIndex} / 选项 {i + 1} 跳转到不存在的对话 ID：{option.nextDialogueId}");
            }
        }
    }

    private void ValidateLocalizationKey(string dialogueId, int lineIndex, string key, string label)
    {
        if (string.IsNullOrEmpty(key))
        {
            AddIssue(DialogueValidationSeverity.Error, dialogueId, lineIndex, $"{label} 为空。");
            return;
        }

        if (_localizationTable == null || _localizationTable.languages == null)
        {
            AddIssue(DialogueValidationSeverity.Warning, dialogueId, lineIndex, $"未选择 LocalizationTable，无法校验 {key}。");
            return;
        }

        foreach (LocalizationLanguage language in _localizationTable.languages)
        {
            if (language == null) continue;
            if (!HasLocalizationEntry(language, key))
            {
                AddIssue(DialogueValidationSeverity.Warning, dialogueId, lineIndex, $"{label} 在 {language.languageCode} 中缺少文本：{key}");
            }
        }
    }

    private void ValidateConditionSyntax(string dialogueId, int lineIndex, string condition, string label)
    {
        if (string.IsNullOrWhiteSpace(condition)) return;

        string[] parts = condition.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string rawPart in parts)
        {
            string part = rawPart.Trim();
            if (string.IsNullOrEmpty(part)) continue;
            if (part.StartsWith("!", StringComparison.Ordinal)) part = part.Substring(1);
            if (part.StartsWith("item:", StringComparison.Ordinal) || part.StartsWith("flag:", StringComparison.Ordinal)) continue;
            if (part.StartsWith("quest:", StringComparison.Ordinal))
            {
                if (part.Substring("quest:".Length).Split('=').Length == 2) continue;
                AddIssue(DialogueValidationSeverity.Warning, dialogueId, lineIndex, $"{label} 条件格式可能不正确：{rawPart}");
                continue;
            }

            if (ContainsNumberOperator(part)) continue;
            AddIssue(DialogueValidationSeverity.Warning, dialogueId, lineIndex, $"{label} 条件无法识别，将按 flag 名处理：{rawPart}");
        }
    }

    private void ValidateEffectsSyntax(string dialogueId, int lineIndex, string effects, string label)
    {
        if (string.IsNullOrWhiteSpace(effects)) return;

        string[] parts = effects.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string rawPart in parts)
        {
            string part = rawPart.Trim();
            if (string.IsNullOrEmpty(part)) continue;

            if (part.StartsWith("flag:", StringComparison.Ordinal) ||
                part.StartsWith("unflag:", StringComparison.Ordinal) ||
                part.StartsWith("item:", StringComparison.Ordinal) ||
                part.StartsWith("removeItem:", StringComparison.Ordinal))
            {
                continue;
            }

            if (part.StartsWith("set:", StringComparison.Ordinal) ||
                part.StartsWith("add:", StringComparison.Ordinal) ||
                part.StartsWith("quest:", StringComparison.Ordinal))
            {
                if (part.Contains("=")) continue;
            }

            AddIssue(DialogueValidationSeverity.Warning, dialogueId, lineIndex, $"{label} 效果格式可能不正确：{rawPart}");
        }
    }

    private bool ContainsNumberOperator(string value)
    {
        return value.Contains(">=") || value.Contains("<=") || value.Contains("==") || value.Contains("!=") || value.Contains(">") || value.Contains("<");
    }

    private bool HasLocalizationEntry(LocalizationLanguage language, string key)
    {
        if (language.entries == null) return false;

        foreach (LocalizationEntry entry in language.entries)
        {
            if (entry != null && entry.key == key && !string.IsNullOrEmpty(entry.value))
            {
                return true;
            }
        }

        return false;
    }

    private void AddIssue(DialogueValidationSeverity severity, string dialogueId, int lineIndex, string message)
    {
        _validationIssues.Add(new DialogueValidationIssue
        {
            severity = severity,
            dialogueId = dialogueId,
            lineIndex = lineIndex,
            message = message
        });
    }

    private void SelectIssue(DialogueValidationIssue issue)
    {
        _selectedDialogueId = issue.dialogueId;
        List<DialogueTableRow> rows = GetSelectedRows();
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].lineIndex == issue.lineIndex)
            {
                _selectedLineIndex = i;
                break;
            }
        }

        Repaint();
    }

    private void EnsureSelection()
    {
        List<string> ids = GetDialogueIds();
        if (ids.Count == 0)
        {
            _selectedDialogueId = string.Empty;
            _selectedDialogueIds.Clear();
            _selectedLineIndex = 0;
            return;
        }

        if (string.IsNullOrEmpty(_selectedDialogueId) || !ids.Contains(_selectedDialogueId))
        {
            _selectedDialogueId = ids[0];
            _selectedDialogueIds.Clear();
            _selectedDialogueIds.Add(_selectedDialogueId);
            _selectedLineIndex = 0;
        }
    }

    private List<string> GetDialogueIds()
    {
        List<string> ids = new List<string>();
        if (_dialogueTable == null || _dialogueTable.lines == null) return ids;

        foreach (DialogueTableRow row in _dialogueTable.lines)
        {
            if (row == null || string.IsNullOrEmpty(row.dialogueId) || ids.Contains(row.dialogueId)) continue;
            ids.Add(row.dialogueId);
        }

        ids.Sort(StringComparer.Ordinal);
        return ids;
    }

    private void EnsureLayout(List<string> dialogueIds)
    {
        EnsureLayoutAsset();
        HashSet<string> idSet = new HashSet<string>(dialogueIds);
        _layout.RemoveMissingNodes(idSet);

        for (int i = 0; i < dialogueIds.Count; i++)
        {
            Vector2 existingPosition;
            if (_layout.TryGetPosition(dialogueIds[i], out existingPosition)) continue;
            _layout.SetPosition(dialogueIds[i], GetAutoNodePosition(i));
        }

        EditorUtility.SetDirty(_layout);
    }

    private void EnsureLayoutAsset()
    {
        if (_layout != null) return;

        _layout = AssetDatabase.LoadAssetAtPath<DialogueEditorLayout>(DefaultLayoutPath);
        if (_layout != null) return;

        _layout = CreateInstance<DialogueEditorLayout>();
        AssetDatabase.CreateAsset(_layout, DefaultLayoutPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private Vector2 GetNodePosition(string dialogueId, int index)
    {
        if (_layout != null && _layout.TryGetPosition(dialogueId, out Vector2 position))
        {
            return position;
        }

        return GetAutoNodePosition(index);
    }

    private Vector2 GetAutoNodePosition(int index)
    {
        const float xSpacing = 340f;
        const float ySpacing = 178f;
        int column = index / 5;
        int row = index % 5;
        return new Vector2(40 + column * xSpacing, 48 + row * ySpacing);
    }

    private Vector2 GetDefaultNewNodePosition()
    {
        return ViewToWorld(_graphScroll) + new Vector2(80f, 80f);
    }

    private Rect WorldToViewRect(Rect worldRect)
    {
        return new Rect(
            worldRect.x * _graphZoom,
            worldRect.y * _graphZoom,
            worldRect.width * _graphZoom,
            worldRect.height * _graphZoom);
    }

    private Vector2 WorldToView(Vector2 worldPosition)
    {
        return worldPosition * _graphZoom;
    }

    private Vector2 ViewToWorld(Vector2 viewPosition)
    {
        return viewPosition / Mathf.Max(0.01f, _graphZoom);
    }

    private List<DialogueTableRow> GetSelectedRows()
    {
        List<DialogueTableRow> rows = new List<DialogueTableRow>();
        if (_dialogueTable == null || _dialogueTable.lines == null || string.IsNullOrEmpty(_selectedDialogueId)) return rows;

        foreach (DialogueTableRow row in _dialogueTable.lines)
        {
            if (row != null && row.dialogueId == _selectedDialogueId)
            {
                rows.Add(row);
            }
        }

        rows.Sort((left, right) => left.lineIndex.CompareTo(right.lineIndex));
        return rows;
    }

    private DialogueTableRow GetSelectedRow()
    {
        List<DialogueTableRow> rows = GetSelectedRows();
        if (rows.Count == 0) return null;
        _selectedLineIndex = Mathf.Clamp(_selectedLineIndex, 0, rows.Count - 1);
        return rows[_selectedLineIndex];
    }

    private int CountLines(string dialogueId)
    {
        int count = 0;
        if (_dialogueTable == null || _dialogueTable.lines == null) return count;
        foreach (DialogueTableRow row in _dialogueTable.lines)
        {
            if (row != null && row.dialogueId == dialogueId) count++;
        }

        return count;
    }

    private int CountOptions(string dialogueId)
    {
        int count = 0;
        if (_dialogueTable == null || _dialogueTable.lines == null) return count;

        foreach (DialogueTableRow row in _dialogueTable.lines)
        {
            if (row == null || row.dialogueId != dialogueId || row.options == null) continue;
            count += row.options.Count;
        }

        return count;
    }

    private DialogueTableRow GetFirstRow(string dialogueId)
    {
        DialogueTableRow firstRow = null;
        if (_dialogueTable == null || _dialogueTable.lines == null) return null;

        foreach (DialogueTableRow row in _dialogueTable.lines)
        {
            if (row == null || row.dialogueId != dialogueId) continue;
            if (firstRow == null || row.lineIndex < firstRow.lineIndex)
            {
                firstRow = row;
            }
        }

        return firstRow;
    }

    private Color GetNodeColor(string dialogueId, bool selected)
    {
        DialogueValidationSeverity severity = GetHighestSeverity(dialogueId);
        if (severity == DialogueValidationSeverity.Error)
        {
            return selected ? new Color(1f, 0.52f, 0.52f) : new Color(1f, 0.72f, 0.72f);
        }

        if (severity == DialogueValidationSeverity.Warning)
        {
            return selected ? new Color(1f, 0.82f, 0.42f) : new Color(1f, 0.92f, 0.62f);
        }

        return selected ? new Color(0.55f, 0.72f, 1f) : Color.white;
    }

    private DialogueValidationSeverity GetHighestSeverity(string dialogueId)
    {
        if (_validationIssues == null) return DialogueValidationSeverity.None;

        bool hasWarning = false;
        foreach (DialogueValidationIssue issue in _validationIssues)
        {
            if (issue.dialogueId != dialogueId) continue;
            if (issue.severity == DialogueValidationSeverity.Error) return DialogueValidationSeverity.Error;
            if (issue.severity == DialogueValidationSeverity.Warning) hasWarning = true;
        }

        return hasWarning ? DialogueValidationSeverity.Warning : DialogueValidationSeverity.None;
    }

    private int CountValidationIssues(DialogueValidationSeverity severity)
    {
        if (_validationIssues == null) return 0;

        int count = 0;
        foreach (DialogueValidationIssue issue in _validationIssues)
        {
            if (issue.severity == severity)
            {
                count++;
            }
        }

        return count;
    }

    private int GetNextLineIndex(string dialogueId)
    {
        int max = -1;
        foreach (DialogueTableRow row in GetSelectedRows())
        {
            max = Mathf.Max(max, row.lineIndex);
        }

        return max + 1;
    }

    private void SortLines(string dialogueId)
    {
        if (_dialogueTable == null || _dialogueTable.lines == null) return;
        _dialogueTable.lines.Sort((left, right) =>
        {
            if (left == null && right == null) return 0;
            if (left == null) return 1;
            if (right == null) return -1;

            int idCompare = string.Compare(left.dialogueId, right.dialogueId, StringComparison.Ordinal);
            return idCompare != 0 ? idCompare : left.lineIndex.CompareTo(right.lineIndex);
        });

        _selectedDialogueId = dialogueId;
    }

    private LocalizationEntry GetOrCreateLocalizationEntry(LocalizationLanguage language, string key)
    {
        if (language.entries == null)
        {
            language.entries = new List<LocalizationEntry>();
        }

        foreach (LocalizationEntry entry in language.entries)
        {
            if (entry != null && entry.key == key)
            {
                return entry;
            }
        }

        LocalizationEntry newEntry = new LocalizationEntry { key = key, value = string.Empty };
        language.entries.Add(newEntry);
        MarkDirty();
        return newEntry;
    }

    private string GetLocalizedPreview(string key)
    {
        if (_localizationTable == null || string.IsNullOrEmpty(key)) return key;

        foreach (LocalizationLanguage language in _localizationTable.languages)
        {
            if (language == null || language.languageCode != "zh-CN") continue;
            foreach (LocalizationEntry entry in language.entries)
            {
                if (entry != null && entry.key == key)
                {
                    return Truncate(entry.value, 18);
                }
            }
        }

        return key;
    }

    private void SaveAssets()
    {
        MarkDirty();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void ExportDialogueExcel()
    {
        if (_dialogueTable == null) return;

        string selected = EditorUtility.SaveFilePanel(
            "导出 DialogueTable 为 Excel",
            "Assets/Config/Dialogue",
            "DialogueTable_Export.xlsx",
            "xlsx");

        if (string.IsNullOrEmpty(selected))
        {
            return;
        }

        try
        {
            DialogueExcelUtility.ExportXlsx(_dialogueTable, selected);
            EnsureLayoutAsset();
            _layout.lastExportedExcelPath = ToAssetPath(selected);
            _layout.lastExportedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            EditorUtility.SetDirty(_layout);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("导出完成", $"已导出到：{selected}", "确定");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("导出失败", exception.Message, "确定");
        }
    }

    private void MarkDirty()
    {
        if (_dialogueTable != null) EditorUtility.SetDirty(_dialogueTable);
        if (_localizationTable != null) EditorUtility.SetDirty(_localizationTable);
        if (_layout != null) EditorUtility.SetDirty(_layout);
    }

    private void SaveLayoutAsset()
    {
        if (_layout == null) return;

        EditorUtility.SetDirty(_layout);
        AssetDatabase.SaveAssets();
    }

    private void Swap<T>(List<T> list, int left, int right)
    {
        T item = list[left];
        list[left] = list[right];
        list[right] = item;
    }

    private string SafeText(string value)
    {
        return string.IsNullOrEmpty(value) ? "-" : value;
    }

    private string Truncate(string value, int length)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= length) return value;
        return value.Substring(0, length) + "...";
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

public enum DialogueValidationSeverity
{
    None,
    Warning,
    Error
}

public class DialogueValidationIssue
{
    public DialogueValidationSeverity severity;
    public string dialogueId;
    public int lineIndex;
    public string message;
}
