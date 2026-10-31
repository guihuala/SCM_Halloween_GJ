using System;
using System.IO;
using NightCourier;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class CourierCharacterInstaller
{
    public const string CatalogPath = "Assets/Resources/NightVoyage/Characters.asset";
    private const string PortraitFolder = "Assets/Art/Sprite/Character/Portrait/";
    private const string HudPath = "Assets/Resources/Panel/Courier/CourierHudPanel.prefab";

    [MenuItem("Tools/Night Courier/Install Character Portraits (preserve settings)")]
    public static void Install()
    {
        AssetDatabase.Refresh();
        string[] ids = { "drow", "werewolf", "bear", "vampire", "zombie", "witch" };
        string[] names = { "Drow", "Wolfwoman", "Little Werebear", "Vampire", "Immigrant Jiangshi", "Witch" };
        string[] personalities = { "内向嘴拙妹", "豪爽大姐姐", "嘟嘟囔囔、多愁善感的小妹妹", "知心姐姐，曾是播音员的电台DJ", "呆萌的移民老太太", "控场的妈妈" };
        string[] colors = { "#B49DE4", "#E4AE73", "#D6BD78", "#DC8FA1", "#98C1AE", "#A8B3DA" };
        foreach (var id in ids) ImportPortrait(id);
        if(File.Exists(PortraitPath("unknown")))ImportPortrait("unknown");
        var catalog = AssetDatabase.LoadAssetAtPath<CourierCharacterCatalog>(CatalogPath);
        // Reinstalling the UI must never overwrite a designer's portraits or character settings.
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<CourierCharacterCatalog>();
            catalog.fallbackPortrait = Sprite("unknown");
            catalog.characters = new CourierCharacter[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                Color accent; ColorUtility.TryParseHtmlString(colors[i], out accent);
                catalog.characters[i] = new CourierCharacter { stopId = i, id = ids[i], displayName = names[i], personality = personalities[i], portrait = Sprite(ids[i]), accent = accent };
            }
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
        var root = PrefabUtility.LoadPrefabContents(HudPath);
        try
        {
            var hud = root.GetComponent<CourierHudPanel>();
            if (hud.characterCatalog == null) hud.characterCatalog = catalog;
            var parent = hud.subtitleText.transform.parent;
            if (hud.portraitFrame == null)
                hud.portraitFrame = AddImage(parent, "SpeakerAccent", 32, 846, 3, 120);
            if (hud.speakerPortrait == null)
            {
                hud.speakerPortrait = AddImage(parent, "SpeakerPortrait", 32, 635, 360, 360);
                hud.speakerPortrait.preserveAspect = true;
                hud.speakerPortrait.sprite = catalog.PortraitFor(catalog.ForStop(0));
            }
            if (hud.speakerName == null)
            {
                var rect = Element(parent, "SpeakerName", 416, 827, 1224, 36);
                hud.speakerName = rect.gameObject.AddComponent<Text>();
                hud.speakerName.font = hud.subtitleText.font;
                hud.speakerName.fontSize = 28;
                hud.speakerName.alignment = TextAnchor.MiddleLeft;
                hud.speakerName.raycastTarget = false;
                hud.speakerName.text = "Drow";
            }
            PrefabUtility.SaveAsPrefabAsset(root, HudPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        Debug.Log("COURIER_CHARACTERS INSTALLED: six configurable portraits, names, personalities and fallback; existing settings preserved.");
    }

    private static string PortraitPath(string id)
    {
        if (id == "bear") return PortraitFolder + "gameJam2026_LittleWerebear.png";
        if (id == "zombie") return PortraitFolder + "gameJam2026_jiangshiGranny_02.png";
        return PortraitFolder + ((id=="drow"||id=="vampire"||id=="werewolf")?"gamejam_portrait_":"") + id + ".png";
    }
    private static Sprite Sprite(string id) { return AssetDatabase.LoadAssetAtPath<Sprite>(PortraitPath(id)); }
    private static void ImportPortrait(string id)
    {
        string path = PortraitPath(id);
        if (!File.Exists(path)) throw new InvalidOperationException("Missing placeholder portrait: " + path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 512;
        importer.SaveAndReimport();
    }
    private static RectTransform Element(Transform parent, string name, float x, float y, float w, float h)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(w, h);
        return rect;
    }
    private static Image AddImage(Transform parent, string name, float x, float y, float w, float h)
    {
        var image = Element(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
        image.raycastTarget = false;
        return image;
    }
    public static void InstallAndCheck() { Install(); CourierCharacterSmokeTest.Run(); }
}
