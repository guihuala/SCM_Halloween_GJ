using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

// Pack source sprites without changing their GUIDs, runtime paths or nine-slice borders.
public static class CourierHudAtlasBuilder
{
    public const string AtlasPath="Assets/Art/Atlases/CourierHUD.spriteatlasv2";
    private const string Source="Assets/Resources/NightVoyage/HUD";

    [MenuItem("Tools/Night Courier/Build HUD Sprite Atlas and Export")]
    public static void Build()
    {
        Directory.CreateDirectory("Assets/Art/Atlases");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach(var path in Directory.GetFiles(Source,"*.png"))
        {
            var sourceImporter=(TextureImporter)AssetImporter.GetAtPath(path);
            var settings=new TextureImporterSettings();sourceImporter.ReadTextureSettings(settings);
            if(settings.spriteMeshType!=SpriteMeshType.FullRect)
            {settings.spriteMeshType=SpriteMeshType.FullRect;sourceImporter.SetTextureSettings(settings);sourceImporter.SaveAndReimport();}
        }
        if(!File.Exists(AtlasPath))
        {
            var asset=new SpriteAtlasAsset();
            asset.Add(new UnityEngine.Object[]{AssetDatabase.LoadAssetAtPath<DefaultAsset>(Source)});
            SpriteAtlasAsset.Save(asset,AtlasPath);
            UnityEngine.Object.DestroyImmediate(asset);
            AssetDatabase.ImportAsset(AtlasPath,ImportAssetOptions.ForceSynchronousImport);
        }
        var importer=(SpriteAtlasImporter)AssetImporter.GetAtPath(AtlasPath);
        var packing=importer.packingSettings;
        packing.enableRotation=false;packing.enableTightPacking=false;packing.padding=4;
        importer.packingSettings=packing;importer.includeInBuild=true;
        var texture=importer.textureSettings;
        texture.generateMipMaps=false;texture.readable=false;texture.sRGB=true;texture.filterMode=FilterMode.Bilinear;
        importer.textureSettings=texture;
        var platform=importer.GetPlatformSettings("DefaultTexturePlatform");
        platform.maxTextureSize=1024;platform.textureCompression=TextureImporterCompression.Uncompressed;
        importer.SetPlatformSettings(platform);
        importer.SaveAndReimport();
        AssetDatabase.SaveAssets();
        ExportAndCheck();
    }
    [Serializable] private sealed class Entry
    {
        public string name,source;
        public Rect rect;
        public Vector4 border;
        public Vector2 pivot;
    }
    [Serializable] private sealed class Manifest
    {
        public string image="hud-atlas.png",origin="bottom-left",atlas=AtlasPath;
        public int width,height,padding=4;
        public Entry[] sprites;
    }
    private static Rect PackedRect(Sprite sprite,Texture2D page)
    {
        var uv=UnityEditor.Sprites.SpriteUtility.GetSpriteUVs(sprite,true);
        float left=uv.Min(v=>v.x),bottom=uv.Min(v=>v.y),right=uv.Max(v=>v.x),top=uv.Max(v=>v.y);
        return new Rect(Mathf.Round(left*page.width),Mathf.Round(bottom*page.height),Mathf.Round((right-left)*page.width),Mathf.Round((top-bottom)*page.height));
    }
    private static void ExportAndCheck()
    {
        var atlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath);
        if(atlas==null)throw new Exception("HUD atlas did not import");
        var sources=Directory.GetFiles(Source,"*.png").OrderBy(p=>p,StringComparer.Ordinal).Select(AssetDatabase.LoadAssetAtPath<Sprite>).ToArray();
        if(sources.Length!=36||atlas.spriteCount!=sources.Length)throw new Exception("HUD atlas must contain all 36 sprites: "+atlas.spriteCount);
        SpriteAtlasUtility.PackAtlases(new[]{atlas},EditorUserBuildSettings.activeBuildTarget,false);
        var sprites=new Sprite[atlas.spriteCount];atlas.GetSprites(sprites);
        var pages=sources.Select(s=>UnityEditor.Sprites.SpriteUtility.GetSpriteTexture(s,true)).Distinct().ToArray();
        if(pages.Length!=1)throw new Exception("HUD atlas should fit a single texture page: "+string.Join(", ",pages.Select(t=>t.name+" "+t.width+"x"+t.height)));
        foreach(var source in sources)
        {
            if(source==null||!atlas.CanBindTo(source))throw new Exception("Atlas cannot bind source sprite");
            var packed=sprites.Single(s=>s.name.Replace("(Clone)","")==source.name);
            if(packed.packingRotation!=SpritePackingRotation.None||packed.border!=source.border)throw new Exception("Rotation or nine-slice border changed: "+source.name);
        }
        var manifest=new Manifest{width=pages[0].width,height=pages[0].height,sprites=sources.OrderBy(s=>s.name,StringComparer.Ordinal).Select(s=>new Entry{
            name=s.name.Replace("(Clone)",""),source=Source+"/"+s.name.Replace("(Clone)","")+".png",
            rect=PackedRect(s,pages[0]),border=s.border,pivot=s.pivot
        }).ToArray()};
        Directory.CreateDirectory("ArtPreviews");
        var target=RenderTexture.GetTemporary(manifest.width,manifest.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
        var previous=RenderTexture.active;
        var output=new Texture2D(manifest.width,manifest.height,TextureFormat.RGBA32,false);
        try
        {
            Graphics.Blit(pages[0],target);RenderTexture.active=target;
            output.ReadPixels(new Rect(0,0,manifest.width,manifest.height),0,0);output.Apply();
            File.WriteAllBytes("ArtPreviews/hud-atlas.png",output.EncodeToPNG());
            File.WriteAllText("ArtPreviews/hud-atlas.json",JsonUtility.ToJson(manifest,true));
        }
        finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(output);foreach(var sprite in sprites)UnityEngine.Object.DestroyImmediate(sprite);}
        Debug.Log("COURIER_HUD_ATLAS PASS: 36 sprites on one "+manifest.width+"x"+manifest.height+" page; source bindings, no rotation, borders and PNG/JSON export verified.");
    }
    public static void BuildAndPlayCheck(){Build();CourierMapPageSmokeTest.Run();}
}
