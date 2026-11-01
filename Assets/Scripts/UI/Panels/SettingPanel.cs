using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : BasePanel
{
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Button backButton;

    [Header("Language — authored in SettingPanel.prefab")]
    [SerializeField] private Button previousLanguageButton;
    [SerializeField] private Button currentLanguageButton;
    [SerializeField] private Button nextLanguageButton;
    [SerializeField] private Text languageValue;

    protected override void Awake()
    {
        base.Awake();
        BindExistingLanguageControls();
        if (previousLanguageButton != null)
            previousLanguageButton.onClick.AddListener(() => NightCourier.CourierLocalization.Cycle(-1));
        if (currentLanguageButton != null)
            currentLanguageButton.onClick.AddListener(() => NightCourier.CourierLocalization.Cycle(1));
        if (nextLanguageButton != null)
            nextLanguageButton.onClick.AddListener(() => NightCourier.CourierLocalization.Cycle(1));
    }

    // Recover references to authored controls without creating or changing the UI.
    [ContextMenu("Bind Existing Language Controls")]
    private void BindExistingLanguageControls()
    {
        var row = transform.Find("BG/LanguageSelector");
        if (row == null) return;
        if (previousLanguageButton == null) previousLanguageButton = row.Find("PreviousLanguage")?.GetComponent<Button>();
        if (currentLanguageButton == null) currentLanguageButton = row.Find("CurrentLanguage")?.GetComponent<Button>();
        if (nextLanguageButton == null) nextLanguageButton = row.Find("NextLanguage")?.GetComponent<Button>();
        if (languageValue == null && currentLanguageButton != null)
            languageValue = currentLanguageButton.GetComponentInChildren<Text>(true);
    }

    private void OnValidate() => BindExistingLanguageControls();

    private void Start()
    {
        bgmVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.bgmVolumeFactor);
        sfxVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.sfxVolumeFactor);
        bgmVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.ChangeBgmVolume);
        sfxVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.ChangeSfxVolume);
        backButton.onClick.AddListener(Back);
        AddAudioCredits();
        bgmVolumeSlider.Select();
    }

    private void OnEnable()
    {
        NightCourier.CourierLocalization.RefreshSystemLanguage();
        NightCourier.CourierLocalization.Changed += RefreshLanguage;
        RefreshLanguage();
    }

    private void RefreshLanguage()
    {
        if (languageValue != null) NightCourier.CourierLocalization.Set(languageValue, NightCourier.CourierLocalization.FollowsSystem ? "Follow system" : NightCourier.CourierLocalization.LanguageName);
    }

    private void AddAudioCredits()
    {
        var credits=Resources.Load<TextAsset>("Audio/Courier/AudioCredits");
        var label=backButton.GetComponentInChildren<Text>();
        if(credits==null||label==null)return;
        var go=new GameObject("AudioCredits",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));
        go.transform.SetParent(transform,false);
        var text=go.GetComponent<Text>();text.font=NightCourier.CourierTypography.Body;text.fontSize=18;
        text.alignment=TextAnchor.LowerCenter;text.color=NightCourier.CourierColors.Muted;text.raycastTarget=false;
        text.text=credits.text;
        var rect=text.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.right;
        rect.pivot=new Vector2(.5f,0);rect.anchoredPosition=new Vector2(0,16);rect.sizeDelta=new Vector2(-64,180);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Back();
    }

    private void Back()
    {
        PlayerPrefs.Save();
        UIManager.Instance.ClosePanel(panelName);
    }

    private void OnDisable()
    {
        NightCourier.CourierLocalization.Changed -= RefreshLanguage;
        PlayerPrefs.Save();
    }
}
