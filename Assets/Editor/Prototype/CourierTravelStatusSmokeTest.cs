using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[InitializeOnLoad]
public static class CourierTravelStatusSmokeTest
{
    private const string Key = "CourierTravelStatusSmoke.Active";
    private static int step;
    private static double next, deadline;
    private static float pausedPhase;
    static CourierTravelStatusSmokeTest() { EditorApplication.update += Tick; }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
        SessionState.SetBool(Key, true); Application.runInBackground = true; EditorApplication.isPlaying = true;
    }
    private static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    private static float Phase(CourierTravelStatus view) =>
        (float)typeof(CourierTravelStatus).GetField("phase", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);

    private static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
        if (deadline == 0) { deadline = EditorApplication.timeSinceStartup + 75; next = EditorApplication.timeSinceStartup + 3; }
        if (EditorApplication.timeSinceStartup < next) return;
        try
        {
            Require(EditorApplication.timeSinceStartup < deadline, "Travel status timed out at " + step);
            var owner = UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();
            var hud = UIManager.Instance.GetPanel("CourierHudPanel") as CourierHudPanel;
            var page = UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
            Require(owner != null && hud != null, "Gameplay/HUD missing");
            var view = hud.GetComponentInChildren<CourierTravelStatus>(true);
            Require(view != null, "Travel status missing");
            switch (step)
            {
                case 0:
                    // Preview Traditional Chinese without writing the player's language preference.
                    typeof(CourierLocalization).GetField("language", BindingFlags.Static | BindingFlags.NonPublic)
                        .SetValue(null, CourierLanguage.TraditionalChinese);
                    owner.RestartShift(); owner.Tutorial.Dismiss(); while (owner.ConversationActive) owner.AdvanceDialogue();
                    owner.enabled = false; owner.Run.VehicleIndex = -1; owner.Run.EventProbability = 0;
                    page.Select(2);
                    break;
                case 1:
                    Require(page.destinationTooltip.gameObject.activeInHierarchy && page.destinationTooltip.rect.height <= ((RectTransform)page.destinationTooltip.parent).rect.height-136,
                        "Explicit route preview is missing or still oversized");
                    Require(page.destinationTooltip.GetComponent<CanvasGroup>().alpha > .99f, "Route preview is still transparent");
                    Require(!page.wearCostText.gameObject.activeSelf && page.riskText.enabled &&
                        !page.destinationTooltip.Find("RouteWarningSymbol").gameObject.activeSelf, "Empty ghost route repeats wear/cost text or shows a false warning");
                    Capture("/private/tmp/courier-route-card-clean.png", 1280, 720);
                    Require(owner.Run.StartJourney(2), "Cannot start journey");
                    owner.Run.Tick(3.6f);
                    break;
                case 2:
                    var rect = (RectTransform)view.transform;
                    Require(view.gameObject.activeInHierarchy && view.GetComponent<CanvasGroup>().alpha > .99f, "Travel status not visible");
                    Require(rect.anchorMin == new Vector2(.5f, 0) && rect.rect.width >= 640 &&
                        hud.progressFill.rectTransform.rect.height >= 14, "Progress bar is not prominent at bottom center");
                    Require(view.GetComponentsInChildren<Graphic>(true).All(g => !g.raycastTarget) &&
                        !view.GetComponent<CanvasGroup>().blocksRaycasts, "Travel ribbon intercepts map input");
                    Require(view.transform.Find("TravelPercent").GetComponent<Text>().text.EndsWith("%"), "Travel percentage missing");
                    Require(view.transform.Find("TravelDash0").gameObject.activeSelf, "Moving feedback missing");
                    Require(!hud.roadStatus.activeSelf,"Empty travel shows a meaningless clock");
                    Capture("/private/tmp/courier-travel-ribbon.png", 1280, 720);
                    HitSkip(hud,false);
                    var hudRoot=(RectTransform)hud.transform;var skip=(RectTransform)hud.skipRideButton.transform;
                    Require(Mathf.Abs(hudRoot.InverseTransformPoint(skip.position).x-(hudRoot.rect.xMax-32))<1,"Skip is not right-aligned");
                    pausedPhase = Phase(view); GameManager.Instance.PauseGame();
                    break;
                case 3:
                    Require(Mathf.Approximately(Phase(view), pausedPhase), "Pause advanced travel effects");
                    Require(!view.transform.Find("TravelDash0").gameObject.activeSelf, "Pause retained moving dashes");
                    GameManager.Instance.ResumeGame(); owner.Run.State = ShiftState.Encounter;
                    break;
                case 4:
                    Require(!view.transform.Find("TravelDash0").gameObject.activeSelf && hud.progressFill.color == CourierColors.Gold,
                        "Road event should stop effects and change status");
                    Require(!view.transform.Find("TravelState").GetComponent<Text>().text.Contains("Road event"), "New state was not localized");
                    Capture("/private/tmp/courier-travel-event.png", 1280, 720);
                    owner.Run.State = ShiftState.Docked;
                    break;
                case 5:
                    Require(!view.gameObject.activeSelf, "Waypoint repeats the Continue prompt in the travel ribbon");
                    Require(!page.destinationTooltip.gameObject.activeSelf && page.nodePromptButton.gameObject.activeInHierarchy,
                        "Waypoint reopens route details or loses its single Continue action");
                    Capture("/private/tmp/courier-waypoint-clean.png", 1280, 720);
                    page.Select(2);
                    Require(!page.nodePromptButton.gameObject.activeSelf, "Explicit route preview duplicates the Continue action");
                    owner.Run.State = ShiftState.Driving; hud.reducedMotion = true;
                    pausedPhase = Phase(view);
                    break;
                case 6:
                    Require(Mathf.Approximately(Phase(view), pausedPhase) &&
                        !view.transform.Find("TravelDash0").gameObject.activeSelf, "Reduced motion still animates");
                    Capture("/private/tmp/courier-travel-ribbon-compact.png", 960, 720);
                    owner.Run.State = ShiftState.Docked; owner.Run.Node = owner.Run.PlannedDestination;
                    break;
                case 7:
                    Require(!view.gameObject.activeSelf, "Travel ribbon remained after arrival");
                    owner.Run.Node = 0; owner.Run.VehicleIndex = 0;
                    owner.Run.Parcels[0].State = ParcelState.Loaded; owner.Run.Parcels[0].Damaged = true;
                    page.Select(2);
                    break;
                case 8:
                    Require(page.destinationTooltip.gameObject.activeInHierarchy && page.destinationTooltip.rect.height <= ((RectTransform)page.destinationTooltip.parent).rect.height-136 &&
                        page.riskText.enabled &&
                        page.destinationTooltip.Find("RouteWarningSymbol").gameObject.activeSelf && page.wearCostText.gameObject.activeSelf,
                        "Compact route card removed meaningful risk or vehicle-wear information");
                    Require(owner.Run.StartJourney(2),"Cannot start loaded control fixture");
                    Capture("/private/tmp/courier-travel-controls-loaded.png",1280,720);
                    Require(hud.roadStatus.activeSelf&&hud.timeText.text.Contains("/"),"Loaded order clock hidden");
                    var status=(RectTransform)hud.roadStatus.transform;var skipButton=(RectTransform)hud.skipRideButton.transform;
                    Require(Mathf.Abs(status.position.y-skipButton.position.y)<1,"Travel controls are not vertically aligned");
                    HitSkip(hud,true);
                    SessionState.SetBool(Key, false);
                    Debug.Log("COURIER_TRAVEL_STATUS PASS: actual pointer hit and fast-forward, single waypoint action, explicit compact route preview, ghost wear hidden, real risks retained, travel motion, pause, event, reduced motion, resize, arrival and Chinese text.");
                    EditorApplication.Exit(0); return;
            }
            step++; next = EditorApplication.timeSinceStartup + .65;
        }
        catch (Exception error)
        { SessionState.SetBool(Key, false); Debug.LogException(error); EditorApplication.Exit(1); }
    }

    private static void HitSkip(CourierHudPanel hud,bool click)
    {
        Canvas.ForceUpdateCanvases();
        var rect=(RectTransform)hud.skipRideButton.transform;
        var canvas=rect.GetComponentInParent<Canvas>().rootCanvas;
        var data=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,
            position=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(rect.rect.center))};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==hud.skipRideButton,
            "Skip pointer blocked: "+string.Join(", ",hits.Select(h=>h.gameObject.name+"/"+h.gameObject.transform.parent.name)));
        Require(hud.skipRideButton.IsInteractable(),"Skip button not interactable");
        if(click)
        {
            var run=NightCourierPrototype.Current.Run;var before=run.Node;var progress=run.DisplayProgress;
            ExecuteEvents.Execute(hits[0].gameObject,data,ExecuteEvents.pointerClickHandler);
            Require(run.Node!=before||run.State!=ShiftState.Driving||run.DisplayProgress!=progress,"Pointer click did not skip travel");
        }
    }
    private static void Capture(string path, int width, int height)
    {
        var camera = UnityEngine.Object.FindObjectOfType<Camera>();
        var target = new RenderTexture(width, height, 24); camera.targetTexture = target;
        foreach (var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())
            if (canvas.isRootCanvas)
            { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10; }
        Canvas.ForceUpdateCanvases();
        // Camera/target changes resize the canvas; settle absolute map-card layout
        // at the capture dimensions before drawing the first frame.
        var page = UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
        page.SendMessage("Refresh"); page.SendMessage("LateUpdate");
        var hud = UIManager.Instance.GetPanel("CourierHudPanel") as CourierHudPanel;
        hud.SendMessage("Refresh");
        Canvas.ForceUpdateCanvases(); camera.Render();
        var previous = RenderTexture.active; RenderTexture.active = target;
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG()); RenderTexture.active = previous;
        camera.targetTexture = null; target.Release(); UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(texture);
    }
}
