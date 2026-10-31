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
            foreach (string name in new[] { "PausePanel", "SettingPanel", "ConfirmPanel", "GameResultPanel", "CreditPanel",
                "Courier/CourierCargoPanel", "Courier/CourierVehiclePanel", "Courier/CourierEventPanel", "Courier/CourierResultPanel", "Courier/CourierRoutePanel", "Courier/CourierTutorialPanel" })
                Verify(name);
            Debug.Log("PANEL_TRANSITION PASS: 11 panels; backdrop before cards, slide/fade, fixed root, input gating, interrupted close/reopen and reduced motion.");
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
        float backgroundAlpha = background!=null?background.color.a:0;
        var scale = instance.transform.localScale;
        var homes = new System.Collections.Generic.Dictionary<RectTransform,Vector2>();
        foreach(Transform child in instance.transform)
            if(child is RectTransform rect)homes.Add(rect,rect.anchoredPosition);
        var data = new SerializedObject(panel);
        float backgroundDuration = background!=null?data.FindProperty("backgroundFadeDuration").floatValue:0;
        float contentDuration = Mathf.Max(data.FindProperty("fadeInDuration").floatValue, data.FindProperty("scaleDuration").floatValue);
        panel.OpenPanel(name);
        bool slides=(bool)typeof(BasePanel).GetProperty("SlideContent",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(panel);
        if(slides)foreach(var home in homes)
            Require(Vector2.Distance(home.Key.anchoredPosition,home.Value+new Vector2(0,-48))<.01f,name+": entrance slide missing");
        Require(group.blocksRaycasts && !group.interactable, name + ": input not blocked during opening");
        Require((background==null||background.color.a == 0f) && instance.transform.localScale == scale, name + ": initial backdrop or root scale incorrect");
        if(backgroundDuration>0)
        {
            Current(panel).Goto(backgroundDuration * 0.5f, false);
            if(backgroundAlpha>0)Require(background.color.a > 0f && background.color.a < backgroundAlpha, name + ": backdrop did not fade first");
            foreach (Transform child in instance.transform)
                Require(child.GetComponent<CanvasGroup>().alpha == 0f, name + ": content visible before backdrop finished");
        }
        Current(panel).Goto(backgroundDuration + contentDuration * 0.5f, false);
        Require(background==null||Mathf.Approximately(background.color.a, backgroundAlpha), name + ": backdrop opacity changed");
        foreach (Transform child in instance.transform)
            Require(child.GetComponent<CanvasGroup>().alpha > 0f, name + ": card did not appear");
        Current(panel).Complete(true);
        Require(group.interactable && group.blocksRaycasts && instance.transform.localScale == scale, name + ": completed state incorrect");
        foreach(var home in homes)Require(Vector2.Distance(home.Key.anchoredPosition,home.Value)<.01f,name+": content did not settle at home");
        panel.ClosePanel();Current(panel).Goto(.09f,false);
        if(slides)foreach(var home in homes)
            Require(home.Key.anchoredPosition.y<home.Value.y&&home.Key.anchoredPosition.y>home.Value.y-48,name+": exit slide missing");
        panel.OpenPanel(name);Current(panel).Complete(true);
        panel.OpenPanel(name);
        Current(panel).Goto(backgroundDuration * 0.5f, false);
        panel.ClosePanel();
        Current(panel).Goto(0.05f, false);
        var closeTween=Current(panel);
        Require(closeTween!=null&&Mathf.Approximately(closeTween.Duration(),data.FindProperty("fadeOutDuration").floatValue),name+": slide and fade are not concurrent");
        Require(!group.interactable && group.alpha < 1f && instance.transform.localScale == scale,
            name + ": interrupted close did not take over the opening animation; input="+group.interactable+", alpha="+group.alpha+", scale="+instance.transform.localScale+", expected="+scale);
        var positions=new System.Collections.Generic.Dictionary<RectTransform,Vector2>();
        foreach(var home in homes)positions.Add(home.Key,home.Key.anchoredPosition);
        panel.OpenPanel(name);
        foreach(var pose in positions)Require(pose.Key.anchoredPosition==pose.Value,name+": reopening snapped content");
        Current(panel).Complete(true);
        Require(group.interactable&&Mathf.Approximately(group.alpha,1),name+": stale close completed after reopen");
        foreach(var home in homes)Require(Vector2.Distance(home.Key.anchoredPosition,home.Value)<.01f,name+": reopen drifted");
        data.Update();data.FindProperty("reducedPanelMotion").boolValue=true;data.ApplyModifiedPropertiesWithoutUndo();
        panel.OpenPanel(name);Current(panel).Complete(true);
        foreach(var home in homes)Require(Vector2.Distance(home.Key.anchoredPosition,home.Value)<.01f,name+": reduced motion moved content");
        UnityEngine.Object.DestroyImmediate(instance);
    }

    // Drive the owned sequence directly: killed sequences remain in the global list until its next update.
    private static Sequence Current(BasePanel panel) { return (Sequence)typeof(BasePanel).GetField("transition",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(panel); }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
