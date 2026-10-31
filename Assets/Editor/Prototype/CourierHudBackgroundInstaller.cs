using System;
using System.IO;
using System.Reflection;
using NightCourier;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CourierHudBackgroundInstaller
{
    private const string PrefabPath = "Assets/Resources/Panel/Courier/CourierHudPanel.prefab";
    private const string MaterialPath = "Assets/Art/UI/Materials/CourierHudPattern.mat";

    [MenuItem("Tools/Night Courier/Install Scrolling HUD Background")]
    public static void Install()
    {
        var shader = Shader.Find("UI/Courier HUD Pattern");
        if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new Exception("HUD pattern shader failed to import.");
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        material.shader = shader;
        var names = new[] { "Ghost", "Moon", "Story", "Box" };
        var slots = new[] { "_GhostTex", "_MoonTex", "_StarTex", "_ParcelTex" };
        for (int i = 0; i < names.Length; i++)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/NightVoyage/HUD/" + names[i] + ".png");
            if (texture == null) throw new Exception("Missing HUD motif: " + names[i]);
            material.SetTexture(slots[i], texture);
        }
        EditorUtility.SetDirty(material);
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var existing = root.transform.Find("ScrollingHudBackground");
            var rect = existing as RectTransform;
            if (rect == null)
            {
                rect = new GameObject("ScrollingHudBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
                rect.SetParent(root.transform, false);
            }
            rect.gameObject.layer = 5;
            rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = rect.GetComponent<Image>();
            image.color = Color.white; image.raycastTarget = false; image.material = material;
            var scroll = rect.GetComponent<UIInfiniteScroll>() ?? rect.gameObject.AddComponent<UIInfiniteScroll>();
            var hud = root.GetComponent<CourierHudPanel>();
            hud.backgroundScroll = scroll; scroll.reducedMotion = hud.reducedMotion;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        Debug.Log("COURIER_HUD_BACKGROUND installed: ghost, moon, sparkle and parcel; seamless diagonal drift.");
    }

    // Render actual UI geometry to check shader output and exact repetition at phase 1.
    [MenuItem("Tools/Night Courier/Check Scrolling HUD Background")]
    public static void InstallAndCheck()
    {
        try
        {
            Install();
            var prefab = AssetDatabase.LoadAssetAtPath<CourierHudPanel>(PrefabPath);
            if (prefab.backgroundScroll == null || prefab.backgroundScroll.GetComponent<Image>().raycastTarget)
                throw new Exception("HUD background is missing or blocks input.");
            if (prefab.backgroundScroll.transform.GetSiblingIndex() != 0)
                throw new Exception("Background must be behind HUD content.");
            var cameraObject = new GameObject("HudPatternCheckCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 540;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.045f,.105f,.12f);
            camera.nearClipPlane = .1f; camera.farClipPlane = 20;
            var target = new RenderTexture(960, 540, 24); camera.targetTexture = target;
            var canvasObject = new GameObject("HudPatternCheckCanvas", typeof(Canvas));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            var imageObject = UnityEngine.Object.Instantiate(prefab.backgroundScroll.gameObject, canvas.transform);
            var image = imageObject.GetComponent<Image>();
            var scroll = imageObject.GetComponent<UIInfiniteScroll>();
            var source = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            // Explicit lifecycle calls permit the same check outside Play mode.
            Invoke(scroll, "OnDisable"); Invoke(scroll, "OnEnable");
            if (image.material == source) throw new Exception("Animation must use a private material.");
            var instance = image.material;
            var a = Capture(camera, target, image, 0);
            var b = Capture(camera, target, image, .25f);
            var c = Capture(camera, target, image, 1);
            var ap = a.GetPixels32(); var bp = b.GetPixels32(); var cp = c.GetPixels32();
            int changed = 0;
            for (int i = 0; i < ap.Length; i++)
            {
                if (Mathf.Abs(ap[i].r - bp[i].r) > 2) changed++;
                if (Mathf.Abs(ap[i].r - cp[i].r) > 1) throw new Exception("Pattern jumps at loop boundary.");
            }
            if (changed < 300) throw new Exception("Pattern did not visibly move: " + changed);
            scroll.reducedMotion = true;
            float phase = instance.GetFloat("_ScrollPhase"); Invoke(scroll, "Update");
            if (instance.GetFloat("_ScrollPhase") != phase) throw new Exception("Reduced motion did not stop drift.");
            if (source.GetFloat("_ScrollPhase") != 0) throw new Exception("Shared material was changed.");
            Invoke(scroll, "OnDisable");
            if (image.material != source) throw new Exception("Material was not restored on disable.");
            File.WriteAllBytes("/private/tmp/courier-hud-pattern-preview.png", b.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(canvasObject); UnityEngine.Object.DestroyImmediate(cameraObject);
            target.Release(); UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b); UnityEngine.Object.DestroyImmediate(c);
            Debug.Log("COURIER_HUD_BACKGROUND PASS: shader render, seamless wrap, visible motion, reduced motion, material isolation and input passthrough.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
    }
    private static void Invoke(UIInfiniteScroll scroll, string method)
    {
        typeof(UIInfiniteScroll).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(scroll, null);
    }
    private static Texture2D Capture(Camera camera, RenderTexture target, Image image, float phase)
    {
        image.material.SetFloat("_ScrollPhase", phase);
        Canvas.ForceUpdateCanvases(); camera.Render();
        var previous = RenderTexture.active; RenderTexture.active = target;
        var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0); texture.Apply();
        RenderTexture.active = previous; return texture;
    }
}
