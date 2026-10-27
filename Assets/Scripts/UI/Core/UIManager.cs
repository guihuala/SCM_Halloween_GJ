using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UIPanelLayer
{
    Bottom = 0,
    Normal = 100,
    Popup = 200,
    Top = 300
}

public class UIManager : SingletonPersistent<UIManager>
{
    // <面板名称, 面板预制体路径>
    private Dictionary<string, string> _panelPathDict;
    // 缓存的面板预制体 <面板名称, 面板预制体>
    private Dictionary<string, GameObject> _uiPrefabDict;
    // 当前已打开的面板实例 <面板名称, 面板实例>
    private Dictionary<string, BasePanel> _panelDict;
    // 父子页面关系 <父面板名称, 子面板名称列表>
    private Dictionary<string, List<string>> _childPanelDict;
    // 子面板对应的父面板 <子面板名称, 父面板名称>
    private Dictionary<string, string> _parentPanelDict;
    // UI 面板的根节点
    private Transform _uiRoot;
    public Transform UIRoot
    {
        get
        {
            if (_uiRoot == null)
            {
                _uiRoot = GameObject.Find("Canvas").transform;
            }
            return _uiRoot;
        }
    }

    public UIDatas uiDatas;

    protected override void Awake()
    {
        base.Awake();
        InitDicts();
    }

    // 初始化字典
    private void InitDicts()
    {
        _panelPathDict = new Dictionary<string, string>();

        foreach (var data in uiDatas.uiDataList)
        {
            _panelPathDict.Add(data.uiName, data.uiPath);
        }

        _uiPrefabDict = new Dictionary<string, GameObject>();
        _panelDict = new Dictionary<string, BasePanel>();
        _childPanelDict = new Dictionary<string, List<string>>();
        _parentPanelDict = new Dictionary<string, string>();
    }

    /// <summary>
    /// 打开UI面板，外部直接调用此方法
    /// </summary>
    /// <param name="name">面板名称</param>
    /// <returns>打开的UI面板脚本</returns>
    public BasePanel OpenPanel(string name)
    {
        return OpenPanel(name, null, UIPanelLayer.Normal);
    }

    public ConfirmPanel OpenConfirm(string title, string message, Action onConfirm, Action onCancel = null, string confirmLabel = null, string cancelLabel = null)
    {
        ConfirmPanel confirmPanel = OpenPanel("ConfirmPanel", null, UIPanelLayer.Top) as ConfirmPanel;
        if (confirmPanel != null)
        {
            confirmPanel.Configure(title, message, onConfirm, onCancel, confirmLabel, cancelLabel);
        }

        return confirmPanel;
    }

    /// <summary>
    /// 打开UI面板，可指定父面板和显示层级
    /// </summary>
    public BasePanel OpenPanel(string name, string parentPanelName, UIPanelLayer layer = UIPanelLayer.Normal)
    {
        BasePanel panel = null;

        // 检查面板是否已经打开
        if (_panelDict.TryGetValue(name, out panel))
        {
            panel.transform.SetAsLastSibling();
            Debug.LogWarning($"面板 {name} 已经打开");
            return panel;
        }

        // 检查面板路径是否存在于路径字典中
        string path = "";
        if (!_panelPathDict.TryGetValue(name, out path))
        {
            Debug.LogWarning($"面板 {name} 的路径不存在");
            return null;
        }

        // 从缓存中获取面板预制体
        GameObject panelPrefab = null;
        if (!_uiPrefabDict.TryGetValue(name, out panelPrefab))
        {
            string prefabPath = path;

            panelPrefab = Resources.Load<GameObject>(prefabPath);

            if (panelPrefab == null)
            {
                Debug.LogError($"面板 {name} 的预制体未找到：{prefabPath}");
                return null;
            }

            _uiPrefabDict.Add(name, panelPrefab);
        }

        Transform parent = GetPanelParent(parentPanelName);

        // 实例化面板并将其挂载到指定父节点，默认挂到 UIRoot
        GameObject panelObj = Instantiate(panelPrefab, parent, false);
        panelObj.transform.SetSiblingIndex(GetLayerSiblingIndex(parent, layer));
        panel = panelObj.GetComponent<BasePanel>();

        if (panel == null)
        {
            Debug.LogError($"面板 {name} 的脚本未挂载或未继承 BasePanel");
            Destroy(panelObj);
            return null;
        }

        panel.OpenPanel(name);
        _panelDict.Add(name, panel);
        RegisterParentChild(name, parentPanelName);

        return panel;
    }

    /// <summary>
    /// 关闭UI面板，外部直接调用此方法
    /// </summary>
    /// <param name="name">面板名称</param>
    /// <returns>是否关闭成功</returns>
    public bool ClosePanel(string name)
    {
        BasePanel panel = null;

        // 检查面板是否已经打开，未打开则无法关闭
        if (!_panelDict.TryGetValue(name, out panel))
        {
            Debug.LogWarning($"面板 {name} 当前未打开，无法关闭");
            return false;
        }

        CloseChildPanels(name);
        
        _panelDict.Remove(name);
        UnregisterParentChild(name);
        panel.ClosePanel();
        
        return true;
    }

    public bool IsPanelOpen(string name)
    {
        return _panelDict.ContainsKey(name);
    }

    public BasePanel GetPanel(string name)
    {
        _panelDict.TryGetValue(name, out var panel);
        return panel;
    }

    private Transform GetPanelParent(string parentPanelName)
    {
        if (string.IsNullOrEmpty(parentPanelName))
        {
            return UIRoot;
        }

        if (_panelDict.TryGetValue(parentPanelName, out var parentPanel))
        {
            return parentPanel.transform;
        }

        Debug.LogWarning($"父面板 {parentPanelName} 未打开，{parentPanelName} 的子面板将挂载到 UIRoot");
        return UIRoot;
    }

    private int GetLayerSiblingIndex(Transform parent, UIPanelLayer layer)
    {
        int childCount = parent != null ? parent.childCount : UIRoot.childCount;
        return Mathf.Clamp((int)layer, 0, childCount);
    }

    private void RegisterParentChild(string panelName, string parentPanelName)
    {
        if (string.IsNullOrEmpty(parentPanelName)) return;

        _parentPanelDict[panelName] = parentPanelName;

        if (!_childPanelDict.TryGetValue(parentPanelName, out var children))
        {
            children = new List<string>();
            _childPanelDict[parentPanelName] = children;
        }

        if (!children.Contains(panelName))
        {
            children.Add(panelName);
        }
    }

    private void UnregisterParentChild(string panelName)
    {
        if (_parentPanelDict.TryGetValue(panelName, out var parentPanelName))
        {
            if (_childPanelDict.TryGetValue(parentPanelName, out var children))
            {
                children.Remove(panelName);
            }

            _parentPanelDict.Remove(panelName);
        }

        _childPanelDict.Remove(panelName);
    }

    private void CloseChildPanels(string panelName)
    {
        if (!_childPanelDict.TryGetValue(panelName, out var children)) return;

        for (int i = children.Count - 1; i >= 0; i--)
        {
            ClosePanel(children[i]);
        }
    }
}
