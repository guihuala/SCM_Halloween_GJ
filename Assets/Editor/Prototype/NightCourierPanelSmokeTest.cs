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
    private static double next,deadline;
    private static float pausedAt;
    static NightCourierPanelSmokeTest()
    {
        EditorApplication.update+=Tick;
        Application.logMessageReceived+=(message,stack,type)=>{if(SessionState.GetBool(Key,false)&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)){SessionState.SetBool(Key,false);EditorApplication.Exit(1);}};
    }
    public static void Run()
    {
        Debug.Log("COURIER_RULES PASS "+CourierChecks.RunAll());
        var configured=new CourierRun();CourierContentOverrides.Apply(configured);
        Require(configured.Stops[0].Name=="Abandoned Subway"&&configured.Stops[3].Vehicle==2,"New stop CSV not loaded");
        Require(configured.Parcels.Count==12&&configured.Parcels[0].Destination==3&&configured.Parcels[11].TimeLimit==90,"New parcel CSV not loaded");
        Require(configured.NightMinutes==240,"Shift CSV not loaded");
        foreach(var name in new[]{"CourierHudPanel","CourierRoutePanel","CourierEventPanel","CourierResultPanel"})
            Require(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Panel/Courier/"+name+".prefab")!=null,"Missing prefab "+name);
        SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
        EditorApplication.ExecuteMenuItem("Window/General/Game");Application.runInBackground=true;EditorApplication.isPlaying=true;
    }
    private static void Require(bool ok,string error){if(!ok)throw new Exception(error);}
    private static T Panel<T>(string name)where T:BasePanel{var p=UIManager.Instance.GetPanel(name)as T;Require(p!=null,"Missing panel "+name);return p;}
    private static CourierRoutePanel Planner{get{return Panel<CourierRoutePanel>("CourierRoutePanel");}}
    private static void Shot(string name){if(!Application.isBatchMode)ScreenCapture.CaptureScreenshot("/tmp/courier-v2-"+name+".png");}
    private static void ClickMapLabel(int node)
    {
        var label=Planner.stopLabels[node].rectTransform;
        var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,label.TransformPoint(label.rect.center)),button=PointerEventData.InputButton.Left};
        var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==Planner.stopButtons[node],"Relocated station name is not clickable");
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerClickHandler);
    }
    private static CourierCargoRow Row(string name){return Planner.GetComponentsInChildren<CourierCargoRow>().First(r=>r.nameText.text==name);}
    private static void VerifyEnglishPresentation()
    {
        var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/BarlowCondensed-Light.ttf");
        foreach(var text in UnityEngine.Object.FindObjectsOfType<Text>(true))
        {
            Require(text.font==font,"Project font missing on "+text.name);
            Require(!System.Text.RegularExpressions.Regex.IsMatch(text.text,@"[\u3400-\u9fff]"),"Non-English UI text on "+text.name);
        }
    }
    private static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        if(deadline==0){deadline=EditorApplication.timeSinceStartup+100;next=EditorApplication.timeSinceStartup+3;}
        if(EditorApplication.timeSinceStartup>deadline){SessionState.SetBool(Key,false);Debug.LogError("V2 UI timeout at "+step);EditorApplication.Exit(1);return;}
        if(EditorApplication.timeSinceStartup<next)return;
        if(SceneLoader.Instance!=null&&SceneLoader.Instance.IsLoading()){next=EditorApplication.timeSinceStartup+.2;return;}
        try
        {
            var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();Require(owner!=null,"Missing prototype");var hud=Panel<CourierHudPanel>("CourierHudPanel");
            VerifyEnglishPresentation();
            switch(step)
            {
                case 0:Require(owner.HasPlanner&&hud.stageGraphic==null,"Map-only surface or stage removal missing");Require(owner.ConversationActive,"Drow opening missing");Shot("opening");break;
                case 1:while(owner.ConversationActive)owner.AdvanceDialogue();owner.ShowPlanner("CourierRoutePanel");break;
                case 2:Require(Planner.stopButtons.Length==6,"Map node count");Row("Party Theme Tape").actionButton.onClick.Invoke();break;
                case 3:Require(owner.Run.Load==1,"Tape UI load failed");ClickMapLabel(3);Require(Planner.departButton.interactable,"Radio destination locked");Shot("planner");break;
                case 4:Planner.departButton.onClick.Invoke();GameManager.Instance.PauseGame();pausedAt=owner.Run.Elapsed;break;
                case 5:Require(owner.Run.Elapsed==pausedAt,"Pause advances clock");GameManager.Instance.ResumeGame();owner.Run.Tick(10000);break;
                case 6:Require(owner.HasPlanner&&!Planner.departButton.gameObject.activeSelf&&Planner.stopButtons.All(b=>!b.interactable),"Map actions active in transit");Require(owner.Run.State==ShiftState.Encounter,"Road encounter missing");Shot("event");owner.ResolveEncounter(true);owner.Run.Tick(10000);break;
                case 7:Require(owner.Run.Node==1&&owner.Run.HasJourney,"Intermediate stop lost journey");owner.ClosePlanner();CourierChecks.Go(owner.Run,3);Require(owner.Run.Parcels[0].State==ParcelState.Delivered,"Tape not delivered to vampire");CourierChecks.Go(owner.Run,2);owner.BeginConversation();owner.ShowPlanner("CourierRoutePanel");break;
                case 8:Planner.possessButton.onClick.Invoke();Require(owner.Run.VehicleIndex==1,"Bear bicycle UI possession failed");Row("Spring Mead").actionButton.onClick.Invoke();Require(owner.Run.Load==4,"Heavy mead UI loading failed");Shot("mead");owner.ClosePlanner();CourierChecks.Go(owner.Run,5);break;
                case 9:Require(!owner.Run.Finished,"Bar still triggers lighthouse ending");owner.Run.Elapsed=CourierRun.ShiftMinutes;foreach(int node in new[]{0,1,2,3,4,5}){if(owner.Run.Finished)break;CourierChecks.Go(owner.Run,node);}break;
                case 10:Require(Panel<CourierResultPanel>("CourierResultPanel").titleText.text=="Together at Dawn","Party result missing");Shot("party");Panel<CourierResultPanel>("CourierResultPanel").restartButton.onClick.Invoke();break;
                case 11:Require(owner.Run.Node==0&&owner.Run.Load==0&&!owner.Run.Finished,"Restart did not reset delivery shift");SessionState.SetBool(Key,false);Debug.Log("COURIER_UI PASS 12 steps: new map, tape, intermediate route, pause, event, bicycle, heavy mead, party and restart.");EditorApplication.Exit(0);return;
            }
            Debug.Log("COURIER_V2_UI step "+step+" passed");step++;next=EditorApplication.timeSinceStartup+.65;
        }
        catch(Exception ex){SessionState.SetBool(Key,false);Debug.LogException(ex);EditorApplication.Exit(1);}
    }
}
