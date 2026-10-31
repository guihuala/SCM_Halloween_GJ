using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PostalLoadingScrollCheck
{
    public static void Run()
    {
        try
        {
            Verify();
            Debug.Log("POSTAL_SCROLL PASS: shader render, seamless wrap, RectMask clipping, material isolation, disable/re-enable");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Verify()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/UI/Materials/PostalLoadingScroll.mat");
        Require(material != null && !ShaderUtil.ShaderHasError(material.shader), "Scroll shader import failed");
        var screen = UnityEngine.Object.Instantiate(Resources.Load<LoadingScreen>("Panel/LoadingScreen"));
        var second = UnityEngine.Object.Instantiate(screen);
        second.gameObject.SetActive(false);
        var scroll = screen.GetComponentInChildren<UIInfiniteScroll>();
        Require(scroll != null, "Loading prefab is not connected to scroll component");
        // Explicit lifecycle calls also support running this check outside Play mode.
        InvokeLifecycle(scroll, "OnDisable");
        InvokeLifecycle(scroll, "OnEnable");
        var image = scroll.GetComponent<Image>();
        Require(image.material != material, "Shared material was used directly");
        var runtime = image.material;
        runtime.SetFloat("_ScrollPhase", 0.25f);
        Require(material.GetFloat("_ScrollPhase") == 0f, "Shared asset phase was modified");
        InvokeLifecycle(scroll, "OnDisable");
        Require(image.material == material, "Disable did not restore source material");
        InvokeLifecycle(scroll, "OnEnable");
        Require(image.material != runtime && image.material.GetFloat("_ScrollPhase") == 0f,
            "Re-enable did not create a fresh animation instance");

        var cameraObject = new GameObject("ScrollCheckCamera", typeof(Camera));
        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 20f;
        var target = new RenderTexture(960, 540, 24);
        camera.targetTexture = target;
        var canvas = screen.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;
        screen.SetAlpha(1f);
        screen.SetProgress(0.65f, true);
        Canvas.ForceUpdateCanvases();
        screen.SetProgress(0.65f, true);
        var a = Capture(camera, target, image, 0f);
        var b = Capture(camera, target, image, 0.25f);
        var wrapped = Capture(camera, target, image, 1f);
        int changed = 0;
        var ac = a.GetPixels32();
        var bc = b.GetPixels32();
        var wc = wrapped.GetPixels32();
        for (int i = 0; i < ac.Length; i++)
        {
            if (Mathf.Abs(ac[i].r - bc[i].r) > 3) changed++;
            Require(Mathf.Abs(ac[i].r - wc[i].r) <= 1, "Animation jumps when wrapping");
        }
        Require(changed > 1000, "Shader did not visibly scroll");
        // Right part of the bar is unrevealed: it must remain the gray track at every phase.
        var rect = image.rectTransform;
        var point = RectTransformUtility.WorldToScreenPoint(camera,
            rect.TransformPoint(new Vector3(rect.rect.xMin + rect.rect.width * 0.9f, 0f, 0f)));
        var outside = a.GetPixel(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y));
        Require(Mathf.Abs(outside.r - outside.g) < 0.01f, "Scrolling pattern leaked outside progress mask");
        File.WriteAllBytes("/private/tmp/postal-loading-scroll-preview.png", b.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(screen.gameObject);
        UnityEngine.Object.DestroyImmediate(second.gameObject);
        UnityEngine.Object.DestroyImmediate(cameraObject);
        target.Release();
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(a);
        UnityEngine.Object.DestroyImmediate(b);
        UnityEngine.Object.DestroyImmediate(wrapped);
    }

    private static void InvokeLifecycle(UIInfiniteScroll scroll, string method)
    {
        typeof(UIInfiniteScroll).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(scroll, null);
    }

    private static Texture2D Capture(Camera camera, RenderTexture target, Image image, float phase)
    {
        image.material.SetFloat("_ScrollPhase", phase);
        image.materialForRendering.SetFloat("_ScrollPhase", phase);
        camera.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        texture.Apply();
        RenderTexture.active = previous;
        return texture;
    }
}
