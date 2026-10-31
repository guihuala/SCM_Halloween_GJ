using System;
using System.IO;
using System.Linq;
using NightCourier;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// An editable fixed-grid sheet, rather than an automatically repacked build-only atlas.
public static class CourierLandmarkAtlasBuilder
{
    public const string SheetPath="Assets/Art/Sprite/MapPlaceholder/Landmarks/landmarks_atlas.png";
    private const string PrefabPath="Assets/Resources/Panel/Courier/CourierRoutePanel.prefab";
    private static string SpriteName(int id)=>"landmark_"+id.ToString("00");

    [MenuItem("Tools/Night Courier/Configure Landmark Atlas")]
    public static void Install()
    {
        // The artist's atlas is the source of truth; never regenerate discarded placeholder art.
        if(!File.Exists(SheetPath))throw new FileNotFoundException("Import or restore the landmark atlas before configuring its slices.",SheetPath);
        AssetDatabase.ImportAsset(SheetPath,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(SheetPath);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;
        importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(SheetPath);
        int cell=texture.width/3;
        if(cell<1||texture.width!=cell*3||texture.height!=cell*2)
            throw new Exception("Landmark atlas must be a 3x2 grid of square cells (768x512, or 1536x1024). Keep the entire canvas.");
        var factory=new SpriteDataProviderFactories();factory.Init();
        var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
        var previous=provider.GetSpriteRects().ToDictionary(r=>r.name);
        var rects=Enumerable.Range(0,6).Select(id=>new SpriteRect{
            name=SpriteName(id),rect=new Rect(id%3*cell,(1-id/3)*cell,cell,cell),
            alignment=SpriteAlignment.Center,pivot=Vector2.one*.5f,
            spriteID=previous.TryGetValue(SpriteName(id),out var old)?old.spriteID:GUID.Generate()
        }).ToArray();
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
        provider.Apply();importer.SaveAndReimport();
        var sprites=AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().ToDictionary(s=>s.name);
        var catalog=Resources.Load<CourierMapArtCatalog>("NightVoyage/MapArtwork");
        if(catalog==null)throw new Exception("Map artwork catalog missing");
        for(int id=0;id<6;id++)
        {
            var entry=catalog.ForStop(id);if(entry==null)throw new Exception("Missing landmark stop "+id);
            entry.sprite=sprites[SpriteName(id)]; // Keep anchor, offset, scale and stop ID untouched.
        }
        EditorUtility.SetDirty(catalog);
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var panel=root.GetComponent<CourierRoutePanel>();
            for(int id=0;id<6;id++)panel.mapArtwork.landmarkImages[id].sprite=catalog.ForStop(id).sprite;
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
        Debug.Log("LANDMARK_ATLAS configured: "+SheetPath+"; top row 0,1,2 / bottom row 3,4,5. Existing PNG pixels and stable sprite IDs preserved.");
    }
    private static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    private static string SpriteKey(Sprite sprite)
    {
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long id);
        return guid+":"+id;
    }
    private static void Verify()
    {
        var catalog=Resources.Load<CourierMapArtCatalog>("NightVoyage/MapArtwork");
        var sprites=AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().ToArray();
        Require(sprites.Length==6&&sprites.Select(s=>s.texture).Distinct().Count()==1,"Landmarks do not share one sheet");
        var keys=Enumerable.Range(0,6).Select(i=>SpriteKey(catalog.ForStop(i).sprite)).ToArray();
        var anchors=Enumerable.Range(0,6).Select(i=>catalog.ForStop(i).anchor).ToArray();
        var offsets=Enumerable.Range(0,6).Select(i=>catalog.ForStop(i).offset).ToArray();
        var scales=Enumerable.Range(0,6).Select(i=>catalog.ForStop(i).scale).ToArray();
        var pixels=File.ReadAllBytes(SheetPath);
        // Reconfiguring must preserve edited artwork and Unity's subasset references.
        Install();
        Require(pixels.SequenceEqual(File.ReadAllBytes(SheetPath)),"Configuration overwrote atlas artwork");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<CourierRoutePanel>();
        for(int i=0;i<6;i++)
        {
            var entry=catalog.ForStop(i);var sprite=entry.sprite;int cell=sprite.texture.width/3;
            Require(SpriteKey(sprite)==keys[i],"Sprite ID changed after reimport: "+i);
            Require(sprite.rect==new Rect(i%3*cell,(1-i/3)*cell,cell,cell),"Wrong atlas cell: "+i);
            Require(entry.anchor==anchors[i]&&entry.offset==offsets[i]&&entry.scale==scales[i],"Landmark placement changed: "+i);
            Require(prefab.mapArtwork.landmarkImages[i].sprite==sprite,"Prefab still references individual artwork: "+i);
        }
        Debug.Log("LANDMARK_ATLAS PASS: six cells, single texture, stable sprite IDs, repeat configuration preserves pixels, catalog/prefab bindings and landmark placement");
    }
    public static void InstallAndCheck()
    {
        try{Install();Verify();EditorApplication.Exit(0);}
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void InstallAndPlayCheck(){Install();Verify();CourierMapBackgroundSmokeTest.RunAndPlayCheck();}
}
