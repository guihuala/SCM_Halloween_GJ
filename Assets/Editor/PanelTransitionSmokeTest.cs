using System;
using System.Reflection;
using DG.Tweening;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PanelTransitionSmokeTest
{
    public static void Run()
    {
        try
        {
            DOTween.Init();
            foreach (string name in new[] { "PausePanel", "SettingPanel", "ConfirmPanel", "GameResultPanel", "DialoguePanel" })
                Verify(name);
            Debug.Log("PANEL_TRANSITION PASS: backdrop before cards, full-screen root, preserved opacity/scale, input gating, interrupted close");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    private static void Verify(string name)
    {
        var instance = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Panel/" + name));
        var panel = instance.GetComponent<BasePanel>();
        // Initialize the base state without running unrelated panel button setup in edit mode.
        typeof(BasePanel).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(panel, null);
        var group = instance.GetComponent<CanvasGroup>();
        var background = instance.GetComponent<Graphic>();
        float backgroundAlpha = background.color.a;
        var scale = instance.transform.localScale;
        var data = new SerializedObject(panel);
        float backgroundDuration = data.FindProperty("backgroundFadeDuration").floatValue;
        float contentDuration = Mathf.Max(data.FindProperty("fadeInDuration").floatValue, data.FindProperty("scaleDuration").floatValue);
        panel.OpenPanel(name);
        Require(group.blocksRaycasts && !group.interactable, name + ": input not blocked during opening");
        Require(background.color.a == 0f && instance.transform.localScale == scale, name + ": initial backdrop or root scale incorrect");
        DOTween.Goto(panel, backgroundDuration * 0.5f, false);
        Require(background.color.a > 0f && background.color.a < backgroundAlpha, name + ": backdrop did not fade first");
        foreach (Transform child in instance.transform)
            Require(child.GetComponent<CanvasGroup>().alpha == 0f, name + ": content visible before backdrop finished");
        DOTween.Goto(panel, backgroundDuration + contentDuration * 0.5f, false);
        Require(Mathf.Approximately(background.color.a, backgroundAlpha), name + ": backdrop opacity changed");
        foreach (Transform child in instance.transform)
            Require(child.GetComponent<CanvasGroup>().alpha > 0f, name + ": card did not appear");
        DOTween.Complete(panel, true);
        Require(group.interactable && group.blocksRaycasts && instance.transform.localScale == scale, name + ": completed state incorrect");
        panel.OpenPanel(name);
        DOTween.Goto(panel, backgroundDuration * 0.5f, false);
        panel.ClosePanel();
        DOTween.Goto(panel, 0.05f, false);
        Require(!group.interactable && group.alpha < 1f && instance.transform.localScale == scale,
            name + ": interrupted close did not take over the opening animation");
        UnityEngine.Object.DestroyImmediate(instance);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
