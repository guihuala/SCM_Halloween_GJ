using System;
using System.IO;
using NightCourier;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Changes only sprite references and slicing, preserving layout, colors and input.
public static class CourierHudArtInstaller
{
    [MenuItem("Tools/Night Courier/Install HUD Artwork (preserve layouts)")]
    public static void Install()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (var path in Directory.GetFiles("Assets/Resources/Panel/Courier", "*.prefab")) InstallPrefab(path);
        InstallPrefab("Assets/Prefab/UI/CourierCargoRow.prefab");
        AssetDatabase.SaveAssets();
        CourierSharedArtInstaller.Install();
        Debug.Log("COURIER_HUD_ART installed custom HUD artwork; layout and interaction preserved.");
    }

    private static void InstallPrefab(string path)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach (var icon in root.GetComponentsInChildren<CourierIconGraphic>(true))
                if (icon.artwork == null) icon.artwork = Load(icon.kind.ToString());
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                // Preserve user supplied portraits, cargo, vehicles, maps and replacement artwork.
                if (image.sprite != null && !AssetDatabase.GetAssetPath(image.sprite).Contains("unity_builtin_extra")) continue;
                string name = image.name;
                string skin = null;
                if (image.type == Image.Type.Filled) skin = "ProgressFill";
                else if (name.EndsWith("FillBackground", StringComparison.Ordinal)) skin = "ProgressTrack";
                else if (image.GetComponent<Button>() != null) skin = "Button";
                else if (name == "SubtitleBar") skin = "Dialogue";
                else if (name == "PortraitFrame") skin = "PortraitFrame";
                else if (name == "Window") skin = "Panel";
                else if (name == "CourierCargoRow" || name == "CargoDrawer" || name == "GuideStrip" || name == "IconTooltip") skin = "Card";
                if (skin == null) continue;
                image.sprite = Load(skin);
                if (image.type != Image.Type.Filled) image.type = Image.Type.Sliced;
            }
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    private static Sprite Load(string name)
    {
        var sprite = Resources.Load<Sprite>("NightVoyage/HUD/" + name);
        if (sprite == null) throw new InvalidOperationException("Missing HUD artwork: " + name);
        return sprite;
    }
    public static void InstallAndCheck()
    {
        Install();
        foreach (CourierIconKind kind in Enum.GetValues(typeof(CourierIconKind))) Load(kind.ToString());
        foreach (var name in new[] {"Panel","Button","Card","Dialogue","PortraitFrame","ProgressTrack","ProgressFill"}) Load(name);
        foreach (var path in Directory.GetFiles("Assets/Resources/Panel/Courier", "*.prefab"))
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach(var icon in root.GetComponentsInChildren<CourierIconGraphic>(true))
                if(icon.artwork==null) throw new Exception("Unassigned HUD icon: "+path+"/"+icon.name);
            foreach(var image in root.GetComponentsInChildren<Image>(true))
                if(image.sprite!=null && AssetDatabase.GetAssetPath(image.sprite).Contains("unity_builtin_extra"))
                    throw new Exception("Default UI sprite remains: "+path+"/"+image.name);
        }
        Debug.Log("COURIER_HUD_ART PASS: all icon kinds and seven surfaces loaded; serialized icon references and no built-in sprites verified.");
    }
}
