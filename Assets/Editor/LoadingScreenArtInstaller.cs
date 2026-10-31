using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Restyles the authored loading prefab while preserving its real progress mask and tape material.
public static class LoadingScreenArtInstaller
{
    private const string Path = "Assets/Resources/Panel/LoadingScreen.prefab";
    private static readonly Color Cream = new Color(.93f, .91f, .79f);
    private static readonly Color Mint = new Color(.64f, .83f, .75f);
    private static Font font;

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        r.gameObject.layer = 5;
        r.SetParent(parent, false);
        Place(r, position, size);
        return r;
    }
    private static void Place(RectTransform r, Vector2 position, Vector2 size)
    {
        r.anchorMin = r.anchorMax = r.pivot = Vector2.one * .5f;
        r.anchoredPosition = position;
        r.sizeDelta = size;
    }
    private static Image Art(string name, Transform parent, string sprite, Vector2 position, Vector2 size, Color color)
    {
        var i = Rect(name, parent, position, size).gameObject.AddComponent<Image>();
        i.sprite = Resources.Load<Sprite>(sprite);
        if (i.sprite == null) throw new Exception("Missing loading artwork: " + sprite);
        i.color = color; i.preserveAspect = true; i.raycastTarget = false;
        return i;
    }
    private static Text Label(string name, Transform parent, string value, Vector2 position, Vector2 size, int points, Color color, TextAnchor align = TextAnchor.MiddleCenter)
    {
        var t = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
        t.font = font; t.text = value; t.fontSize = points; t.color = color;
        t.alignment = align; t.raycastTarget = false;
        return t;
    }
    [MenuItem("Tools/Night Courier/Install Loading Screen Artwork")]
    public static void Install()
    {
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/BarlowCondensed-Light.ttf");
        var root = PrefabUtility.LoadPrefabContents(Path);
        try
        {
            var loading = root.transform.Find("Loading");
            var prior = loading.Find("NightPostalArtwork");
            if (prior != null) UnityEngine.Object.DestroyImmediate(prior.gameObject);
            loading.GetComponent<Image>().color = new Color(.045f, .105f, .12f);
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var art = Rect("NightPostalArtwork", loading, Vector2.zero, new Vector2(1500, 1000));
            // Keep the percentage first in the hierarchy for existing integrations.
            art.SetAsLastSibling();
            Label("Edition", art, "N I G H T   C O U R I E R   /   P O S T A L   S E R V I C E", new Vector2(0, 365), new Vector2(1000, 40), 22, Mint);
            Label("Heading", art, "A little further into the night.", new Vector2(0, 295), new Vector2(1200, 90), 66, Cream);
            Label("Subtitle", art, "Strange roads. Familiar faces. One more delivery.", new Vector2(0, 226), new Vector2(1100, 48), 28, new Color(.6f,.72f,.7f));
            Art("Moon", art, "NightVoyage/HUD/Moon", new Vector2(305, 108), new Vector2(100,100), Cream);
            var horizon = Rect("RoadScene", art, new Vector2(0,-16), new Vector2(850,210));
            Art("Destination", horizon, "NightVoyage/HUD/Home", new Vector2(402,-37), new Vector2(65,65), Mint);
            Art("Parcel", horizon, "NightVoyage/HUD/Box", new Vector2(-402,-42), new Vector2(45,45), Cream);
            for(int n=0;n<19;n++)
                Art("RoadDash"+n, horizon, "NightVoyage/HUD/ProgressFill", new Vector2(-350+n*38,-76), new Vector2(19,4), new Color(.35f,.51f,.49f));
            var truck = Art("Courier", horizon, "NightVoyage/VehicleIcons/old_pickup", new Vector2(-330,4), new Vector2(215,215), Color.white).rectTransform;
            Art("GhostCompanion", art, "NightVoyage/HUD/Ghost", new Vector2(-265,124), new Vector2(38,45), new Color(.45f,.65f,.62f,.65f));
            var progress = loading.Find("Progress").GetComponent<RectTransform>();
            Place(progress, new Vector2(0,-220), new Vector2(820,30));
            progress.GetComponent<Image>().color = new Color(.24f,.24f,.24f);
            progress.GetComponent<Image>().raycastTarget = false;
            var mask = progress.Find("ProgressReveal").GetComponent<RectTransform>();
            mask.sizeDelta = new Vector2(-8,-8);
            var fill = mask.GetComponentInChildren<Image>();
            fill.raycastTarget = false;
            var percent = progress.Find("Text").GetComponent<Text>();
            Place(percent.rectTransform, new Vector2(355,51), new Vector2(110,48));
            percent.fontSize=34;percent.color=Cream;percent.alignment=TextAnchor.MiddleRight;percent.raycastTarget=false;
            var status = Label("Status", art, "Preparing your next stop...", new Vector2(-65,-169), new Vector2(690,48), 30, Cream, TextAnchor.MiddleLeft);
            Label("TipLabel", art, "C O U R I E R ' S   N O T E", new Vector2(0,-312), new Vector2(900,32), 20, Mint);
            Label("Tip", art, "Pick up a batch at its sender. Deliver it together at the next stop.", new Vector2(0,-353), new Vector2(1120,44), 27, new Color(.68f,.77f,.73f));
            Label("Footer", art, "HANDLE WITH CARE  /  EVEN AFTER DARK", new Vector2(0,-443), new Vector2(1000,30), 19, new Color(.39f,.56f,.54f));
            var data = new SerializedObject(root.GetComponent<LoadingScreen>());
            data.FindProperty("courier").objectReferenceValue=truck;
            data.FindProperty("statusText").objectReferenceValue=status;
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,Path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }

    public static void InstallAndCheck()
    {
        try
        {
            Install();
            // Existing regression check also renders the newly styled screen.
            PostalLoadingScrollCheck.Run();
        }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
