using System;
using DG.Tweening;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// -executeMethod UILoadingAnimationSmokeTest.Run in an isolated project copy.
public static class UILoadingAnimationSmokeTest
{
    public static void Run()
    {
        try
        {
            VerifyPrefab();
            VerifyLayout();
            Debug.Log("UI_REGRESSION PASS: loading prefab, horizontal layout, all animation modes, resize, disable/re-enable");
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

    private static void VerifyPrefab()
    {
        var manager = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/-----MANAGER-----.prefab");
        Require(manager.GetComponentInChildren<Canvas>(true) == null, "Manager still embeds the loading canvas");
        var loader = new SerializedObject(manager.GetComponent<SceneLoader>());
        var path = loader.FindProperty("loadingScreenResourcePath").stringValue;
        Require(path == "Panel/LoadingScreen", "Loading resource path incorrect");
        var prefab = Resources.Load<GameObject>(path)?.GetComponent<LoadingScreen>();
        Require(prefab != null, "Loading resource prefab missing");
        var screen = UnityEngine.Object.Instantiate(prefab);
        screen.SetAlpha(0f);
        screen.SetProgress(0f);
        var group = screen.GetComponentInChildren<CanvasGroup>();
        Require(group.alpha == 0f && !group.blocksRaycasts, "Hidden overlay intercepts input");
        screen.SetAlpha(1f);
        screen.SetProgress(0.5f, true);
        Require(group.alpha == 1f && group.blocksRaycasts, "Visible overlay does not block input");
        Require(screen.GetComponentInChildren<Text>().text == "50%", "Progress text incorrect");
        var data = new SerializedObject(screen);
        var mask = data.FindProperty("progressMask").objectReferenceValue as RectMask2D;
        Require(mask != null, "Progress mask missing");
        var bar = mask.GetComponentInChildren<Image>();
        Require(bar.type == Image.Type.Sliced && bar.sprite != null && bar.sprite.border.sqrMagnitude > 0f,
            "Progress image lost nine-slice settings");
        var rootRect = (RectTransform)screen.transform;
        rootRect.localScale = Vector3.one;
        rootRect.sizeDelta = new Vector2(1920, 1080);
        var imageRect = bar.rectTransform;
        var imageScale = imageRect.localScale;
        var imageSize = imageRect.sizeDelta;
        foreach (float value in new[] { -1f, 0f, 0.01f, 0.5f, 1f, 2f })
        {
            screen.SetProgress(value, true);
            float expected = Mathf.Clamp01(value);
            Require(Mathf.Approximately(mask.padding.z, mask.canvasRect.width * (1f - expected)),
                "Incorrect horizontal reveal at " + value);
            Require(imageRect.sizeDelta == imageSize && imageRect.localScale == imageScale && bar.type == Image.Type.Sliced,
                "Progress animation changed image geometry or slicing");
        }
        mask.transform.parent.localScale = new Vector3(0.65f, 1.5f, 1f);
        rootRect.sizeDelta = new Vector2(2560, 1440);
        screen.SetProgress(0.5f, true);
        Require(mask.canvasRect.width > 0f && Mathf.Approximately(mask.padding.z, mask.canvasRect.width * 0.5f),
            "Resize or non-uniform parent scaling broke clipping");
        screen.SetProgress(0f, true);
        screen.SetProgress(1f);
        Require(!screen.IsProgressComplete, "Progress animation skipped interpolation");
        UnityEngine.Object.DestroyImmediate(screen.gameObject);
    }

    private static void VerifyLayout()
    {
        DOTween.Init();
        var canvas = new GameObject("TestCanvas", typeof(RectTransform), typeof(Canvas));
        var row = new GameObject("Horizontal", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(canvas.transform, false);
        var rect = (RectTransform)row.transform;
        rect.sizeDelta = new Vector2(600, 100);
        var group = row.GetComponent<HorizontalLayoutGroup>();
        group.spacing = 20f;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = true;
        var buttons = new ButtonHoverAnimation[3];
        for (int i = 0; i < buttons.Length; i++)
        {
            var go = new GameObject("Button" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(row.transform, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(100, 40);
            go.GetComponent<LayoutElement>().preferredWidth = 100;
            go.GetComponent<LayoutElement>().minWidth = 50;
            buttons[i] = go.AddComponent<ButtonHoverAnimation>();
            // Edit-mode AddComponent does not invoke Awake; run lifecycle explicitly.
            go.SendMessage("Awake");
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        for (int i = 1; i < buttons.Length; i++)
            Require(buttons[i].transform.position.x > buttons[i - 1].transform.position.x + 100, "Buttons overlap before animation");
        var eventData = new PointerEventData(null);
        foreach (AnimationType mode in Enum.GetValues(typeof(AnimationType)))
        {
            foreach (var button in buttons)
            {
                button.animationType = mode;
                button.enableShake = true;
                Vector3 slotPosition = button.transform.parent.localPosition;
                button.OnPointerEnter(eventData);
                DOTween.CompleteAll();
                button.OnPointerDown(eventData);
                button.OnPointerUp(eventData);
                button.OnPointerExit(eventData);
                DOTween.CompleteAll();
                Require(button.transform.localPosition.sqrMagnitude < 0.001f, "Button failed to reset in " + mode + ": " + button.transform.localPosition);
                Require(button.transform.parent.localPosition == slotPosition, "Animation moved the layout slot");
                button.gameObject.SetActive(false);
                button.SendMessage("OnDisable");
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                button.gameObject.SetActive(true);
                button.SendMessage("OnEnable");
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            }
        }
        rect.sizeDelta = new Vector2(900, 100);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        for (int i = 1; i < buttons.Length; i++)
            Require(buttons[i].transform.position.x > buttons[i - 1].transform.position.x + 200, "Resize broke button layout");
        DOTween.KillAll();
        UnityEngine.Object.DestroyImmediate(canvas);
    }
}
