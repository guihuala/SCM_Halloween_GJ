using System;
using System.IO;
using System.Linq;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Runs the real gameplay scene. Undo and event-confirm inputs are supplied by the tester.
[InitializeOnLoad]
public static class CourierMovementUndoPlayTest
{
    private const string Key="CourierMovementUndoPlayTest.Active";
    private const string Status="/tmp/courier-undo-live-status.txt";
    private const string Report="/tmp/courier-undo-live-report.txt";
    private static int stage,assertions,encounter;
    private static double next,deadline;
    private static SceneSetup[] scenes;
    private static NightCourierPrototype owner;
    private static CourierRoutePanel page;
    private static CourierHudPanel hud;
    private static int startNode,parcelId;
    private static float durability;
    static CourierMovementUndoPlayTest(){EditorApplication.update+=Tick;}

    [MenuItem("Tools/Night Courier/Checks/Movement Undo Live Test")]
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop play mode before starting undo tests.");
        scenes=EditorSceneManager.GetSceneManagerSetup();
        SessionState.SetString(Key+".Scene",EditorSceneManager.GetActiveScene().path);
        File.WriteAllText(Report,"Unity Play Mode movement undo verification\n");
        File.WriteAllText(Status,"STARTING");
        EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
        stage=0;assertions=0;deadline=0;next=0;
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    private static void Require(bool valid,string message)
    {assertions++;if(!valid)throw new Exception(message);}
    private static void WaitForInput(string message)
    {
        File.WriteAllText(Status,message);
        Debug.Log("UNDO_LIVE "+message);
    }
    private static void Pass(string message)
    {File.AppendAllText(Report,"PASS "+message+"\n");Debug.Log("UNDO_LIVE PASS "+message);}
    private static void Prepare(string from,int cargo,int vehicle,float fuel,float probability)
    {
        owner.RestartShift();owner.Tutorial.Dismiss();
        for(int i=0;i<200&&owner.ConversationActive;i++){hud.CompleteSubtitle();owner.AdvanceDialogue();}
        Require(!owner.ConversationActive,"Opening conversation was not dismissed");
        startNode=CourierStreetMap.Node(from).StopId;parcelId=cargo;
        var run=owner.Run;run.Node=startNode;run.VehicleIndex=vehicle;run.EventProbability=probability;
        run.PreferSafeRoutes=false;
        if(vehicle>=0)run.Durabilities[vehicle]=fuel;
        durability=run.Durability;
        run.Parcels[cargo].State=ParcelState.Loaded;run.Parcels[cargo].CarriedTurns=2;
        run.AudioCues.Clear();page.Bind(owner);hud.Bind(owner);
    }
    private static void Depart(string to)
    {Require(owner.DepartTo(CourierStreetMap.Node(to).StopId),"Real controller rejected departure to "+to);}
    private static void CheckRestored(string scenario)
    {
        var run=owner.Run;var p=run.Parcels[parcelId];
        Require(run.Node==startNode&&run.State==ShiftState.Docked,"Undo did not return to departure node");
        Require(p.State==ParcelState.Loaded&&p.CarriedTurns==2&&!p.Damaged&&!p.EventTriggered,"Undo retained cargo damage, event or clock");
        Require(run.Elapsed==0&&run.Durability==durability&&run.Results.Count==0&&run.Delivered==0&&run.Earnings==0,"Undo retained cost, wear, delivery or reward");
        Require(!owner.ConversationActive&&!hud.subtitleText.gameObject.activeSelf,"Undo left dialogue visible");
        Require(UIManager.Instance.GetPanel("CourierEventPanel")==null,"Undo left event panel registered");
        Require(page.UndoButton!=null&&!page.UndoButton.interactable,"Undo button remained enabled with empty history");
        Require(page.GetComponentsInChildren<CourierCargoRow>(true).Count(r=>ReferenceEquals(r.Item,p))==1,"Undo duplicated or lost cargo row");
        Require(run.PlannedDestination==-1&&!run.CanUndoMovement,"Undo left route or history");
        Pass(scenario+": departure node, cargo, turns, vehicle, rewards, result, dialogue, event popup and cargo row restored");
    }
    private static void Tick()
    {
        // Other editor checks also own this scene; never run fixtures concurrently.
        if(SessionState.GetBool("CourierVehicleStations.Active",false))return;
        if(File.Exists("/tmp/courier-undo-live-request")&&!EditorApplication.isPlaying&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating)
        {
            File.Delete("/tmp/courier-undo-live-request");Run();return;
        }
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        try
        {
            if(deadline==0){deadline=EditorApplication.timeSinceStartup+900;next=EditorApplication.timeSinceStartup+3;}
            Require(EditorApplication.timeSinceStartup<deadline,"Live test timed out at stage "+stage);
            if(EditorApplication.timeSinceStartup<next)return;
            owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();
            page=UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
            hud=UIManager.Instance.GetPanel("CourierHudPanel") as CourierHudPanel;
            if(owner==null||page==null||hud==null||!owner.Active)return;
            switch(stage)
            {
                case 0:
                    Prepare("3b",0,1,2,0);Depart("3");stage=1;break;
                case 1:
                    if(owner.Run.State!=ShiftState.Docked)return;
                    Require(owner.Run.Parcels[0].State==ParcelState.Delivered&&owner.Run.Results.Count==1,"Real arrival did not deliver");
                    Require(owner.ConversationActive,"Delivery did not open real dialogue");
                    Require(page.UndoButton.interactable,"Undo button disabled during delivery dialogue");
                    Pass("Delivery completed in real scene; receipt dialogue and undo button visible");
                    WaitForInput("WAIT_DELIVERY_UNDO: click Undo button in game");stage=2;break;
                case 2:
                    if(owner.Run.CanUndoMovement)return;
                    stage=3;next=EditorApplication.timeSinceStartup+1;break;
                case 3:
                    CheckRestored("Mouse undo after delivery");
                    Prepare("U",1,-1,0,1);Depart("C");stage=4;break;
                case 4:
                    if(owner.Run.State!=ShiftState.Encounter)return;
                    encounter=owner.Run.EncounterIndex;
                    Require(UIManager.Instance.GetPanel("CourierEventPanel")!=null,"Pending wait event has no popup");
                    Require(owner.Run.Parcels[1].Damaged,"Heat surface did not damage test cargo");
                    WaitForInput("WAIT_PENDING_EVENT_UNDO: focus game and press Z");stage=5;break;
                case 5:
                    if(owner.Run.CanUndoMovement)return;
                    stage=6;next=EditorApplication.timeSinceStartup+1;break;
                case 6:
                    CheckRestored("Z undo while wait event popup is open");
                    Depart("C");stage=7;break;
                case 7:
                    if(owner.Run.State!=ShiftState.Encounter)return;
                    Require(owner.Run.EncounterIndex==encounter,"Event rerolled after undo and replay");
                    Pass("Same route replayed the same event");
                    WaitForInput("WAIT_RESOLVE_WAIT: click event confirm button");stage=8;break;
                case 8:
                    if(owner.Run.State!=ShiftState.Docked)return;
                    Require(owner.Run.OrderTurns==4&&owner.Run.Node==CourierStreetMap.Node("C").StopId,"Wait consequence was not applied");
                    WaitForInput("WAIT_RESOLVED_WAIT_UNDO: click Undo button");stage=9;next=EditorApplication.timeSinceStartup+1;break;
                case 9:
                    if(owner.Run.CanUndoMovement)return;
                    stage=10;next=EditorApplication.timeSinceStartup+1;break;
                case 10:
                    CheckRestored("Mouse undo after resolving wait event");
                    Prepare("3b",0,-1,0,1);Depart("N");stage=11;break;
                case 11:
                    if(owner.Run.State!=ShiftState.Encounter)return;
                    Require(owner.Run.Events[owner.Run.EncounterIndex].Effect==RoadEventEffect.Retreat,"Wrong retreat event fixture");
                    WaitForInput("WAIT_RESOLVE_RETREAT: click event confirm button");stage=12;break;
                case 12:
                    if(owner.Run.State!=ShiftState.Docked)return;
                    Require(owner.Run.Node==startNode&&owner.Run.OrderTurns==3&&owner.Run.OrderEventTriggered,"Retreat consequence was not applied");
                    WaitForInput("WAIT_RESOLVED_RETREAT_UNDO: focus game and press Z");stage=13;next=EditorApplication.timeSinceStartup+1;break;
                case 13:
                    if(owner.Run.CanUndoMovement)return;
                    stage=14;next=EditorApplication.timeSinceStartup+1;break;
                case 14:
                    CheckRestored("Z undo after resolving retreat event");
                    owner.Run.EventProbability=0;Depart("3");stage=15;break;
                case 15:
                    if(owner.Run.State!=ShiftState.Docked)return;
                    Require(owner.Run.Parcels[0].State==ParcelState.Delivered&&owner.Run.Results.Count==1,"Delivery after event undo failed");
                    Pass("Can choose another route and deliver after event undo; no duplicate delivery");
                    WaitForInput("WAIT_FINAL_DELIVERY_UNDO: press Z to check re-delivery undo");stage=16;break;
                case 16:
                    if(owner.Run.CanUndoMovement)return;
                    stage=17;next=EditorApplication.timeSinceStartup+1;break;
                case 17:
                    CheckRestored("Z undo after subsequent delivery");
                    owner.RestartShift();
                    Require(!owner.Run.CanUndoMovement,"Restart retained movement history");
                    Pass("Restart clears undo history");
                    WaitForInput("PASS: all live scenarios verified; "+assertions+" checks");
                    File.AppendAllText(Report,"PASS ALL live scenarios; "+assertions+" checks\n");
                    Finish();return;
            }
        }
        catch(Exception e)
        {
            File.AppendAllText(Report,"FAIL stage "+stage+": "+e+"\n");
            WaitForInput("FAIL stage "+stage+": "+e.Message);Debug.LogException(e);Finish();
        }
    }
    private static void Finish()
    {
        SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;
        EditorApplication.playModeStateChanged+=RestoreScene;
    }
    private static void RestoreScene(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredEditMode)return;
        EditorApplication.playModeStateChanged-=RestoreScene;
        if(scenes!=null)EditorSceneManager.RestoreSceneManagerSetup(scenes);
        else{var path=SessionState.GetString(Key+".Scene","");if(!string.IsNullOrEmpty(path))EditorSceneManager.OpenScene(path);}
    }
}
