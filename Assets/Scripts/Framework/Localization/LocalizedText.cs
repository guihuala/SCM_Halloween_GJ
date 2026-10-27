using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string localizationKey;

    private Text _text;

    private void Awake()
    {
        _text = GetComponent<Text>();
    }

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= Refresh;
    }

    public void SetKey(string key)
    {
        localizationKey = key;
        Refresh();
    }

    public void Refresh()
    {
        if (_text == null)
        {
            _text = GetComponent<Text>();
        }

        _text.text = LocalizationManager.Get(localizationKey);
    }
}
