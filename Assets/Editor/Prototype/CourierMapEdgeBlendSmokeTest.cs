using System;
using System.IO;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CourierMapEdgeBlendSmokeTest
{
    private const string Key="CourierMapEdgeBlend.Active";
    private static double readyAt,deadline;
    static CourierMapEdgeBlendSmokeTest(){EditorApplication.update+=Tick;}
    private static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    public static void Run()
    {
        try
        {
            VerifyShader();
            EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
            SessionState.SetBool(Key,true);Application.runInBackground=true;EditorApplication.isPlaying=true;
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    private static void VerifyShader()
    {
        var source=Resources.Load<Material>("NightVoyage/MapEdgeBlend");
        Require(source!=null&&!ShaderUtil.ShaderHasError(source.shader),"Edge blend shader failed to import");
        var camera=new GameObject("EdgeCheckCamera",typeof(Camera)).GetComponent<Camera>();
        camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.orthographic=true;
        var target=new RenderTexture(256,256,24);camera.targetTexture=target;
        var host=new GameObject("EdgeCheckCanvas",typeof(RectTransform),typeof(Canvas));host.layer=31;
        var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
        var imageObject=new GameObject("WhiteTerrain",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));imageObject.layer=31;imageObject.transform.SetParent(host.transform,false);
        var image=imageObject.GetComponent<Image>();var rect=image.rectTransform;
        rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        Canvas.ForceUpdateCanvases();var bounds=rect.rect;
        var material=new Material(source);image.material=material;
        material.SetVector("_SpriteUVRect",new Vector4(0,0,1,1));material.SetFloat("_EdgeFade",.16f);
        var soft=Read(camera,target);
        Require(soft.GetPixel(128,128).r>.98f,"Feather faded the center of the image");
        foreach(var p in new[]{new Vector2Int(1,128),new Vector2Int(254,128),new Vector2Int(128,1),new Vector2Int(128,254)})
            Require(soft.GetPixel(p.x,p.y).a<.005f,"Terrain edge does not blend to its background: "+p+" "+soft.GetPixel(p.x,p.y));
        // Alpha avoids gamma conversion changing the RGB threshold near transparent pixels.
        float middle=soft.GetPixel(20,128).a;
        Require(middle>.04f&&middle<.9f,"Feather has no intermediate transition");
        material.SetFloat("_EdgeFade",0);var hard=Read(camera,target);
        Require(hard.GetPixel(1,128).r>.98f,"Zero feather does not restore the original hard edge");
        Require(Mathf.Approximately(source.GetFloat("_EdgeFade"),.16f),"Shared material was modified");
        UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(camera.gameObject);target.Release();
        UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(soft);UnityEngine.Object.DestroyImmediate(hard);
    }
    private static Texture2D Read(Camera camera,RenderTexture target)
    {
        Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
        var result=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
        result.ReadPixels(new Rect(0,0,target.width,target.height),0,0);result.Apply();RenderTexture.active=previous;return result;
    }
    private static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        if(deadline==0){deadline=EditorApplication.timeSinceStartup+60;readyAt=EditorApplication.timeSinceStartup+3;}
        if(EditorApplication.timeSinceStartup<readyAt)return;
        try
        {
            Require(EditorApplication.timeSinceStartup<deadline,"Map edge preview timed out");
            var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();
            if(owner==null||!owner.HasPlanner)return;
            owner.Tutorial.Dismiss();for(int i=0;i<100&&owner.ConversationActive;i++)owner.AdvanceDialogue();
            var page=UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;Require(page!=null,"Map missing");
            // Keep the onboarding banner out of visual QA screenshots of the map boundary.
            var tutorial=UIManager.Instance.GetPanel("CourierTutorialPanel");
            if(tutorial!=null)tutorial.gameObject.SetActive(false);
            foreach(var guide in UnityEngine.Object.FindObjectsOfType<CourierTutorialView>(true))guide.gameObject.SetActive(false);
            page.ApplyFullscreenLayout();var art=page.mapArtwork;
            var instance=art.terrainImage.material;var source=Resources.Load<Material>("NightVoyage/MapEdgeBlend");
            Require(instance!=source&&instance.shader==source.shader,"Terrain is not using an isolated feather material");
            Require(art.routeImage.material.shader!=source.shader,"Feather applied to the hand-drawn roads");
            foreach(var landmark in art.landmarkImages)Require(landmark.material.shader!=source.shader,"Feather applied to landmarks");
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(2560,1080),new Vector2Int(1280,800)})Capture(page,size);
            art.enabled=false;Require(art.terrainImage.material!=instance,"Disable did not restore original terrain material");
            art.enabled=true;art.Apply(page,owner.Run.Stops);
            Require(art.terrainImage.material!=instance&&art.terrainImage.material.shader==source.shader,"Re-enable did not recreate feather material");
            Require(Mathf.Approximately(source.GetFloat("_EdgeFade"),.16f),"Runtime modified shared material asset");
            SessionState.SetBool(Key,false);
            Debug.Log("MAP_EDGE_BLEND PASS: shader pixels, four transparent boundaries, preserved center, zero-width fallback, isolated material, unchanged road/landmark shaders, 3 aspect ratios, disable/re-enable and gameplay previews");
            EditorApplication.Exit(0);
        }
        catch(Exception error){SessionState.SetBool(Key,false);Debug.LogException(error);EditorApplication.Exit(1);}
    }
    private static void Capture(CourierRoutePanel page,Vector2Int size)
    {
        var canvas=page.GetComponentInParent<Canvas>().rootCanvas;var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldDistance=canvas.planeDistance;
        var camera=new GameObject("MapBlendPreview",typeof(Camera)).GetComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=CourierColors.Night;
        var target=new RenderTexture(size.x,size.y,24);camera.targetTexture=target;
        try
        {
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
            Canvas.ForceUpdateCanvases();page.ApplyFullscreenLayout();Canvas.ForceUpdateCanvases();
            var art=page.mapArtwork;var bounds=art.terrainImage.rectTransform.rect;var value=art.terrainImage.material.GetVector("_SpriteUVRect");
            Require(value==UnityEngine.Sprites.DataUtility.GetOuterUV(art.terrainImage.sprite),"Feather UVs do not match the sprite");
            Require(art.routeImage.rectTransform.rect.size==bounds.size,"Road/background alignment changed");
            var image=Read(camera,target);
            // Verify actual image boundaries after Canvas batching, with its offset below the HUD.
            if(art.catalog.terrainEdgeFeather>.0001f)foreach(var direction in new[]{Vector2.left,Vector2.right,Vector2.up,Vector2.down})
            {
                var edge=bounds.center+Vector2.Scale(direction,bounds.size*.5f);
                var inside=PixelAt(image,camera,art.terrainImage.rectTransform,edge-direction*2);
                var outside=PixelAt(image,camera,art.terrainImage.rectTransform,edge+direction*6);
                Require(Mathf.Abs(inside.r-outside.r)+Mathf.Abs(inside.g-outside.g)+Mathf.Abs(inside.b-outside.b)<.06f,"Hard edge remains in gameplay at "+size+" "+direction);
            }
            Require(Mathf.Approximately(art.terrainImage.material.GetFloat("_EdgeFade"),art.catalog.terrainEdgeFeather),"Terrain edge settings were not applied");
            string folder=art.catalog.terrainEdgeFeather>.0001f?"ArtPreviews/MapEdgeBlend":"ArtPreviews/TerrainIntegrated";
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(folder+"/map-"+size.x+"x"+size.y+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
        }
        finally
        {
            canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldDistance;
            camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(camera.gameObject);
            Canvas.ForceUpdateCanvases();page.ApplyFullscreenLayout();
        }
    }
    private static Color PixelAt(Texture2D image,Camera camera,RectTransform rect,Vector2 local)
    {
        var point=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(local));
        return image.GetPixel(Mathf.Clamp(Mathf.RoundToInt(point.x),0,image.width-1),Mathf.Clamp(Mathf.RoundToInt(point.y),0,image.height-1));
    }
}
