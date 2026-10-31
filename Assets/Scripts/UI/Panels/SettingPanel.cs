using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : BasePanel
{
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Button backButton;

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

    private void AddAudioCredits()
    {
        var credits=Resources.Load<TextAsset>("Audio/Courier/AudioCredits");
        var label=backButton.GetComponentInChildren<Text>();
        if(credits==null||label==null)return;
        var go=new GameObject("AudioCredits",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));
        go.transform.SetParent(transform,false);
        var text=go.GetComponent<Text>();text.font=label.font;text.fontSize=18;
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

    private void OnDisable() => PlayerPrefs.Save();
}
