using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ButtonAnimationPlayModeSmokeTest
{
    private const string Active = "ButtonAnimationPlayModeSmokeTest.Active";
    private static int frames;
    private static int step;
    private static float next;

    static ButtonAnimationPlayModeSmokeTest()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += OnLog;
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        SessionState.SetBool(Active, true);
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Active, false) || !EditorApplication.isPlaying || ++frames < 20) return;
        if (Time.realtimeSinceStartup < next) return;
        try
        {
            var group = UnityEngine.Object.FindObjectOfType<HorizontalOrVerticalLayoutGroup>();
            Require(group != null, "Menu layout missing");
            // Batch mode has no rendered canvas; request the layout pass explicitly.
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)group.transform);
            var buttons = group.GetComponentsInChildren<ButtonHoverAnimation>();
            Require(buttons.Length == 3, "Expected three menu buttons");
            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                Require(button.transform.parent.GetComponent<ButtonAnimationLayoutSlot>() != null, "Missing automatic slot: " + button.name);
                var rect = (RectTransform)button.transform;
                Require(Mathf.Abs(rect.rect.width - 300) < 1 && Mathf.Abs(rect.rect.height - 100) < 1, "Incorrect button size: " + button.name + " " + rect.rect);
                if (i > 0) Require(Vector3.Distance(button.transform.parent.localPosition, buttons[i - 1].transform.parent.localPosition) > 50, "Menu buttons overlap");
                var data = new PointerEventData(null);
                if (step == 0) button.OnPointerEnter(data);
                if (step == 1) { button.OnPointerDown(data); button.OnPointerUp(data); button.OnPointerExit(data); }
                if (step == 2) Require(button.transform.localPosition.sqrMagnitude < 0.01f, "Menu button did not reset: " + button.name + " " + button.transform.localPosition);
            }
            if (step++ == 2)
            {
                VerifyInactiveHierarchyStartup();
                Finish(true, "Real menu startup, authored size, hover/reset, inactive horizontal/vertical layout startup");
                return;
            }
            next = Time.realtimeSinceStartup + 0.8f;
        }
        catch (Exception ex) { Finish(false, ex.ToString()); }
    }

    private static void VerifyInactiveHierarchyStartup()
    {
        foreach (bool horizontal in new[] { true, false })
        {
            var row = new GameObject("InactiveLayout", typeof(RectTransform));
            row.SetActive(false);
            var rect = (RectTransform)row.transform;
            rect.sizeDelta = new Vector2(900, 400);
            HorizontalOrVerticalLayoutGroup group = horizontal
                ? (HorizontalOrVerticalLayoutGroup)row.AddComponent<HorizontalLayoutGroup>()
                : row.AddComponent<VerticalLayoutGroup>();
            group.enabled = false;
            group.spacing = 20;
            group.childControlWidth = false;
            group.childControlHeight = false;
            var buttons = new ButtonHoverAnimation[3];
            for (int i = 0; i < buttons.Length; i++)
            {
                var go = new GameObject("Button" + i, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(rect, false);
                ((RectTransform)go.transform).sizeDelta = new Vector2(100, 60);
                buttons[i] = go.AddComponent<ButtonHoverAnimation>();
            }
            row.SetActive(true);
            group.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            for (int i = 0; i < buttons.Length; i++)
            {
                Require(buttons[i].transform.parent.GetComponent<ButtonAnimationLayoutSlot>() != null,
                    "Slot missing after inactive hierarchy startup");
                if (i > 0) Require(Vector3.Distance(buttons[i].transform.parent.localPosition,
                    buttons[i - 1].transform.parent.localPosition) > 60, "Inactive-start layout overlaps");
            }
            UnityEngine.Object.Destroy(row);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        if (SessionState.GetBool(Active, false) && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
            Finish(false, message + "\n" + stack);
    }

    private static void Finish(bool passed, string message)
    {
        SessionState.SetBool(Active, false);
        Debug.Log("BUTTON_PLAYMODE " + (passed ? "PASS " : "FAIL ") + message);
        EditorApplication.Exit(passed ? 0 : 1);
    }
}
