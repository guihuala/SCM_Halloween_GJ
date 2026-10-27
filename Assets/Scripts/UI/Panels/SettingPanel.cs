using System.Collections.Generic;
using System;
using SimpleUITips;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : BasePanel
{
    private enum SettingsTab
    {
        Audio,
        Video,
        Input
    }

    [Header("页签")]
    [SerializeField] private Button audioTabButton;
    [SerializeField] private Button videoTabButton;
    [SerializeField] private Button inputTabButton;
    [SerializeField] private GameObject audioPage;
    [SerializeField] private GameObject videoPage;
    [SerializeField] private GameObject inputPage;

    [Header("输入重绑定")]
    [SerializeField] private Button jumpKeyButton;
    [SerializeField] private Button attackKeyButton;
    [SerializeField] private Button interactKeyButton;
    [SerializeField] private Button pauseKeyButton;
    [SerializeField] private Button confirmKeyButton;
    [SerializeField] private Button cancelKeyButton;
    [SerializeField] private Text jumpKeyText;
    [SerializeField] private Text attackKeyText;
    [SerializeField] private Text interactKeyText;
    [SerializeField] private Text pauseKeyText;
    [SerializeField] private Text confirmKeyText;
    [SerializeField] private Text cancelKeyText;

    [Header("通用组件 - 音频")]
    public Slider bgmVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("通用组件 - 视频")]
    public Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;
    public Button chineseLanguageButton;
    public Button englishLanguageButton;

    [Header("通用组件 - 数据")]
    public Button clearDataButton;

    [Header("按钮")]
    public Button backButton;     
    public Button resetButton;    
    
    private Resolution[] _resolutions; // 缓存系统支持的分辨率列表
    private InputActionType? _waitingForInputAction;

    private void Start()
    {
        InitTabs();
        InitAudioSettings();
        InitVideoSettings();
        InitLanguageSettings();
        InitButtons();
        InitInputRebindButtons();
        ShowTab(SettingsTab.Audio);
    }

    private void Update()
    {
        if (!_waitingForInputAction.HasValue) return;

        foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
        {
            if (!Input.GetKeyDown(keyCode)) continue;

            InputManager.Instance.SetKeys(_waitingForInputAction.Value, new[] { keyCode });
            _waitingForInputAction = null;
            RefreshInputLabels();
            return;
        }
    }
    
    private void InitAudioSettings()
    {
        bgmVolumeSlider.value = AudioManager.Instance.bgmVolumeFactor;
        sfxVolumeSlider.value = AudioManager.Instance.sfxVolumeFactor;
        
        bgmVolumeSlider.onValueChanged.AddListener(ChangeBgmVolume);
        sfxVolumeSlider.onValueChanged.AddListener(ChangeSfxVolume);
    }

    // 初始化视频设置逻辑
    private void InitVideoSettings()
    {
        // 1. 设置全屏 Toggle 状态
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        // 2. 设置分辨率 Dropdown
        if (resolutionDropdown != null)
        {
            _resolutions = Screen.resolutions;
            resolutionDropdown.ClearOptions();

            List<string> options = new List<string>();
            int currentResolutionIndex = 0;

            for (int i = 0; i < _resolutions.Length; i++)
            {
                // 构建显示的字符串，例如 "1920 x 1080"
                string option = _resolutions[i].width + " x " + _resolutions[i].height + " @" + _resolutions[i].refreshRate + "Hz";
                options.Add(option);

                // 找到当前屏幕分辨率对应的索引，以便默认选中
                if (_resolutions[i].width == Screen.width &&
                    _resolutions[i].height == Screen.height)
                {
                    currentResolutionIndex = i;
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();

            resolutionDropdown.onValueChanged.AddListener(SetResolution);
        }
    }

    private void InitButtons()
    {
        if(backButton) backButton.onClick.AddListener(OnBackButtonClick);
        if(resetButton) resetButton.onClick.AddListener(OnResetButtonClick);
        
        if(clearDataButton) clearDataButton.onClick.AddListener(OnClearDataClick);
    }

    private void InitLanguageSettings()
    {
        if (chineseLanguageButton != null)
        {
            chineseLanguageButton.onClick.AddListener(() => SetLanguage("zh-CN"));
        }

        if (englishLanguageButton != null)
        {
            englishLanguageButton.onClick.AddListener(() => SetLanguage("en-US"));
        }
    }

    private void InitTabs()
    {
        if (audioTabButton) audioTabButton.onClick.AddListener(() => ShowTab(SettingsTab.Audio));
        if (videoTabButton) videoTabButton.onClick.AddListener(() => ShowTab(SettingsTab.Video));
        if (inputTabButton) inputTabButton.onClick.AddListener(() => ShowTab(SettingsTab.Input));
    }

    private void ShowTab(SettingsTab tab)
    {
        if (audioPage != null) audioPage.SetActive(tab == SettingsTab.Audio);
        if (videoPage != null) videoPage.SetActive(tab == SettingsTab.Video);
        if (inputPage != null) inputPage.SetActive(tab == SettingsTab.Input);

        SetTabInteractable(audioTabButton, tab != SettingsTab.Audio);
        SetTabInteractable(videoTabButton, tab != SettingsTab.Video);
        SetTabInteractable(inputTabButton, tab != SettingsTab.Input);
    }

    private void SetTabInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    private void InitInputRebindButtons()
    {
        if (jumpKeyButton) jumpKeyButton.onClick.AddListener(() => BeginRebind(InputActionType.Jump));
        if (attackKeyButton) attackKeyButton.onClick.AddListener(() => BeginRebind(InputActionType.Attack));
        if (interactKeyButton) interactKeyButton.onClick.AddListener(() => BeginRebind(InputActionType.Interact));
        if (pauseKeyButton) pauseKeyButton.onClick.AddListener(() => BeginRebind(InputActionType.Pause));
        if (confirmKeyButton) confirmKeyButton.onClick.AddListener(() => BeginRebind(InputActionType.Confirm));
        if (cancelKeyButton) cancelKeyButton.onClick.AddListener(() => BeginRebind(InputActionType.Cancel));

        RefreshInputLabels();
    }

    private void BeginRebind(InputActionType action)
    {
        if (InputManager.Instance == null) return;

        _waitingForInputAction = action;
        SetInputLabel(action, LocalizationManager.Get("input.press_key"));
    }

    private void RefreshInputLabels()
    {
        SetInputLabel(InputActionType.Jump, GetInputDisplayName(InputActionType.Jump));
        SetInputLabel(InputActionType.Attack, GetInputDisplayName(InputActionType.Attack));
        SetInputLabel(InputActionType.Interact, GetInputDisplayName(InputActionType.Interact));
        SetInputLabel(InputActionType.Pause, GetInputDisplayName(InputActionType.Pause));
        SetInputLabel(InputActionType.Confirm, GetInputDisplayName(InputActionType.Confirm));
        SetInputLabel(InputActionType.Cancel, GetInputDisplayName(InputActionType.Cancel));
    }

    private string GetInputDisplayName(InputActionType action)
    {
        if (InputManager.Instance == null) return LocalizationManager.Get("common.not_available");

        var keys = InputManager.Instance.GetKeys(action);
        return keys.Count > 0 ? keys[0].ToString() : LocalizationManager.Get("common.none");
    }

    private void SetInputLabel(InputActionType action, string content)
    {
        Text target = null;

        switch (action)
        {
            case InputActionType.Jump:
                target = jumpKeyText;
                break;
            case InputActionType.Attack:
                target = attackKeyText;
                break;
            case InputActionType.Interact:
                target = interactKeyText;
                break;
            case InputActionType.Pause:
                target = pauseKeyText;
                break;
            case InputActionType.Confirm:
                target = confirmKeyText;
                break;
            case InputActionType.Cancel:
                target = cancelKeyText;
                break;
        }

        if (target != null)
        {
            target.text = content;
        }
    }

    #region 视频控制

    public void SetResolution(int resolutionIndex)
    {
        if (_resolutions == null || resolutionIndex >= _resolutions.Length) return;
        
        Resolution resolution = _resolutions[resolutionIndex];
        // 设置分辨率，第三个参数为是否全屏
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        
        Debug.Log($"分辨率设置为: {resolution.width} x {resolution.height}");
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        Debug.Log($"全屏状态: {isFullscreen}");
    }

    public void SetLanguage(string languageCode)
    {
        LocalizationManager.SetLanguage(languageCode);
        Debug.Log($"语言设置为: {languageCode}");
    }

    #endregion

    #region 音量控制

    private void ChangeBgmVolume(float value)
    {
        AudioManager.Instance.ChangeBgmVolume(value);
    }

    private void ChangeSfxVolume(float value)
    {
        AudioManager.Instance.ChangeSfxVolume(value);
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat("MainVolume", AudioManager.Instance.mainVolume);
        PlayerPrefs.SetFloat("BgmVolumeFactor", AudioManager.Instance.bgmVolumeFactor);
        PlayerPrefs.SetFloat("SfxVolumeFactor", AudioManager.Instance.sfxVolumeFactor);
        
        // 注意：分辨率和全屏状态 Unity 会自动保存（在 Windows 注册表中），
        // 但如果需要跨设备同步，你也可以在这里手动保存分辨率 Index。
        
        PlayerPrefs.Save();
        Debug.Log("Settings Saved!");
    }

    #endregion

    #region 按钮回调
    
    private void OnClearDataClick()
    {
        // 1. 清空所有 PlayerPrefs
        PlayerPrefs.DeleteAll(); 
        PlayerPrefs.Save();
        
        Debug.Log("所有存档数据已清空！");

        // 2. 视觉反馈：重置 UI 状态到默认值
        OnResetButtonClick(); 
        
        // 3. 弹出一个飘字提示
        UIHelper.Instance.ShowFixedText(FixedUIPosType.Center, LocalizationManager.Get("settings.data_cleared"), 1.5f);
    }

    private void OnBackButtonClick()
    {
        SaveSettings();
        UIManager.Instance.ClosePanel(panelName);
    }

    private void OnResetButtonClick()
    {
        // 重置 UI 显示
        bgmVolumeSlider.value = 0.8f;
        sfxVolumeSlider.value = 0.8f;

        // 应用到底层逻辑
        AudioManager.Instance.ChangeMainVolume(1f);
        ChangeBgmVolume(0.8f);
        ChangeSfxVolume(0.8f);
    }

    #endregion
}
