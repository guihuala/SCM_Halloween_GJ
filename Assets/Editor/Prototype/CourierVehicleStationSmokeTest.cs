using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CourierVehicleStationSmokeTest
{
    const string Flag="/tmp/courier-vehicle-stations-run",Result="/tmp/courier-vehicle-stations-result.txt",Key="CourierVehicleStations.Active";
    static int step;static double next,deadline;static SceneSetup[] scenes;
    static CourierLanguage language;
    static CourierVehicleStationSmokeTest(){EditorApplication.update+=Tick;}
    [MenuItem("Tools/Night Courier/Checks/Vehicle Station Interaction")]
    public static void Run()
    {
        if(EditorApplication.isPlaying)return;
        scenes=EditorSceneManager.GetSceneManagerSetup();language=CourierLocalization.Language;
        SessionState.SetString(Key+"Scene",EditorSceneManager.GetActiveScene().path);
        SessionState.SetInt(Key+"Language",(int)language);
        File.WriteAllText(Result,"Running");
        EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
        step=0;deadline=0;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static void SetLanguage(CourierLanguage value)
    {
        typeof(CourierLocalization).GetField("language",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,value);
        ((System.Collections.IDictionary)typeof(CourierLocalization).GetField("cache",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null)).Clear();
    }
    static void Tick()
    {
        if(SessionState.GetBool("CourierMovementUndoPlayTest.Active",false)&&!SessionState.GetBool(Key,false))return;
        if(File.Exists(Flag)&&!EditorApplication.isCompiling&&!EditorApplication.isPlaying){File.Delete(Flag);Run();}
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        Application.runInBackground=true;
        EditorApplication.QueuePlayerLoopUpdate();
        var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();if(owner==null||!owner.HasPlanner)return;
        var pump=owner.GetComponent<CourierVehicleStationCheckPump>()??owner.gameObject.AddComponent<CourierVehicleStationCheckPump>();pump.Check=CheckInGame;
    }
    static void CheckInGame()
    {
        if(!SessionState.GetBool(Key,false))return;
        if(deadline==0){deadline=EditorApplication.timeSinceStartup+90;next=EditorApplication.timeSinceStartup+4;}
        if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+1;
        try
        {
            Require(EditorApplication.timeSinceStartup<deadline,"Station checks timed out");
            var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();
            var page=UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
            Require(owner!=null&&page!=null,"Route page missing");
            if(GameManager.Instance!=null)GameManager.Instance.ResumeGame();
            if(!owner.Active)return;
            var run=owner.Run;
            if(step==0)
            {
                owner.Tutorial.Dismiss();for(int n=0;n<100&&owner.ConversationActive;n++)owner.AdvanceDialogue();
                owner.enabled=false;SetLanguage(CourierLanguage.SimplifiedChinese);page.SetCargoOpen(false);
                run.Node=6;run.VehicleIndex=-1;run.EventProbability=0;
                run.Parcels[0].State=ParcelState.Loaded;
                ScreenCapture.CaptureScreenshot("/tmp/courier-station-layout.png");
            }
            else if(step<=3)
            {
                int id=step-1;Require(run.Node==id+6,"Wrong station fixture");
                page.Bind(owner); // Direct fixture edits must be presented before inspecting the UI.
                Canvas.ForceUpdateCanvases();CheckLayout(page);
                var label=page.possessButton.GetComponentInChildren<Text>(true);
                Require(page.possessButton.gameObject.activeInHierarchy&&label.enabled&&label.text.Contains("换载具")&&label.text.Contains("5"),"Swap CTA missing visible instructions: "+label.text+" / active="+page.possessButton.gameObject.activeInHierarchy+" / enabled="+label.enabled);
                Require(label.preferredHeight<=label.rectTransform.rect.height+1,"Swap CTA text clipped");
                Require(page.possessButton.interactable,"Swap CTA disabled");
                Require(page.mapArtwork.stationButtons[id].GetComponentInChildren<Text>().text.Contains("E"),"Current station missing action label");
                int turns=run.OrderTurns;
                if(id==0)owner.InteractAtNode();
                else if(id==1)page.mapArtwork.stationButtons[id].onClick.Invoke();
                else page.possessButton.onClick.Invoke();
                Require(run.VehicleIndex==id&&run.Durability==5,"Station swap failed: "+id);
                Require(run.OrderTurns==turns+1,"Swap cost differs from one order turn");
                CheckExchangeFeedback(page,run);
                // A same-type exchange still spends a turn and must confirm its refill.
                if(id==1)
                {
                    page.possessButton.onClick.Invoke();
                    Require(run.OrderTurns==turns+2,"Same vehicle exchange did not execute");
                    CheckExchangeFeedback(page,run);
                }
                ScreenCapture.CaptureScreenshot("/tmp/courier-vehicle-station-"+id+".png");
                if(id<2){run.Node=id+7;run.VehicleIndex=-1;}
                else SetLanguage(CourierLanguage.English);
            }
            else if(step==4)
            {
                var label=page.possessButton.GetComponentInChildren<Text>(true);
                Require(label.enabled&&label.text.Contains("Swap vehicle")&&label.preferredHeight<=label.rectTransform.rect.height+1,"English CTA clipped");
                run.Node=CourierStreetMap.Node("S").StopId;
            }
            else
            {
                Require(!page.possessButton.gameObject.activeSelf,"Swap CTA visible away from station");
                Finish(true,"All three stations: clear roads, node hit targets, Chinese/English CTA, E/current-station/button interaction, five uses and one order turn cost; HUD and map exchange confirmation, including same-type refill.");return;
            }
            step++;
        }catch(Exception e){Finish(false,e.ToString());}
    }
    static void CheckExchangeFeedback(CourierRoutePanel page,CourierRun run)
    {
        Require(page.vehicleArtwork.artwork.sprite==page.vehicleArtwork.catalog.ForVehicle(run.VehicleIndex),"HUD vehicle not refreshed immediately");
        Require(page.mapArtwork.travelerIcon.artwork.sprite==page.mapArtwork.travelerIcon.catalog.ForVehicle(run.VehicleIndex),"Map player not refreshed immediately");
        var overlay=page.transform.Find("FriendlyFeedback");
        var notes=overlay.GetComponentsInChildren<Text>();
        Require(notes.Length==2&&notes.All(n=>n.text.Contains("已换车")),"Exchange needs localized confirmation at both HUD and player");
        Require(notes.Any(n=>n.text.Contains("5")),"Exchange feedback missing refilled durability");
        Require(notes.All(n=>!n.raycastTarget)&&!overlay.GetComponent<CanvasGroup>().blocksRaycasts,"Exchange feedback blocks map input");
        if(!page.MapReducedMotion)
            Require(page.vehicleArtwork.GetComponent<CourierValuePulse>().IsPlaying&&page.mapArtwork.travelerIcon.GetComponent<CourierValuePulse>().IsPlaying,"HUD or map player missing exchange pulse");
    }
    static void CheckLayout(CourierRoutePanel page)
    {
        var map=(RectTransform)page.mapArtwork.transform;
        foreach(var button in page.mapArtwork.stationButtons)
        {
            var rect=(RectTransform)button.transform;var center=new Vector2(rect.anchoredPosition.x,-rect.anchoredPosition.y);
            var bounds=new Rect(center-rect.sizeDelta*.5f-Vector2.one*8,rect.sizeDelta+Vector2.one*16);
            foreach(var edge in CourierStreetMap.Edges)foreach(var point in edge.Points)
                Require(!bounds.Contains(CourierRouteGraphic.MapPoint(point,map.rect.size)),"Station covers road: "+button.name);
            var hit=rect.Find("NodeHitArea") as RectTransform;
            var canvas=rect.GetComponentInParent<Canvas>().rootCanvas;
            var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,hit.TransformPoint(hit.rect.center))};
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"Station node hit obstructed: "+button.name+" / "+string.Join(",",hits.Select(h=>h.gameObject.name))+" / "+data.position);
        }
    }
    static void Finish(bool ok,string message)
    {
        SessionState.SetBool(Key,false);File.WriteAllText(Result,(ok?"PASS ":"FAIL ")+message);
        SetLanguage((CourierLanguage)SessionState.GetInt(Key+"Language",0));EditorApplication.isPlaying=false;
        EditorApplication.delayCall+=()=>{if(scenes!=null)EditorSceneManager.RestoreSceneManagerSetup(scenes);else {var scene=SessionState.GetString(Key+"Scene","");if(scene!="")EditorSceneManager.OpenScene(scene);}};
    }
}
