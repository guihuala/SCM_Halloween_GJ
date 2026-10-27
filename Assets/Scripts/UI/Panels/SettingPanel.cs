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
        bgmVolumeSlider.Select();
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
