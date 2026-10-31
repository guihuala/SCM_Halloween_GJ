using System;
using System.Linq;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CourierVehicleArtInstaller
{
    [MenuItem("Tools/Night Courier/Install Vehicle Artwork (preserve layouts)")]
    public static void Install()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach(var path in System.IO.Directory.GetFiles("Assets/Resources/NightVoyage/VehicleIcons","*.png"))
        {
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new Exception("Vehicle sprite missing: "+path);
            if(importer.textureType!=TextureImporterType.Sprite||importer.mipmapEnabled||!importer.alphaIsTransparency||importer.textureCompression!=TextureImporterCompression.Uncompressed)
            {
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
        }
        var catalog=Resources.Load<CourierVehicleIconCatalog>("NightVoyage/VehicleIcons");
        if(catalog==null||catalog.icons.Length!=3||catalog.icons.Select(i=>i.vehicleId).Distinct().Count()!=3)throw new Exception("Vehicle catalog must contain IDs 0, 1 and 2");
        for(int id=0;id<3;id++)if(catalog.ForVehicle(id)==null)throw new Exception("Missing vehicle sprite "+id);
        foreach(var name in new[]{"CourierRoutePanel","CourierVehiclePanel"})
        {
            var path="Assets/Resources/Panel/Courier/"+name+".prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var map=root.GetComponent<CourierRoutePanel>();if(map!=null)map.PrepareVehicleArtwork();
                var detail=root.GetComponent<CourierVehiclePanel>();if(detail!=null)detail.PrepareVehicleArtwork();
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();Debug.Log("COURIER_VEHICLE_ART installed 3 SVG-derived sprites in map, switch button and vehicle details.");
    }
    public static void InstallAndCheck(){Install();CourierVehicleArtSmokeTest.Run();}
}

[InitializeOnLoad]
public static class CourierVehicleArtSmokeTest
{
    private const string Key="CourierVehicleArtwork.Active";
    private static int step;
    private static double next,deadline;
    static CourierVehicleArtSmokeTest(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        Debug.Log("COURIER_RULES PASS "+CourierChecks.RunAll());
        EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");EditorApplication.ExecuteMenuItem("Window/General/Game");
        SessionState.SetBool(Key,true);Application.runInBackground=true;EditorApplication.isPlaying=true;
    }
    private static void Require(bool ok,string text){if(!ok)throw new Exception(text);}
    private static void Check(CourierVehicleIconView view,int id)
    {
        Require(view!=null&&view.artwork!=null,"Vehicle icon view missing");
        Require(!view.artwork.raycastTarget&&view.artwork.preserveAspect,"Vehicle icon blocks input or stretches");
        if(id<0)Require(view.artwork.sprite==null&&!view.artwork.enabled&&view.ghost.gameObject.activeSelf,"Exhaustion retains vehicle artwork instead of ghost");
        else Require(view.artwork.enabled&&view.artwork.sprite==view.catalog.ForVehicle(id)&&!view.ghost.gameObject.activeSelf,"Vehicle artwork ID mismatch: "+id);
        Require(view.GetComponentsInChildren<CourierIconGraphic>(true).Count(g=>g.name=="GhostFallback")==1,"Duplicate ghost fallback");
    }
    private static void CheckDetails(NightCourierPrototype owner)
    {
        var root=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Panel/Courier/CourierVehiclePanel"));
        var detail=root.GetComponent<CourierVehiclePanel>();detail.Bind(owner);Check(detail.currentArtwork,owner.Run.VehicleIndex);
        int parked=owner.Run.Stops[owner.Run.Node].Vehicle;
        if(parked>=0&&parked!=owner.Run.VehicleIndex)Check(detail.parkedArtwork,parked);
        UnityEngine.Object.Destroy(root);
    }
    private static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        if(deadline==0){deadline=EditorApplication.timeSinceStartup+80;next=EditorApplication.timeSinceStartup+3;}
        if(EditorApplication.timeSinceStartup<next)return;
        try
        {
            Require(EditorApplication.timeSinceStartup<deadline,"Vehicle icon smoke test timed out");
            var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();Require(owner!=null&&owner.HasPlanner,"Missing map");
            var page=UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
            switch(step)
            {
                case 0:
                    owner.Tutorial.Dismiss();while(owner.ConversationActive)owner.AdvanceDialogue();Check(page.vehicleArtwork,0);CheckDetails(owner);
                    ScreenCapture.CaptureScreenshot("/tmp/courier-vehicle-scooter.png");break;
                case 1:
                    Require(page.vehicleText.rectTransform.anchoredPosition.x>page.vehicleArtwork.GetComponent<RectTransform>().anchoredPosition.x+68,"Vehicle name overlaps art");
                    CourierChecks.Go(owner.Run,2);owner.Run.TalkToNpc();break;
                case 2:
                    Require(page.possessButton.IsInteractable()&&!page.possessIcon.gameObject.activeSelf,"Bike switch button unavailable");Check(page.parkedVehicleArtwork,1);CheckDetails(owner);
                    page.possessButton.onClick.Invoke();break;
                case 3:
                    Check(page.vehicleArtwork,1);ScreenCapture.CaptureScreenshot("/tmp/courier-vehicle-bicycle.png");CourierChecks.Go(owner.Run,3);break;
                case 4:
                    Require(!page.possessButton.IsInteractable()&&page.possessIcon.gameObject.activeSelf&&!page.parkedVehicleArtwork.gameObject.activeSelf,"Locked pickup lost lock indicator");
                    CourierChecks.Go(owner.Run,5);Require(owner.Run.LoadParcel(owner.Run.Parcels[9]),"Cannot collect repair batch");CourierChecks.Go(owner.Run,3);break;
                case 5:
                    Check(page.parkedVehicleArtwork,2);Require(page.possessButton.IsInteractable(),"Delivered repair did not unlock pickup");page.possessButton.onClick.Invoke();break;
                case 6:
                    Check(page.vehicleArtwork,2);CheckDetails(owner);ScreenCapture.CaptureScreenshot("/tmp/courier-vehicle-pickup.png");GameManager.Instance.PauseGame();break;
                case 7:
                    Check(page.vehicleArtwork,2);GameManager.Instance.ResumeGame();owner.Run.Durabilities[2]=0;Require(owner.DepartTo(4),"Ghost departure failed");break;
                case 8:
                    Check(page.vehicleArtwork,-1);CheckDetails(owner);ScreenCapture.CaptureScreenshot("/tmp/courier-vehicle-ghost.png");owner.RestartShift();break;
                case 9:
                    Check(page.vehicleArtwork,0);Require(owner.Run.VehicleIndex==0,"Restart vehicle mismatch");SessionState.SetBool(Key,false);
                    Debug.Log("COURIER_VEHICLE_ART PASS: all 3 mappings, map header, parked switch art, locked pickup, possession, details, pause, ghost fallback and restart.");EditorApplication.Exit(0);return;
            }
            Debug.Log("COURIER_VEHICLE_ART step "+step+" passed");step++;next=EditorApplication.timeSinceStartup+.8;
        }
        catch(Exception e){SessionState.SetBool(Key,false);Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
