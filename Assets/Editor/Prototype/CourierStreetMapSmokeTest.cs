using System;
using System.IO;
using System.Linq;
using NightCourier;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

// This check uses the existing scene/prefabs and leaves the editor open after completion.
[InitializeOnLoad]
public static class CourierStreetMapSmokeTest
{
    private const string Key="CourierStreetMap.Check";
    private static int step;
    private static double next,deadline;
    static CourierStreetMapSmokeTest(){EditorApplication.update+=EnsurePlayerCheck;}
    private static void EnsurePlayerCheck()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        if(UnityEngine.Object.FindObjectOfType<NightCourierPrototype>()==null)return;
        Application.runInBackground=true;
        var pump=UnityEngine.Object.FindObjectOfType<CourierStreetMapCheckPump>();
        if(pump==null)pump=new GameObject("StreetMapCheckPump").AddComponent<CourierStreetMapCheckPump>();
        pump.Check=Tick;
    }
    [MenuItem("Tools/Night Courier/Checks/Shared Street Map (keep editor open)")]
    public static void Run()
    {
        Debug.Log("COURIER_STREETS RULES "+CourierChecks.RunAll());
        step=0;deadline=0;next=EditorApplication.timeSinceStartup+2;
        SessionState.SetBool(Key,true);Application.runInBackground=true;EditorApplication.isPlaying=true;
    }
    private static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    private static void ClickStation(CourierRoutePanel page,int station)
    {
        Canvas.ForceUpdateCanvases();var button=page.mapArtwork.stationButtons[station-6];var rect=(RectTransform)button.transform;
        var canvas=page.GetComponentInParent<Canvas>().rootCanvas;
        var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
        var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>()==button,"Station blocked: "+station);
        ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
    }
    private static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;
        try
        {
            if(deadline==0){deadline=EditorApplication.timeSinceStartup+120;next=EditorApplication.timeSinceStartup+2;return;}
            Require(EditorApplication.timeSinceStartup<deadline,"Street-map integration check timed out at step "+step);
            var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();
            if(owner==null||!owner.HasPlanner)return;
            var page=UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
            Require(page!=null,"Map panel missing");
            if(step==0)
            {
                owner.RestartShift();owner.Tutorial.Dismiss();for(int dialogue=0;dialogue<100&&owner.ConversationActive;dialogue++)owner.AdvanceDialogue();
                Require(!owner.ConversationActive,"Dialogue did not finish during setup");
                page.SetCargoOpen(false);step++;next=EditorApplication.timeSinceStartup+.5;return;
            }
            if(step==1)
            {
                Require(page.stopButtons.Length==6&&owner.Run.Parcels.Count==10,"Stable customer/order bindings changed");
                Require(page.mapArtwork.terrainImage.gameObject.activeSelf&&!page.mapGraphic.drawRegions,"Background missing or covered by procedural regions");
                Require(page.mapArtwork.landmarkLayer.Find("StreetLabel_1a")!=null&&page.mapArtwork.landmarkLayer.Find("StreetLabel_6b")!=null,"Dual entrance labels missing");
                Canvas.ForceUpdateCanvases();
                var hit=page.mapArtwork.landmarkHitAreas[3].rectTransform;
                var canvas=page.GetComponentInParent<Canvas>().rootCanvas;
                var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
                var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(camera,hit.TransformPoint(hit.rect.center)),button=PointerEventData.InputButton.Left};
                var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
                Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>()==page.stopButtons[3],"Destination hit area blocked: "+canvas.renderMode+" "+Screen.width+"x"+Screen.height+" raycasters="+UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.GraphicRaycaster>().Length+" active="+owner.Active+" conversation="+owner.ConversationActive+" hit="+hit.gameObject.activeInHierarchy+" at "+data.position+": "+string.Join(", ",hits.Select(h=>h.gameObject.name+" / "+h.gameObject.transform.parent.name)));
                ExecuteEvents.Execute(page.stopButtons[3].gameObject,data,ExecuteEvents.pointerClickHandler);step++;next=EditorApplication.timeSinceStartup+.5;return;
            }
            if(step==2)
            {
                Require(page.mapGraphic.SelectedRoute.Count==1&&page.destinationTooltip.gameObject.activeInHierarchy,"Click destination did not preview street route");
                Canvas.ForceUpdateCanvases();var mesh=page.mapGraphic.canvasRenderer.GetMesh();
                Require(mesh!=null&&mesh.vertexCount>1000,"Street geometry missing");
                Directory.CreateDirectory("ArtPreviews");ScreenCapture.CaptureScreenshot("ArtPreviews/map-v24-route.png");
                step++;next=EditorApplication.timeSinceStartup+.5;return;
            }
            if(step==3)
            {
                Require(owner.Run.LoadParcel(owner.Run.Parcels[0])&&owner.DepartTo(3),"Cannot start street delivery");
                owner.Run.Tick(2);step++;next=EditorApplication.timeSinceStartup+.5;return;
            }
            if(step==4)
            {
                var graphic=page.mapArtwork.travelOverlay;var rect=graphic.rectTransform.rect;
                var point=CourierRouteGraphic.TravelPoint(owner.Run,rect.size);
                var expected=graphic.transform.TransformPoint(new Vector3(rect.xMin+point.x,rect.yMax-point.y,0));
                Require(Vector3.Distance(page.mapArtwork.travelerIcon.transform.position,expected)<1,"Traveler is not on its street curve");
                Require(owner.Run.Progress==0&&owner.Run.DisplayProgress>0,"Fractional movement missing");
                ScreenCapture.CaptureScreenshot("ArtPreviews/map-v24-moving.png");
                step++;next=EditorApplication.timeSinceStartup+.5;return;
            }
            if(step==5)
            {
                owner.Run.EncounterIndex=0;
                for(int i=0;i<10&&owner.Run.State!=ShiftState.Docked;i++)
                {if(owner.Run.State==ShiftState.Encounter){owner.Run.EncounterIndex=0;owner.Run.ResolveEncounter(true);}else owner.Run.FastForwardTravel();}
                step++;next=EditorApplication.timeSinceStartup+.5;return;
            }
            if(step==6)
            {
                Require(owner.Run.Node==3&&owner.Run.Results.Count==1&&owner.Run.Parcels[0].State==ParcelState.Delivered,"Street journey did not preserve delivery");
                Require(!owner.Run.HasJourney,"Completed route still active");
                for(int dialogue=0;dialogue<100&&owner.ConversationActive;dialogue++)owner.AdvanceDialogue();
                Require(!owner.ConversationActive,"Dialogue did not finish during setup");owner.Run.UseGhost();
                step++;next=EditorApplication.timeSinceStartup+.5;return;
            }
            if(step>=7&&step<16)
            {
                int station=6+(step-7)/3,phase=(step-7)%3;
                if(phase==0)ClickStation(page,station);
                else if(phase==1)
                {
                    Require(page.destinationTooltip.gameObject.activeInHierarchy&&page.riskText.text.Contains("Visit"),"Station effect missing from preview");
                    if(station==6)ScreenCapture.CaptureScreenshot("ArtPreviews/map-v24-station-preview.png");
                }
                else
                {
                    Require(owner.DepartTo(station),"Cannot depart to station");
                    for(int i=0;i<30&&owner.Run.State!=ShiftState.Docked;i++)
                    {
                        if(owner.Run.State==ShiftState.Encounter){owner.Run.EncounterIndex=0;owner.Run.ResolveEncounter(true);}
                        else owner.Run.FastForwardTravel();
                    }
                    Require(owner.Run.Node==station&&owner.Run.HasProtection(station),"Station failed to install protection");
                    Require(!owner.ConversationActive&&owner.Run.Results.Count==1,"Station changed dialogue or orders");
                }
                step++;next=EditorApplication.timeSinceStartup+.5;return;
            }
            if(step==16)
            {
                Require(page.mapArtwork.stationButtons.All(b=>b.GetComponentInChildren<UnityEngine.UI.Text>().text.Contains("Installed")),"Installed status not shown");
                ScreenCapture.CaptureScreenshot("ArtPreviews/map-v24-stations-installed.png");
                step++;next=EditorApplication.timeSinceStartup+.5;return;
            }
            owner.RestartShift();
            Require(!owner.Run.HeatShield&&!owner.Run.Waterproof&&!owner.Run.Suspension&&owner.Run.Parcels.Count==10,"Restart did not reset protection and preserve orders");
            Debug.Log("COURIER_STREETS PASS: dual entrances, click preview, graph mesh, smooth curved movement, automatic junction traversal, delivery, three clickable stations, protection status and restart.");
            SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;
        }
        catch(Exception e){SessionState.SetBool(Key,false);Debug.LogException(e);EditorApplication.isPlaying=false;}
    }
}
