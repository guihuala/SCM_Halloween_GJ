using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MainMenuMotionSmokeTest
{
    private static void Require(bool value, string message)
    { if (!value) throw new Exception(message); }
    private static void Invoke(MainMenuMotion motion, string method)
    { typeof(MainMenuMotion).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(motion, null); }
    private static void Set(MainMenuMotion motion, string field, float value)
    { typeof(MainMenuMotion).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(motion, value); }

    public static void Run()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            var motion = UnityEngine.Object.FindObjectOfType<MainMenuMotion>();
            Require(motion != null && motion.background != null, "Menu atmosphere not wired");
            var source = motion.background.material;
            Require(source.shader.name == "UI/Main Menu Atmosphere" && !ShaderUtil.ShaderHasError(source.shader), "Atmosphere shader failed");
            Invoke(motion, "OnEnable");
            Require(motion.background.material != source, "Material asset mutated directly");
            var canvas = motion.GetComponentInParent<Canvas>();
            var camera = UnityEngine.Object.FindObjectOfType<Camera>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            camera.nearClipPlane = .1f; camera.farClipPlane = 100;
            var target = new RenderTexture(1280, 720, 24); camera.targetTexture = target;
            motion.Evaluate(3f, Vector2.zero);
            var a = Capture(camera, target, "idle-03");
            // Hold every artwork layer still so this comparison measures the shader alone.
            motion.background.material.SetFloat("_AtmosphereTime", 13f);
            var b = Capture(camera, target, "idle-13");
            int changed = 0; var ac = a.GetPixels32(); var bc = b.GetPixels32();
            for (int i = 0; i < ac.Length; i++) if (Mathf.Abs(ac[i].g - bc[i].g) > 2) changed++;
            Require(changed > 1500, "Atmosphere did not visibly animate");
            Require(source.GetFloat("_AtmosphereTime") == 0, "Shared material modified");
            int callbacks = 0;
            motion.BeginExit(() => callbacks++);
            motion.BeginExit(() => callbacks += 100);
            foreach (var layer in motion.layers)
                Require(!layer.group.interactable && !layer.group.blocksRaycasts, "Departure accepts menu input");
            Set(motion, "exitElapsed", motion.exitDuration * .5f); motion.Evaluate(13f, Vector2.zero);
            Capture(camera, target, "departure-half");
            Set(motion, "exitElapsed", motion.exitDuration + 1); Invoke(motion, "Update"); Invoke(motion, "Update");
            Require(callbacks == 1, "Departure callback was lost or repeated");
            foreach (var layer in motion.layers) Require(layer.group.alpha == 0, "Departure left a layer visible");
            Invoke(motion, "OnDisable");
            Require(motion.background.material == source, "Material not restored");
            Invoke(motion, "OnEnable");
            Require(!motion.IsExiting, "Re-enable retained departure state");
            motion.reducedMotion = true; motion.Evaluate(3, Vector2.one);
            Require(motion.background.material.GetFloat("_Strength") == 0, "Reduced motion still animates shader");
            foreach (var layer in motion.layers) Require(layer.rect.anchoredPosition == layer.origin, "Reduced motion moves artwork");
            motion.BeginExit(() => callbacks++); Set(motion, "exitElapsed", .16f); Invoke(motion, "Update");
            Require(callbacks == 2, "Reduced motion transition failed");
            Invoke(motion, "OnDisable"); Invoke(motion, "OnEnable");
            motion.BeginExit(() => callbacks += 100); Invoke(motion, "OnDisable");
            Invoke(motion, "OnEnable"); Set(motion, "exitElapsed", 3); Invoke(motion, "Update");
            Require(callbacks == 2, "Cancelled departure fired a stale callback");
            Invoke(motion, "OnDisable");
            camera.targetTexture = null; target.Release();
            UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b);
            Debug.Log("MAINMENU_MOTION PASS: shader render, animated atmosphere, isolated material, departure input lock, single callback, cancellation, reduced motion, re-enable");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    private static Texture2D Capture(Camera camera, RenderTexture target, string name)
    {
        Canvas.ForceUpdateCanvases(); camera.Render();
        var previous = RenderTexture.active; RenderTexture.active = target;
        var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); texture.Apply();
        Directory.CreateDirectory("/private/tmp/scm-menu-motion-previews");
        File.WriteAllBytes("/private/tmp/scm-menu-motion-previews/" + name + ".png", texture.EncodeToPNG());
        RenderTexture.active = previous; return texture;
    }
}
