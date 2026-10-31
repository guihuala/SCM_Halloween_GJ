using System;
using System.Linq;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class CourierCargoArtInstaller
{
    [MenuItem("Tools/Night Courier/Install Cargo Artwork (preserve layouts)")]
    public static void Install()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        const string directory="Assets/Resources/NightVoyage/CargoIcons";
        foreach(var path in System.IO.Directory.GetFiles(directory,"*.png"))
        {
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new Exception("Cargo texture not imported: "+path);
            if(importer.textureType!=TextureImporterType.Sprite||importer.mipmapEnabled||!importer.alphaIsTransparency||importer.textureCompression!=TextureImporterCompression.Uncompressed)
            {
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }
        var catalog=Resources.Load<CourierCargoIconCatalog>("NightVoyage/CargoIcons");
        if(catalog==null||catalog.icons.Length!=10||catalog.icons.Select(i=>i.parcelId).Distinct().Count()!=10)throw new Exception("Cargo artwork catalog must contain 10 unique parcel IDs");
        for(int id=0;id<10;id++)if(catalog.ForParcel(id)==null)throw new Exception("Missing cargo sprite "+id);
        const string prefabPath="Assets/Prefab/UI/CourierCargoRow.prefab";
        var root=PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var row=root.GetComponent<CourierCargoRow>();row.PrepareArtwork();
            row.nameText.rectTransform.anchoredPosition=new Vector2(112,-8);row.nameText.rectTransform.sizeDelta=new Vector2(294,40);
            row.destinationText.rectTransform.anchoredPosition=new Vector2(112,-52);row.destinationText.rectTransform.sizeDelta=new Vector2(294,40);
            ((RectTransform)row.timeGroup.transform.parent).anchoredPosition=new Vector2(112,-100);
            row.GetComponent<LayoutElement>().preferredHeight=144;row.GetComponent<LayoutElement>().minHeight=144;
            PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Debug.Log("COURIER_CARGO_ART installed 10 cargo sprites; existing page layouts preserved.");
    }
    public static void InstallAndCheck(){Install();CourierCargoArtSmokeTest.Run();}
}

[InitializeOnLoad]
public static class CourierCargoArtSmokeTest
{
    private const string Key="CourierCargoArtwork.Active";
    private static int step;
    private static double next,deadline;
    static CourierCargoArtSmokeTest(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        Debug.Log("COURIER_RULES PASS "+CourierChecks.RunAll());
        EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");EditorApplication.ExecuteMenuItem("Window/General/Game");
        SessionState.SetBool(Key,true);Application.runInBackground=true;EditorApplication.isPlaying=true;
    }
    private static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    private static void CheckRows(CourierRoutePanel page,CourierRun run)
    {
        foreach(var row in page.GetComponentsInChildren<CourierCargoRow>())
        {
            row.Refresh();
            Require(row.cargoArtwork!=null&&row.cargoArtwork.sprite==row.iconCatalog.ForParcel(run.Parcels.IndexOf(row.Item)),"Wrong cargo icon: "+row.Item.Name);
            Require(!row.cargoArtwork.raycastTarget&&row.cargoArtwork.preserveAspect,"Cargo artwork blocks input or stretches");
            Require(row.GetComponentsInChildren<Image>(true).Count(i=>i.name=="CargoArtwork")==1,"Repeated refresh duplicated cargo artwork");
            Require(row.nameText.preferredHeight<=row.nameText.rectTransform.rect.height+.1f&&row.destinationText.preferredHeight<=row.destinationText.rectTransform.rect.height+.1f,"Cargo text clipped");
            Require(row.nameText.rectTransform.anchoredPosition.x>row.cargoArtwork.rectTransform.anchoredPosition.x+row.cargoArtwork.rectTransform.rect.width,"Text overlaps cargo artwork");
        }
    }
    private static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        if(deadline==0){deadline=EditorApplication.timeSinceStartup+80;next=EditorApplication.timeSinceStartup+3;}
        if(EditorApplication.timeSinceStartup<next)return;
        try
        {
            Require(EditorApplication.timeSinceStartup<deadline,"Cargo artwork test timed out");
            var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();
            Require(owner!=null&&owner.HasPlanner,"Gameplay surface missing");
            var page=UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
            switch(step)
            {
                case 0:
                    owner.Tutorial.Dismiss();while(owner.ConversationActive)owner.AdvanceDialogue();page.SetCargoOpen(true);break;
                case 1:
                    CheckRows(page,owner.Run);ScreenCapture.CaptureScreenshot("/tmp/courier-cargo-icons-list.png");
                    var tape=page.GetComponentsInChildren<CourierCargoRow>().First(r=>r.Item==owner.Run.Parcels[0]);
                    tape.OnBeginDrag(new PointerEventData(EventSystem.current));
                    Require(GameObject.Find("DraggedParcel/CargoArtwork")!=null,"Drag feedback lacks cargo artwork");tape.OnEndDrag(null);
                    GameManager.Instance.PauseGame();break;
                case 2:
                    CheckRows(page,owner.Run);GameManager.Instance.ResumeGame();owner.Run.UseGhost();CourierChecks.Go(owner.Run,5);
                    while(owner.ConversationActive)owner.AdvanceDialogue();
                    var batch=page.GetComponentsInChildren<CourierCargoRow>().First(r=>r.Item==owner.Run.Parcels[8]);
                    batch.actionButton.onClick.Invoke();break;
                case 3:
                    CheckRows(page,owner.Run);
                    var loaded=page.GetComponentsInChildren<CourierCargoRow>().First(r=>r.Item==owner.Run.Parcels[8]);
                    Require(loaded.transform.parent==page.loadedContent&&loaded.secondaryArtwork.gameObject.activeSelf,"Batch artwork missing second item");
                    Require(loaded.secondaryArtwork.sprite==loaded.iconCatalog.ForParcel(10)&&!loaded.secondaryArtwork.raycastTarget,"Blood icon mapping or raycast wrong");
                    Require(owner.Run.Parcels[8].State==ParcelState.Loaded&&owner.Run.Parcels[9].State==ParcelState.Loaded,"Artwork changed batch acceptance");
                    ScreenCapture.CaptureScreenshot("/tmp/courier-cargo-icons-batch.png");break;
                case 4:
                    CourierChecks.Go(owner.Run,3);Require(owner.Run.CompletedBatches==1,"Batch delivery failed after artwork integration");
                    owner.RestartShift();break;
                case 5:
                    page.SetCargoOpen(true);CheckRows(page,owner.Run);Require(owner.Run.CompletedBatches==0,"Restart failed");
                    SessionState.SetBool(Key,false);Debug.Log("COURIER_CARGO_ART PASS: 10 sprites, 9 batch cards, both icons in combined batch, drag image, text layout, pause, delivery and restart.");EditorApplication.Exit(0);return;
            }
            Debug.Log("COURIER_CARGO_ART step "+step+" passed");step++;next=EditorApplication.timeSinceStartup+.8;
        }
        catch(Exception error){SessionState.SetBool(Key,false);Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
