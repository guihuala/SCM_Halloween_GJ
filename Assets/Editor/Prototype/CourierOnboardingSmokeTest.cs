using System;
using System.IO;
using System.Reflection;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

[InitializeOnLoad]
public static class CourierOnboardingSmokeTest
{
    private const string Flag = "/tmp/courier-onboarding-run", Result = "/tmp/courier-onboarding-result.txt", Key = "CourierOnboarding.Active";
    private static int phase;
    private static double next, deadline;
    static CourierOnboardingSmokeTest() { EditorApplication.update += Tick; }
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Run()
    {
        File.WriteAllText(Result, "Running");
        SessionState.SetString(Key + "Scene", EditorSceneManager.GetActiveScene().path);
        EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
        SessionState.SetBool(Key, true); phase = 0; deadline = 0;
        Application.runInBackground = true; EditorApplication.isPlaying = true;
    }
    private static void Tick()
    {
        if (File.Exists(Flag) && !EditorApplication.isPlaying && !EditorApplication.isCompiling)
        { File.Delete(Flag); Run(); }
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next) return;
        Application.runInBackground = true;
        if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 90;
        next = EditorApplication.timeSinceStartup + 1;
        try
        {
            Check(EditorApplication.timeSinceStartup < deadline, "Onboarding timed out");
            var owner = NightCourierPrototype.Current;
            if (owner == null || !owner.Active) return;
            var panel = UIManager.Instance.GetPanel("CourierTutorialPanel") as CourierTutorialPanel;
            Check(panel != null, "Tutorial panel missing");
            var view = panel.view;
            switch (phase++)
            {
                case 0:
                    Debug.Log(CourierChecks.RunAll());
                    Check(owner.Run.Node == CourierStreetMap.Node("S").StopId && owner.ConversationActive && owner.DialogueSpeaker == "Gooy", "Opening letter missing at depot");
                    Check(!owner.DepartTo(0), "Opening letter allowed departure");
                    int dialogueGuard = 0;
                    while (owner.ConversationActive)
                    {
                        owner.AdvanceDialogue();
                        Check(++dialogueGuard < 500, "Opening letter stuck");
                    }
                    Check(owner.Run.Story.WasPlayed("gooy_letter") && !owner.Run.Story.HasPending(owner.Run), "Opening letter repeated");
                    owner.enabled = false;
                    typeof(CourierLocalization).GetField("language", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, CourierLanguage.TraditionalChinese);
                    view.Refresh();
                    Check(view.instruction.enabled && !view.instruction.text.Contains("Tab"), "Next action hidden");
                    Check(!owner.GuideOpen, "Start blocked by rules");
                    Capture("/tmp/courier-onboarding-start.png", view);
                    break;
                case 1:
                    Check(view.instruction.preferredHeight <= view.instruction.rectTransform.rect.height + 1, "Opening text clipped");
                    var mapPage=UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
                    var motion=mapPage.GetComponentInChildren<CourierMapMotion>(true);
                    Check(motion.TutorialTarget==mapPage.mapArtwork.landmarkImages[0],"Tunnel is not the tutorial target");
                    Check(System.Array.TrueForAll(view.focusFrames,f=>!f.gameObject.activeSelf),"Legacy focus rectangle visible");
                    var glow=mapPage.mapArtwork.landmarkImages[0].transform.parent.GetComponentsInChildren<CourierLandmarkGlow>();
                    Check(System.Array.Exists(glow,g=>g.TutorialHighlighted&&g.source==motion.TutorialTarget&&g.material.shader.name=="UI/Courier Landmark Glow"),"Tunnel material highlight missing");
                    Canvas.ForceUpdateCanvases();
                    var hit=mapPage.mapArtwork.landmarkHitAreas[0].rectTransform;
                    var canvas=hit.GetComponentInParent<Canvas>().rootCanvas;
                    var data=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,
                        position=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,hit.TransformPoint(hit.rect.center))};
                    var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
                    Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==mapPage.stopButtons[0],
                        "Tunnel pointer blocked: "+string.Join(", ",hits.Select(h=>h.gameObject.name)));
                    ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerClickHandler);view.Refresh();
                    Capture("/tmp/courier-onboarding-depart.png",view);
                    Check(motion.TutorialTarget==mapPage.departButton.targetGraphic,"Departure action not highlighted");
                    view.skipButton.onClick.Invoke();view.Refresh();
                    Check(view.IsCollapsed && !view.instruction.enabled && view.strip.rect.height == 76, "Task did not collapse");
                    Check(!owner.Tutorial.Finished, "Collapse dismissed task");
                    Capture("/tmp/courier-onboarding-collapsed.png",view);
                    owner.Tutorial.AfterLetter(); owner.ShowGuide(); view.Refresh();
                    view.skipButton.onClick.Invoke();
                    Check(!owner.GuideOpen && !owner.Tutorial.Finished, "Closing rules dismissed hints");
                    owner.Run.Node = 0; owner.Run.LoadParcel(owner.Run.Parcels[0]); view.Refresh();
                    Check(owner.Tutorial.Step(owner.Run, false) == 3, "Pickup did not advance hint");
                    Check(view.IsCollapsed && !view.instruction.enabled, "Progress reopened collapsed task");
                    view.skipButton.onClick.Invoke();view.Refresh();
                    Check(!view.IsCollapsed && view.instruction.enabled && view.strip.rect.height == 220,"Task did not expand");
                    Capture("/tmp/courier-onboarding-recipient.png", view);
                    break;
                case 2:
                    owner.Run.StartJourney(3); owner.Tutorial.Leave(); view.Refresh();
                    Check(owner.Tutorial.Step(owner.Run, false) == 4, "Departure did not advance hint");
                    Capture("/tmp/courier-onboarding-travel.png", view);
                    break;
                case 3:
                    view.skipButton.onClick.Invoke(); view.Refresh();
                    Check(!owner.Tutorial.Finished && view.IsCollapsed && view.strip.gameObject.activeSelf, "Minimized task was lost");
                    Finish(true, "Rules, empty travel, pickup, departure, material target highlights, no focus rectangles or Tab prompts, collapse/expand, progress while collapsed.");
                    break;
            }
        }
        catch (Exception e) { Finish(false, e.ToString()); }
    }
    private static void Finish(bool success, string message)
    {
        SessionState.SetBool(Key, false); File.WriteAllText(Result, (success ? "PASS " : "FAIL ") + message);
        EditorApplication.isPlaying = false;
        if (Application.isBatchMode) { EditorApplication.Exit(success ? 0 : 1); return; }
        EditorApplication.playModeStateChanged += RestoreScene;
    }
    private static void RestoreScene(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        EditorApplication.playModeStateChanged -= RestoreScene;
            var scene = SessionState.GetString(Key + "Scene", "");
            if (!string.IsNullOrEmpty(scene)) EditorSceneManager.OpenScene(scene);
    }
    private static void Capture(string path, CourierTutorialView view)
    {
        var camera = UnityEngine.Object.FindObjectOfType<Camera>();
        var target = new RenderTexture(1280, 720, 24); camera.targetTexture = target;
        foreach (var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())
            if (canvas.isRootCanvas) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10; }
        Canvas.ForceUpdateCanvases();
        var page = UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
        page.SendMessage("Refresh"); page.SendMessage("LateUpdate");
        view.Refresh();
        var motion=page.GetComponentInChildren<CourierMapMotion>(true);motion.SendMessage("LateUpdate");
        foreach(var glow in UnityEngine.Object.FindObjectsOfType<CourierLandmarkGlow>())glow.SendMessage("LateUpdate");
        Canvas.ForceUpdateCanvases(); camera.Render();
        var previous = RenderTexture.active; RenderTexture.active = target;
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG()); RenderTexture.active = previous;
        camera.targetTexture = null; target.Release(); UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(texture);
    }
}
