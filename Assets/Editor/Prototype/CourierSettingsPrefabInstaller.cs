using NightCourier;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Editor-only authoring. The saved prefab owns every language control and reference.
public static class CourierSettingsPrefabInstaller
{
    private const string Path = "Assets/Resources/Panel/SettingPanel.prefab";

    [MenuItem("Tools/Night Courier/Author Settings Language Row")]
    public static void Install()
    {
        var root = PrefabUtility.LoadPrefabContents(Path);
        try
        {
            var card = root.transform.Find("BG");
            var existing = card.Find("LanguageSelector");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            var title = card.Find("TitleText").GetComponent<Text>();
            title.text = "Audio Settings"; title.fontSize = 52;
            var row = Rect(card, "LanguageSelector", 112, -76, 744, 96);
            var paper = row.gameObject.AddComponent<Image>();
            paper.sprite = Resources.Load<Sprite>("NightVoyage/HUD/Panel");
            paper.type = Image.Type.Sliced; paper.color = CourierColors.Paper; paper.raycastTarget = false;
            Label(row, "LanguageLabel", "Language", -276, 160);
            var previous = Button(row, "PreviousLanguage", -120, "‹", 80);
            var current = Button(row, "CurrentLanguage", 88, "Follow system", 280);
            var next = Button(row, "NextLanguage", 296, "›", 80);
            var settings = new SerializedObject(root.GetComponent<SettingPanel>());
            var sfx = settings.FindProperty("sfxVolumeSlider").objectReferenceValue as Slider;
            var bgm = settings.FindProperty("bgmVolumeSlider").objectReferenceValue as Slider;
            var back = settings.FindProperty("backButton").objectReferenceValue as UnityEngine.UI.Button;
            foreach (var button in new[] { previous, current, next })
            {
                button.gameObject.AddComponent<CourierLanguageSelector>();
                button.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = sfx, selectOnDown = back };
            }
            sfx.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = bgm, selectOnDown = current };
            settings.FindProperty("previousLanguageButton").objectReferenceValue = previous;
            settings.FindProperty("currentLanguageButton").objectReferenceValue = current;
            settings.FindProperty("nextLanguageButton").objectReferenceValue = next;
            settings.FindProperty("languageValue").objectReferenceValue = current.GetComponentInChildren<Text>();
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, Path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }
    public static void InstallAndTest() { Install(); CourierLocalizationSmokeTest.Run(); }
    private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = parent.gameObject.layer; rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
        return rect;
    }
    private static Text Label(Transform parent, string name, string caption, float x, float width)
    {
        var rect = Rect(parent, name, x, 0, width, 72);
        var label = rect.gameObject.AddComponent<Text>();
        label.font = Resources.Load<Font>("NightVoyage/Fonts/ShortStack-Regular");
        label.text = caption; label.fontSize = 32; label.alignment = TextAnchor.MiddleCenter;
        label.color = CourierColors.Night; label.raycastTarget = false;
        return label;
    }
    private static UnityEngine.UI.Button Button(Transform parent, string name, float x, string caption, float width)
    {
        var rect = Rect(parent, name, x, 0, width, 80);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = Resources.Load<Sprite>("NightVoyage/HUD/Button"); image.type = Image.Type.Sliced; image.color = CourierColors.Mint;
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
        Label(rect, "Label", caption, 0, width - 16);
        return button;
    }
}
