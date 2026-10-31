using System;
using System.Linq;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

[InitializeOnLoad]
public static class CourierMapTravelerSmokeTest
{
    private const string Key="CourierMapTraveler.Active";
    private static int step;
    private static double next,deadline;
    private static Vector3 pausedPosition;
    private static Vector3[] pausedMesh;
    private static float clock,visual,progress;
    private static CourierVehicleIconCatalog originalCatalog,missingCatalog;
    static CourierMapTravelerSmokeTest(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        Debug.Log("COURIER_RULES PASS "+CourierChecks.RunAll());
        EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");EditorApplication.ExecuteMenuItem("Window/General/Game");
        SessionState.SetBool(Key,true);Application.runInBackground=true;EditorApplication.isPlaying=true;
    }
    private static void Require(bool valid,string message){if(!valid)throw new Exception(message);}
    private static void BeginTrip(NightCourierPrototype owner,int id)
    {
        owner.RestartShift();owner.Tutorial.Dismiss();while(owner.ConversationActive)owner.AdvanceDialogue();
        owner.Run.VehicleIndex=id;owner.Run.Durabilities[id]=owner.Run.Vehicles[id].MaxDurability;
        Require(owner.Run.LoadParcel(owner.Run.Parcels[0])&&owner.DepartTo(2),"Cannot start marker test journey");owner.Run.Tick(8);
    }
    private static void CheckMarker(CourierRoutePanel page,NightCourierPrototype owner,int id)
    {
        var art=page.mapArtwork;var marker=art.travelerIcon;var graphic=art.travelOverlay;
        Require(marker!=null&&marker.gameObject.activeInHierarchy,"Travel icon not visible");
        Require(!marker.artwork.raycastTarget&&!marker.ghost.raycastTarget,"Travel icon intercepts map input");
        if(id<0)Require(marker.artwork.sprite==marker.ghostArtwork&&marker.artwork.enabled&&!marker.ghost.gameObject.activeSelf,"Exhausted vehicle did not become a ghost icon");
        else Require(marker.artwork.enabled&&marker.artwork.sprite==marker.catalog.ForVehicle(id)&&!marker.ghost.gameObject.activeSelf,"Wrong moving vehicle sprite "+id);
        Require(!graphic.drawTravelDot,"Legacy center dot duplicates moving icon");
        var run=owner.Run;var r=graphic.rectTransform.rect;
        var point=CourierRouteGraphic.TravelPoint(run,r.size);var expected=graphic.transform.TransformPoint(new Vector3(r.xMin+point.x,r.yMax-point.y,0));
        Require(Vector3.Distance(marker.transform.position,expected)<.1f,"Icon and route progress are out of sync");
        Require(marker.artwork.preserveAspect&&Mathf.Abs(((RectTransform)marker.transform).rect.width-((RectTransform)marker.transform).rect.height)<.1f,"Moving icon is stretched");
        Canvas.ForceUpdateCanvases();var mesh=graphic.canvasRenderer.GetMesh();Require(mesh!=null&&mesh.vertexCount>0,"Progress and breathing halo disappeared");
        var vertices=mesh.vertices;var colors=mesh.colors32;int translucent=0;
        for(int i=0;i<colors.Length;i++)if(colors[i].a>0&&colors[i].a<255)translucent++;
        Require(translucent>0&&graphic.travelHaloRadius>=art.travelerSize*.5f,"Breathing halo missing or fully hidden by the icon");
        var route=page.mapGraphic.canvasRenderer.GetMesh();var selected=(Color32)page.mapGraphic.selectedRoadColor;
        Require(route!=null&&route.colors32.Any(c=>c.r==selected.r&&c.g==selected.g&&c.b==selected.b),"Route highlight changed");
    }
    private static void CheckParked(CourierRoutePanel page,NightCourierPrototype owner,int id)
    {
        var marker=page.mapArtwork.travelerIcon;
        Require(marker.gameObject.activeInHierarchy,"Arrival vehicle marker disappeared");
        if(id<0)Require(marker.artwork.sprite==marker.ghostArtwork&&marker.artwork.enabled&&!marker.ghost.gameObject.activeSelf,"Arriving custom ghost marker missing");
        else Require(marker.artwork.sprite==marker.catalog.ForVehicle(id)&&marker.artwork.enabled&&!marker.ghost.gameObject.activeSelf,"Parked marker retained previous vehicle");
        var rect=page.mapGraphic.rectTransform;var point=CourierRouteGraphic.NodePoint(owner.Run.Stops[owner.Run.Node],rect.rect.size);
        var expected=rect.TransformPoint(new Vector3(rect.rect.xMin+point.x,rect.rect.yMax-point.y,0));
        Require(Vector3.Distance(marker.transform.position,expected)<.1f,"Parked vehicle is not centered on current node");
        Canvas.ForceUpdateCanvases();var mesh=page.mapGraphic.canvasRenderer.GetMesh();var idle=(Color32)page.mapGraphic.idleRoadColor;
        var selected=(Color32)page.mapGraphic.selectedRoadColor;
        Require(mesh!=null&&!mesh.colors32.Any(c=>c.r==selected.r&&c.g==selected.g&&c.b==selected.b),"Previous road remains highlighted after arrival");
    }
    private static void CheckHover(CourierRoutePanel page,NightCourierPrototype owner)
    {
        var events=new PointerEventData(EventSystem.current);
        ExecuteEvents.Execute(page.stopButtons[3].gameObject,events,ExecuteEvents.pointerEnterHandler);
        Require(page.mapGraphic.PreviewRoute.SequenceEqual(owner.Run.FindRoute(3)),"Hover did not preview complete route from current node");
        Canvas.ForceUpdateCanvases();var mesh=page.mapGraphic.canvasRenderer.GetMesh();var green=(Color32)page.mapGraphic.previewRoadColor;
        Require(mesh.colors32.Any(c=>c.r==green.r&&c.g==green.g&&c.b==green.b),"Hover route is not green");
        Require(!page.destinationTooltip.gameObject.activeSelf,"Hover selected destination without clicking");
        ExecuteEvents.Execute(page.stopButtons[3].gameObject,events,ExecuteEvents.pointerExitHandler);
        Require(page.mapGraphic.PreviewRoute.Count==0,"Hover route remains after pointer exit");
        ExecuteEvents.Execute(page.stopButtons[owner.Run.Node].gameObject,events,ExecuteEvents.pointerEnterHandler);
        Require(page.mapGraphic.PreviewRoute.Count==0,"Current node generates a hover route");
    }
    private static void Pause(CourierRoutePanel page,NightCourierPrototype owner)
    {
        Canvas.ForceUpdateCanvases();pausedPosition=page.mapArtwork.travelerIcon.transform.position;
        pausedMesh=page.mapArtwork.travelOverlay.canvasRenderer.GetMesh().vertices;
        clock=owner.Run.Elapsed;progress=owner.Run.Progress;visual=owner.VisualTime;GameManager.Instance.PauseGame();
    }
    private static void CheckPause(CourierRoutePanel page,NightCourierPrototype owner)
    {
        Canvas.ForceUpdateCanvases();var vertices=page.mapArtwork.travelOverlay.canvasRenderer.GetMesh().vertices;
        Require(owner.Run.Elapsed==clock&&owner.Run.Progress==progress&&owner.VisualTime==visual,"Pause advanced simulation or pulse time");
        Require(page.mapArtwork.travelerIcon.transform.position==pausedPosition&&pausedMesh.SequenceEqual(vertices),"Paused icon position or breathing halo changed");
    }
    private static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        if(deadline==0){deadline=EditorApplication.timeSinceStartup+100;next=EditorApplication.timeSinceStartup+3;}
        if(EditorApplication.timeSinceStartup<next)return;
        try
        {
            Require(EditorApplication.timeSinceStartup<deadline,"Map traveler test timeout at "+step);
            var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();Require(owner!=null&&owner.HasPlanner,"Gameplay missing");
            var page=UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
            switch(step)
            {
                case 0:
                    owner.Tutorial.Dismiss();while(owner.ConversationActive)owner.AdvanceDialogue();CheckHover(page,owner);BeginTrip(owner,0);break;
                case 1:CheckMarker(page,owner,0);ScreenCapture.CaptureScreenshot("/tmp/courier-map-traveler-scooter.png");break;
                case 2:BeginTrip(owner,1);break;
                case 3:CheckMarker(page,owner,1);ScreenCapture.CaptureScreenshot("/tmp/courier-map-traveler-bicycle.png");break;
                case 4:BeginTrip(owner,2);break;
                case 5:CheckMarker(page,owner,2);ScreenCapture.CaptureScreenshot("/tmp/courier-map-traveler-pickup.png");break;
                case 6:CheckMarker(page,owner,2);Pause(page,owner);break;
                case 7:
                    CheckPause(page,owner);GameManager.Instance.ResumeGame();
                    owner.Run.Durabilities[2]=1;owner.Run.Tick(8);Require(owner.Run.IsGhost&&owner.Run.Parcels[0].State==ParcelState.Loaded,"Natural mid-road exhaustion did not preserve the order");break;
                case 8:
                    CheckMarker(page,owner,-1);if(owner.Run.State==ShiftState.Encounter)owner.ResolveEncounter(true);
                    ScreenCapture.CaptureScreenshot("/tmp/courier-map-traveler-ghost.png");break;
                case 9:CheckMarker(page,owner,-1);Pause(page,owner);break;
                case 10:
                    CheckPause(page,owner);GameManager.Instance.ResumeGame();
                    for(int i=0;i<12&&owner.Run.State!=ShiftState.Docked;i++){if(owner.Run.State==ShiftState.Encounter)owner.ResolveEncounter(true);else owner.SkipRide();}
                    Require(owner.Run.Node==2,"Ghost travel did not arrive");break;
                case 11:
                    CheckParked(page,owner,-1);owner.RestartShift();break;
                case 12:
                    Require(owner.Run.VehicleIndex==0,"Restart did not restore scooter");CheckParked(page,owner,0);
                    Require(page.mapArtwork.travelOverlay.GetComponentsInChildren<CourierVehicleIconView>(true).Length==1,"Restart duplicated traveler");
                    originalCatalog=page.mapArtwork.travelerIcon.catalog;missingCatalog=ScriptableObject.CreateInstance<CourierVehicleIconCatalog>();page.mapArtwork.travelerIcon.catalog=missingCatalog;
                    BeginTrip(owner,0);break;
                case 13:
                    Require(page.mapArtwork.travelOverlay.drawTravelDot&&!page.mapArtwork.travelerIcon.artwork.enabled&&!page.mapArtwork.travelerIcon.ghost.gameObject.activeSelf,"Missing custom art removes all travel indication");
                    page.mapArtwork.travelerIcon.catalog=originalCatalog;UnityEngine.Object.Destroy(missingCatalog);break;
                case 14:
                    CheckMarker(page,owner,0);SessionState.SetBool(Key,false);Debug.Log("COURIER_MAP_TRAVELER PASS: green hover route and pointer exit, current-node hover ignored, scooter/bicycle/pickup travel, mid-road ghost switch, pause, parked arrival marker, no past-road tint, restart and missing-sprite fallback.");EditorApplication.Exit(0);return;
            }
            Debug.Log("COURIER_MAP_TRAVELER step "+step+" passed");step++;next=EditorApplication.timeSinceStartup+.65;
        }
        catch(Exception error){SessionState.SetBool(Key,false);Debug.LogException(error);EditorApplication.Exit(1);}
    }
    public static void InstallAndCheck(){CourierMapArtworkInstaller.Install();Run();}
}
