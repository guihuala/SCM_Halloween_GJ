using System;
using System.Linq;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class NightCourierPanelSmokeTest
{
    private const string Key="CourierPanelSmoke.Active";
    private static int step;
    private static double next, deadline;
    private static float pausedAt;
    static NightCourierPanelSmokeTest()
    {
        EditorApplication.update+=Tick;
        Application.logMessageReceived+=(message,stack,type)=>
        {
            if(SessionState.GetBool(Key,false) && (type==LogType.Error||type==LogType.Exception||type==LogType.Assert))
            { SessionState.SetBool(Key,false); EditorApplication.Exit(1); }
        };
    }
    public static void Run()
    {
        Debug.Log("COURIER_RULES PASS "+CourierChecks.RunAll());
        foreach(var name in new[]{"CourierHudPanel","CourierCargoPanel","CourierRoutePanel","CourierVehiclePanel","CourierEventPanel","CourierResultPanel"})
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Panel/Courier/"+name+".prefab");
            Require(asset!=null && asset.GetComponent<BasePanel>()!=null,"Missing prefab "+name);
        }
        SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        Application.runInBackground=true; EditorApplication.isPlaying=true;
    }
    private static void Require(bool value,string error) { if(!value) throw new Exception(error); }
    private static T Panel<T>(string name) where T:BasePanel
    {var p=UIManager.Instance.GetPanel(name) as T;Require(p!=null,"Missing open panel "+name);return p;}
    private static void Shot(string name) { if(!Application.isBatchMode) ScreenCapture.CaptureScreenshot("/tmp/courier-panels-"+name+".png"); }
    private static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        if(deadline==0) {deadline=EditorApplication.timeSinceStartup+65;next=EditorApplication.timeSinceStartup+3;}
        if(EditorApplication.timeSinceStartup>deadline) {SessionState.SetBool(Key,false);Debug.LogError("Panel smoke timed out at "+step);EditorApplication.Exit(1);return;}
        if(EditorApplication.timeSinceStartup<next)return;
        try
        {
            var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();Require(owner!=null,"Missing prototype");
            var hud=Panel<CourierHudPanel>("CourierHudPanel");
            switch(step)
            {
                case 0:
                    Require(!owner.HasPlanner,"Startup cluttered by modal");
                    Require(hud.cargoButton.gameObject.activeSelf && hud.routeButton.gameObject.activeSelf,"Essential dock actions missing");
                    Require(!hud.vehicleButton.gameObject.activeSelf,"Unavailable vehicle action should be hidden");
                    Shot("hud");break;
                case 1: hud.cargoButton.onClick.Invoke();break;
                case 2:
                    var cargo=Panel<CourierCargoPanel>("CourierCargoPanel");
                    var row=cargo.GetComponentsInChildren<CourierCargoRow>().First(r=>r.nameText.text=="余温尚存的信");
                    row.actionButton.onClick.Invoke();break;
                case 3:
                    var c=Panel<CourierCargoPanel>("CourierCargoPanel");
                    Require(owner.Run.Load==1 && c.loadedContent.childCount==1,"Cargo UI failed to load and relocate row: load="+owner.Run.Load+", rows="+c.loadedContent.childCount);
                    // Verify the native event system hits the visible button rather than the scene behind it.
                    var point=RectTransformUtility.WorldToScreenPoint(null,c.continueButton.transform.position);
                    var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
                    Require(hits.Count>0 && hits[0].gameObject.GetComponentInParent<Button>()==c.continueButton,"Modal action blocked by another UI surface");
                    Shot("cargo");break;
                case 4: Panel<CourierCargoPanel>("CourierCargoPanel").continueButton.onClick.Invoke();break;
                case 5:
                    var route=Panel<CourierRoutePanel>("CourierRoutePanel");
                    Require(!UIManager.Instance.IsPanelOpen("CourierCargoPanel"),"Two planners remained open");
                    Require(route.mapGraphic.GetComponent<CanvasRenderer>() != null,"Map has no native canvas renderer");
                    route.stopButtons[1].onClick.Invoke();break;
                case 6:
                    var selected=Panel<CourierRoutePanel>("CourierRoutePanel");
                    Require(selected.destinationText.text.Contains("霓虹餐厅") && selected.departButton.interactable,"Route selection failed");
                    Shot("route");break;
                case 7:
                    Panel<CourierRoutePanel>("CourierRoutePanel").departButton.onClick.Invoke();
                    Require(owner.Run.State==ShiftState.Driving&&!owner.HasPlanner,"Departure kept planner open");
                    GameManager.Instance.PauseGame();pausedAt=owner.Run.Elapsed;break;
                case 8:
                    Require(owner.Run.Elapsed==pausedAt,"Pause advanced time");GameManager.Instance.ResumeGame();break;
                case 9:
                    if(owner.Run.State!=ShiftState.Encounter){next=EditorApplication.timeSinceStartup+.25;return;}
                    Require(UIManager.Instance.IsPanelOpen("CourierEventPanel"),"No event prefab");
                    if(Panel<CourierEventPanel>("CourierEventPanel").GetComponentsInChildren<CanvasGroup>().Any(g=>g.alpha<.99f)) {next=EditorApplication.timeSinceStartup+.25;return;}
                    Shot("event");break;
                case 10: Panel<CourierEventPanel>("CourierEventPanel").safeButton.onClick.Invoke();break;
                case 11:
                    if(owner.Run.State!=ShiftState.Docked){next=EditorApplication.timeSinceStartup+.25;return;}
                    Require(owner.Run.Delivered==1 && owner.Run.Node==1,"Delivery after event failed");
                    hud.vehicleButton.onClick.Invoke();break;
                case 12:
                    var vehicle=Panel<CourierVehiclePanel>("CourierVehiclePanel");Require(!vehicle.repairButton.interactable,"Non-repair stop offers repair");Shot("vehicle");break;
                case 13:
                    Require(owner.ClosePlanner()&&!owner.HasPlanner,"Back failed");
                    owner.Run.Node=2;owner.ShowPlanner("CourierVehiclePanel");break;
                case 14:
                    Panel<CourierVehiclePanel>("CourierVehiclePanel").possessButton.onClick.Invoke();
                    Require(owner.Run.VehicleIndex==1&&owner.InCar,"Vehicle panel possession failed");owner.ClosePlanner();
                    owner.Run.State=ShiftState.Won;break;
                case 15: Require(UIManager.Instance.IsPanelOpen("CourierResultPanel"),"Result prefab missing");Shot("result");break;
                case 16:
                    Panel<CourierResultPanel>("CourierResultPanel").restartButton.onClick.Invoke();
                    Require(owner.Run.Node==0&&owner.Run.Load==0&&owner.Run.Delivered==0&&!owner.HasPlanner,"New shift did not reset");
                    break;
                case 17:
                    SessionState.SetBool(Key,false);Debug.Log("COURIER_PANELS PASS 18 steps: native prefab loading, cargo, raycasts, modal switch, map, departure, pause, event, delivery, vehicle, result, restart.");EditorApplication.Exit(0);return;
            }
            Debug.Log("COURIER_PANELS step "+step+" passed");step++;next=EditorApplication.timeSinceStartup+.6;
        }
        catch(Exception ex) {SessionState.SetBool(Key,false);Debug.LogException(ex);EditorApplication.Exit(1);}
    }
}
