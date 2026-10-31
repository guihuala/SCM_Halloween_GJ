using System;
using System.Linq;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class NightCourierTravelSmokeTest
{
    private const string Key="CourierTravelSmoke.Active";
    private static int step;
    private static double next,deadline;
    private static bool pending,clicked,drivingCaptured,arrivalCaptured;
    private static string clickError;
    private static float clock,visual,motion;
    static NightCourierTravelSmokeTest()
    {
        EditorApplication.update+=Tick;
        Application.logMessageReceived+=(message,stack,type)=>{if(SessionState.GetBool(Key,false)&&(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)){SessionState.SetBool(Key,false);EditorApplication.Exit(1);}};
    }
    public static void Run()
    {
        Debug.Log("COURIER_RULES PASS "+CourierChecks.RunAll());
        EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");EditorApplication.ExecuteMenuItem("Window/General/Game");
        Application.runInBackground=true;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    private static void Require(bool valid,string message){if(!valid)throw new Exception(message);}
    private static T Panel<T>(string name) where T:BasePanel{return UIManager.Instance.GetPanel(name) as T;}
    private static System.Collections.IEnumerator Click(Button button)
    {
        yield return null;Canvas.ForceUpdateCanvases();
        try
        {
            Require(button!=null&&button.gameObject.activeInHierarchy&&button.IsInteractable(),"Travel control unavailable");
            var rect=(RectTransform)button.transform;var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"Travel control raycast blocked: "+button.name);
            ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
        }
        catch(Exception error){clickError=error.ToString();}
        clicked=true;
    }
    private static bool ClickAndWait(NightCourierPrototype owner,Button button)
    {
        if(!pending){pending=true;owner.StartCoroutine(Click(button));}
        if(!clicked){next=EditorApplication.timeSinceStartup+.1;return false;}
        Require(clickError==null,clickError);pending=clicked=false;return true;
    }
    private static void Record(NightCourierPrototype owner){clock=owner.Run.Elapsed;visual=owner.VisualTime;motion=owner.RoadMotionTime;}
    private static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        if(deadline==0){deadline=EditorApplication.timeSinceStartup+100;next=EditorApplication.timeSinceStartup+3;}
        if(EditorApplication.timeSinceStartup<next)return;
        if(SceneLoader.Instance!=null&&SceneLoader.Instance.IsLoading())return;
        try
        {
            Require(EditorApplication.timeSinceStartup<deadline,"Travel controls timed out at "+step);
            var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();Require(owner!=null,"Prototype missing");
            var hud=Panel<CourierHudPanel>("CourierHudPanel");
            switch(step)
            {
                case 0:
                    Require(owner.HasPlanner&&hud.stageGraphic==null&&Panel<CourierRoutePanel>("CourierRoutePanel").pauseButton.gameObject.activeSelf&&!hud.skipRideButton.gameObject.activeSelf,"Dock pause / skip visibility wrong");
                    while(owner.ConversationActive)owner.AdvanceDialogue();owner.ShowPlanner("CourierRoutePanel");break;
                case 1:
                    var map=Panel<CourierRoutePanel>("CourierRoutePanel");
                    Require(map.GetComponent<CourierTooltip>().viewPrefab!=null,"Existing tooltip prefab was replaced");
                    if(!ClickAndWait(owner,map.pauseButton))return;Record(owner);break;
                case 2:
                    Require(GameManager.Instance.CurrentState==GameManager.GameState.Paused&&owner.Run.Elapsed==clock&&owner.VisualTime==visual&&!owner.SkipRide(),"Map pause failed");
                    GameManager.Instance.ResumeGame();
                    var planner=Panel<CourierRoutePanel>("CourierRoutePanel");planner.GetComponentsInChildren<CourierCargoRow>().First(r=>r.Item==owner.Run.Parcels[0]).actionButton.onClick.Invoke();
                    planner.stopButtons[3].onClick.Invoke();planner.departButton.onClick.Invoke();break;
                case 3:
                    if(!pending)
                    {
                        Require(owner.RoadMotionTime>0&&owner.DriveBlend>0&&hud.skipRideButton.gameObject.activeSelf,"Driving motion / skip missing");
                        if(!drivingCaptured){ScreenCapture.CaptureScreenshot("/tmp/courier-travel-driving.png");drivingCaptured=true;next=EditorApplication.timeSinceStartup+.25;return;}
                    }
                    if(!ClickAndWait(owner,hud.skipRideButton))return;break;
                case 4:
                    Require(owner.Run.State==ShiftState.Encounter&&!owner.SkipRide(),"Skip passed through event choice");
                    var reference=new CourierRun(31);CourierContentOverrides.Apply(reference);reference.LoadParcel(reference.Parcels[0]);reference.StartJourney(3);reference.FastForwardTravel();
                    Require(Math.Abs(reference.Elapsed-owner.Run.Elapsed)<.01f&&Math.Abs(reference.Durability-owner.Run.Durability)<.01f,"Skip changed time / wear costs");
                    var encounter=Panel<CourierEventPanel>("CourierEventPanel");
                    if(!ClickAndWait(owner,encounter.pauseButton))return;Record(owner);break;
                case 5:
                    Require(owner.Run.Elapsed==clock&&owner.VisualTime==visual&&owner.RoadMotionTime==motion&&!owner.SkipRide(),"Paused event or animation advanced");
                    GameManager.Instance.ResumeGame();break;
                case 6:
                    Require(owner.Run.State==ShiftState.Encounter,"Resume lost pending encounter");
                    owner.ResolveEncounter(true);break;
                case 7:
                    if(!ClickAndWait(owner,hud.skipRideButton))return;break;
                case 8:
                    Require(owner.Run.Node==1&&owner.Run.HasJourney&&owner.ConversationActive,"Skip skipped intermediate station / dialogue");
                    if(!arrivalCaptured){ScreenCapture.CaptureScreenshot("/tmp/courier-travel-arrival.png");arrivalCaptured=true;next=EditorApplication.timeSinceStartup+.25;return;}
                    owner.Run.Durabilities[owner.Run.VehicleIndex]=0;Require(owner.DepartTo(3)&&owner.Run.State==ShiftState.Pushing,"Push setup failed");break;
                case 9:
                    if(!ClickAndWait(owner,hud.skipRideButton))return;break;
                case 10:
                    Require(owner.Run.Node==3&&owner.Run.Durability>0&&owner.Run.Parcels[0].State==ParcelState.Delivered,"Skip broke pushing / delivery");
                    Require(hud.pauseButton.GetComponent<CourierButtonFeedback>()!=null&&hud.skipRideButton.GetComponent<CourierButtonFeedback>()!=null,"Existing interaction effect missing on new buttons");
                    SessionState.SetBool(Key,false);Debug.Log("COURIER_TRAVEL_UI PASS 11 steps: native pause in map/event, pause freezes clock and motion, skip preserves costs and choices, intermediate stops, push recovery, delivery, existing feedback.");EditorApplication.Exit(0);return;
            }
            Debug.Log("COURIER_TRAVEL_UI step "+step+" passed");step++;next=EditorApplication.timeSinceStartup+.65;
        }
        catch(Exception error){SessionState.SetBool(Key,false);Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
