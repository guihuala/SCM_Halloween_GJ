using System;
using System.Linq;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class CourierMapArtworkInstaller
{
    [MenuItem("Tools/Night Courier/Install Map Artwork (preserve layouts)")]
    public static void Install()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var catalog=Resources.Load<CourierMapArtCatalog>("NightVoyage/MapArtwork");
        if(catalog==null||catalog.terrain==null)throw new Exception("Map artwork catalog or terrain missing");
        for(int i=0;i<6;i++)if(catalog.ForStop(i)==null||catalog.ForStop(i).sprite==null)throw new Exception("Missing landmark "+i);
        const string path="Assets/Resources/Panel/Courier/CourierRoutePanel.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var panel=root.GetComponent<CourierRoutePanel>();panel.PrepareMapArtwork();panel.ApplyFullscreenLayout();
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Debug.Log("COURIER_MAP_ART installed: terrain, 6 separate landmarks, existing buttons, procedural routes and motion overlay.");
    }
    public static void InstallAndCheck(){Install();CourierMapArtworkSmokeTest.Run();}
}

[InitializeOnLoad]
public static class CourierMapArtworkSmokeTest
{
    private const string Key="CourierMapArtwork.Active";
    private static int step;
    private static double next,deadline;
    private static float clock,visual,progress;
    static CourierMapArtworkSmokeTest(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        Debug.Log("COURIER_RULES PASS "+CourierChecks.RunAll());
        EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");EditorApplication.ExecuteMenuItem("Window/General/Game");
        SessionState.SetBool(Key,true);Application.runInBackground=true;EditorApplication.isPlaying=true;
    }
    private static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    private static void CheckLayout(CourierRoutePanel panel,CourierRun run)
    {
        var art=panel.mapArtwork;var map=(RectTransform)art.transform;map.ForceUpdateRectTransforms();
        Require(art.terrainImage.sprite==art.catalog.terrain&&!art.terrainImage.raycastTarget,"Terrain missing or intercepts input");
        Require(art.terrainImage.gameObject.activeSelf&&!panel.mapGraphic.drawRegions,"Background not visible or procedural regions cover it");
        Require(Vector2.Distance(art.terrainImage.rectTransform.rect.size,CourierRouteGraphic.MapBounds(map.rect.size).size)<.1f,"Terrain does not match route canvas");
        Require(panel.mapGraphic.transform.GetSiblingIndex()<art.landmarkLayer.GetSiblingIndex()&&art.landmarkLayer.GetSiblingIndex()<art.travelOverlay.transform.GetSiblingIndex(),"Road/landmark/motion draw order wrong");
        Require(!panel.mapGraphic.drawTravel&&art.travelOverlay.drawTravel&&!art.travelOverlay.drawRoads&&!art.travelOverlay.raycastTarget,"Travel overlay duplicates roads or blocks input");
        for(int i=0;i<6;i++)
        {
            var image=art.landmarkImages[i];var point=CourierRouteGraphic.NodePoint(run.Stops[i],map.rect.size);
            var entry=art.catalog.ForStop(i);var expected=map.TransformPoint(new Vector3(map.rect.xMin+point.x+entry.offset.x,map.rect.yMax-point.y+entry.offset.y,0));
            Require(image.sprite==entry.sprite&&Vector3.Distance(image.transform.position,expected)<.1f,"Landmark not anchored to live node "+i);
            Require(image.preserveAspect&&!image.raycastTarget&&Mathf.Abs(image.rectTransform.rect.width-image.rectTransform.rect.height)<.1f,"Landmark distorted or intercepts input");
            Require(Vector3.Distance(image.transform.position,art.landmarkHitAreas[i].transform.position)<.1f,"Landmark hit region offset "+i);
            Require(panel.stopLabels[i].preferredHeight<=panel.stopLabels[i].rectTransform.rect.height+.1f,"Map label clipped "+i);
        }
    }
    private static void Click(RectTransform rect,Button expected)
    {
        Canvas.ForceUpdateCanvases();
        var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
        var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==expected,"Node click intercepted: "+expected.name);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerClickHandler);
    }
    private static bool HasColor(CourierRouteGraphic graphic,Color color)
    {
        var mesh=graphic.canvasRenderer.GetMesh();if(mesh==null)return false;
        var target=(Color32)color;bool found=mesh.colors32.Any(c=>c.r==target.r&&c.g==target.g&&c.b==target.b);
        return found;
    }
    private static void CheckCompactLayouts(CourierRoutePanel page,CourierRun run)
    {
        var map=(RectTransform)page.mapGraphic.transform.parent;var saved=map.rect.size;
        try
        {
            foreach(var size in new[]{new Vector2(900,260),new Vector2(1280,740),new Vector2(1856,712)})
            {
                map.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,size.x);map.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,size.y);
                for(int i=0;i<6;i++){var p=CourierRouteGraphic.NodePoint(run.Stops[i],size);((RectTransform)page.stopButtons[i].transform).anchoredPosition=new Vector2(p.x-90,-p.y+36);}
                page.mapArtwork.Apply(page,run.Stops);CheckLayout(page,run);
            }
        }
        finally{map.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,saved.x);map.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,saved.y);page.ApplyFullscreenLayout();}
    }
    private static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        if(deadline==0){deadline=EditorApplication.timeSinceStartup+100;next=EditorApplication.timeSinceStartup+3;}
        if(EditorApplication.timeSinceStartup<next)return;
        try
        {
            Require(EditorApplication.timeSinceStartup<deadline,"Map artwork test timeout at "+step);
            var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();Require(owner!=null&&owner.HasPlanner,"Missing gameplay map");
            var page=UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
            switch(step)
            {
                case 0:
                    owner.Tutorial.Dismiss();while(owner.ConversationActive)owner.AdvanceDialogue();break;
                case 1:
                    CheckLayout(page,owner.Run);CheckCompactLayouts(page,owner.Run);
                    for(int i=0;i<6;i++)
                    {
                        Click(page.mapArtwork.landmarkHitAreas[i].rectTransform,page.stopButtons[i]);
                        while(owner.ConversationActive)owner.AdvanceDialogue();page.ApplyFullscreenLayout();
                    }
                    Click(page.stopLabels[2].rectTransform,page.stopButtons[2]);break;
                case 2:
                    Require(page.mapGraphic.SelectedRoute.Count>0&&HasColor(page.mapGraphic,page.mapArtwork.catalog.selectedRoadColor),"Route highlight lost");
                    ScreenCapture.CaptureScreenshot("/tmp/courier-map-art-idle.png");break;
                case 3:
                    Require(owner.Run.LoadParcel(owner.Run.Parcels[0])&&owner.DepartTo(2),"Cannot depart after artwork integration");
                    owner.Run.Tick(8);Require(owner.Run.Progress>0&&owner.Run.State==ShiftState.Driving,"Travel setup did not produce motion");break;
                case 4:
                    Canvas.ForceUpdateCanvases();
                    foreach(var count in page.stopCounts)Require(count.preferredWidth<=count.rectTransform.rect.width+.1f,"Delivery marker text clipped");
                    Require(HasColor(page.mapArtwork.travelOverlay,new Color(.45f,.94f,.77f)),"Moving dot / progress mesh missing");
                    ScreenCapture.CaptureScreenshot("/tmp/courier-map-art-travel.png");clock=owner.Run.Elapsed;progress=owner.Run.Progress;visual=owner.VisualTime;GameManager.Instance.PauseGame();break;
                case 5:
                    Require(owner.Run.Elapsed==clock&&owner.Run.Progress==progress&&owner.VisualTime==visual,"Pause no longer freezes map motion");
                    GameManager.Instance.ResumeGame();owner.SkipRide();Require(owner.Run.State==ShiftState.Encounter,"Skip bypassed road choice");owner.ResolveEncounter(true);owner.SkipRide();break;
                case 6:
                    Require(owner.Run.Node==2&&owner.Run.State==ShiftState.Docked,"Road arrival lost");CheckLayout(page,owner.Run);
                    var original=page.mapArtwork.catalog;var custom=UnityEngine.Object.Instantiate(original);
                    custom.landmarks=original.landmarks.Select(e=>new CourierMapLandmark{stopId=e.stopId,sprite=e.sprite,anchor=e.anchor,offset=e.offset,scale=e.scale}).Reverse().ToArray();
                    custom.ForStop(1).sprite=original.ForStop(0).sprite;custom.ForStop(2).sprite=null;custom.terrain=null;page.mapArtwork.catalog=custom;page.mapArtwork.Apply(page,owner.Run.Stops);
                    Require(page.mapArtwork.landmarkImages[1].sprite==original.ForStop(0).sprite,"Custom landmark replacement or stable ID mapping failed");
                    Require(!page.mapArtwork.terrainImage.gameObject.activeSelf&&!page.mapArtwork.landmarkImages[2].gameObject.activeSelf&&!page.mapArtwork.landmarkHitAreas[2].gameObject.activeSelf,"Missing artwork fallback failed");
                    Click(page.stopLabels[5].rectTransform,page.stopButtons[5]);page.mapArtwork.catalog=original;UnityEngine.Object.Destroy(custom);page.ApplyFullscreenLayout();break;
                case 7:
                    ScreenCapture.CaptureScreenshot("/tmp/courier-map-art-dialogue.png");break;
                case 8:
                    owner.RestartShift();break;
                case 9:
                    CheckLayout(page,owner.Run);
                    Require(page.mapArtwork.GetComponentsInChildren<CourierRouteGraphic>(true).Length==2&&page.mapArtwork.landmarkImages.Length==6,"Restart duplicated map layers");
                    SessionState.SetBool(Key,false);Debug.Log("COURIER_MAP_ART PASS: terrain + six landmarks, 3 aspect ratios, native landmark/label clicks, route tint, moving progress, pause/event/arrival, replacement/fallback, restart.");EditorApplication.Exit(0);return;
            }
            Debug.Log("COURIER_MAP_ART step "+step+" passed");step++;next=EditorApplication.timeSinceStartup+.65;
        }
        catch(Exception error){SessionState.SetBool(Key,false);Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
