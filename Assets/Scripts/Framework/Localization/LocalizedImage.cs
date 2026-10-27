using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class LocalizedImage : MonoBehaviour
{
    private const string AssetTablePath = "Data/LocalizationAssetTable";

    [SerializeField] private string localizationKey;

    private static LocalizationAssetTable _assetTable;
    private Image _image;

    private void Awake()
    {
        _image = GetComponent<Image>();
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
        if (_image == null)
        {
            _image = GetComponent<Image>();
        }

        if (_assetTable == null)
        {
            _assetTable = Resources.Load<LocalizationAssetTable>(AssetTablePath);
        }

        if (_assetTable == null || string.IsNullOrEmpty(localizationKey))
        {
            return;
        }

        Sprite sprite = _assetTable.GetSprite(localizationKey, LocalizationManager.CurrentLanguage);
        if (sprite != null)
        {
            _image.sprite = sprite;
        }
    }
}
