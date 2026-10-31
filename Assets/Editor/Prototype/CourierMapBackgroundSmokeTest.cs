using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using NightCourier;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CourierMapBackgroundSmokeTest
{
    private static bool playCheck;
    public static void RunAndPlayCheck(){playCheck=true;Run();}
    private static void Require(bool value,string message) { if(!value)throw new Exception(message); }
    private static Mesh MeshFor(CourierRouteGraphic graphic)
    {
        using(var helper=new VertexHelper())
        {
            typeof(CourierRouteGraphic).GetMethod("OnPopulateMesh",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(VertexHelper)},null).Invoke(graphic,new object[]{helper});
            var mesh=new Mesh();helper.FillMesh(mesh);return mesh;
        }
    }
    private static bool HasColor(Mesh mesh,Color color)
    { var c=(Color32)color;return mesh.colors32.Any(v=>v.r==c.r&&v.g==c.g&&v.b==c.b); }
    public static void Run()
    {
        try
        {
            CourierMapArtGuideExporter.Export();
            var xml=XDocument.Load("ArtPreviews/MapArtGuide/map-route-guide.svg");
            XNamespace ns="http://www.w3.org/2000/svg";
            Require(xml.Descendants(ns+"circle").Count()==CourierStreetMap.Nodes.Length,"Guide missing map nodes");
            Require(xml.Descendants(ns+"polyline").Count()==CourierStreetMap.Edges.Length,"Guide missing road paths");
            var host=new GameObject("MapCheckCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=host.GetComponent<Canvas>();
            var panel=UnityEngine.Object.Instantiate(Resources.Load<CourierRoutePanel>("Panel/Courier/CourierRoutePanel"),host.transform,false);
            var run=new CourierRun();panel.PrepareMapArtwork();panel.ApplyFullscreenLayout();
            var art=panel.mapArtwork;var map=(RectTransform)art.transform;
            var original=art.catalog;
            if(original.routeArtwork!=null)CheckPaintedNodes(original.routeArtwork);
            var custom=UnityEngine.Object.Instantiate(original);art.catalog=custom;
            foreach(var size in new[]{new Vector2(900,260),new Vector2(1280,740),new Vector2(1856,872),new Vector2(600,900)})
            {
                map.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,size.x);map.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,size.y);
                art.Apply(panel,run.Stops);map.ForceUpdateRectTransforms();
                Require(art.terrainImage.gameObject.activeSelf&&!panel.mapGraphic.drawRegions,"Terrain hidden or covered by procedural regions");
                Require(!art.terrainImage.raycastTarget,"Background intercepts map input");
                Require(art.terrainImage.transform.GetSiblingIndex()<panel.mapGraphic.transform.GetSiblingIndex()&&panel.mapGraphic.transform.GetSiblingIndex()<art.landmarkLayer.GetSiblingIndex(),"Background draw order wrong");
                var bg=art.terrainImage.rectTransform;
                if(custom.routeArtwork!=null)
                {
                    Require(art.routeImage.gameObject.activeSelf&&art.routeImage.sprite==custom.routeArtwork&&!art.routeImage.raycastTarget,"Hand-drawn route missing or blocks input");
                    Require(!panel.mapGraphic.drawBaseRoads&&!panel.mapGraphic.drawBaseNodes,"Procedural roads or dots duplicate hand-drawn art");
                    Require(art.terrainImage.transform.GetSiblingIndex()<art.routeImage.transform.GetSiblingIndex()&&art.routeImage.transform.GetSiblingIndex()<panel.mapGraphic.transform.GetSiblingIndex(),"Hand-drawn route draw order wrong");
                    Require(art.routeImage.rectTransform.rect.size==bg.rect.size&&art.routeImage.rectTransform.position==bg.position,"Route image not aligned to background canvas");
                }
                Require(Mathf.Abs(bg.rect.width/bg.rect.height-1200f/820f)<.001f,"Background canvas distorted");
                foreach(var node in CourierStreetMap.Nodes)
                {
                    var point=CourierRouteGraphic.MapPoint(node.Point,map.rect.size);
                    var routeWorld=map.TransformPoint(new Vector3(map.rect.xMin+point.x,map.rect.yMax-point.y));
                    var artWorld=bg.TransformPoint(new Vector3(bg.rect.xMin+node.Point.X/1200f*bg.rect.width,bg.rect.yMax-node.Point.Y/820f*bg.rect.height));
                    Require(Vector3.Distance(routeWorld,artWorld)<.01f,"Background/route alignment lost at "+size+" node "+node.Id);
                }
                Require(!art.travelOverlay.drawRoads&&art.travelOverlay.drawTravel&&!art.travelOverlay.raycastTarget,"Motion layer no longer independent");
            }
            panel.mapGraphic.SelectedRoute.Clear();panel.mapGraphic.PreviewRoute.Clear();
            var idle=MeshFor(panel.mapGraphic);
            if(custom.routeArtwork!=null)Require(!HasColor(idle,panel.mapGraphic.idleRoadColor),"Idle procedural roads remain visible");
            UnityEngine.Object.DestroyImmediate(idle);
            panel.mapGraphic.SelectedRoute.Add(run.Roads[0]);
            var selected=MeshFor(panel.mapGraphic);Require(HasColor(selected,panel.mapGraphic.selectedRoadColor),"Selected route highlight missing");
            panel.mapGraphic.PreviewRoute.Add(run.Roads[0]);
            var preview=MeshFor(panel.mapGraphic);Require(HasColor(preview,panel.mapGraphic.previewRoadColor),"Hover route highlight missing");
            UnityEngine.Object.DestroyImmediate(selected);UnityEngine.Object.DestroyImmediate(preview);
            var texture=new Texture2D(12,8);var replacement=Sprite.Create(texture,new Rect(0,0,12,8),Vector2.one*.5f);
            custom.terrain=replacement;custom.terrainTint=new Color(.8f,.9f,1);art.Apply(panel,run.Stops);
            Require(art.terrainImage.sprite==replacement&&art.terrainImage.color==custom.terrainTint,"Replacement sprite or tint ignored");
            custom.terrain=null;art.Apply(panel,run.Stops);
            Require(!art.terrainImage.gameObject.activeSelf&&panel.mapGraphic.drawRegions,"Missing-background fallback lost");
            custom.routeArtwork=null;art.Apply(panel,run.Stops);
            Require(!art.routeImage.gameObject.activeSelf&&panel.mapGraphic.drawBaseRoads&&panel.mapGraphic.drawBaseNodes,"Missing-route fallback lost");
            art.catalog=original;art.Apply(panel,run.Stops);
            Require(art.terrainImage.sprite==original.terrain&&!panel.mapGraphic.drawRegions,"Restoring background failed");
            art.enabled=false;
            // Edit-mode checks explicitly exercise the lifecycle, as this is not ExecuteAlways.
            typeof(CourierMapArtwork).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(art,null);
            Require(!art.terrainImage.gameObject.activeSelf&&!art.routeImage.gameObject.activeSelf,"Disabled artwork remains visible");
            art.enabled=true;art.Apply(panel,run.Stops);Require(art.terrainImage.gameObject.activeSelf,"Re-enable did not restore background");
            map.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,1856);
            map.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,872);
            Capture(canvas,panel);
            UnityEngine.Object.DestroyImmediate(replacement);UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(custom);UnityEngine.Object.DestroyImmediate(host);
            Debug.Log("MAP_BACKGROUND PASS: 4 aspect ratios, painted node alignment, isolated route image, no duplicate roads/dots, background/route fallback, input passthrough, selected/preview route meshes, independent motion, re-enable and rendered preview");
            if(playCheck)CourierOptimizationSmokeTest.Run();else EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    private static void CheckPaintedNodes(Sprite sprite)
    {
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
        try
        {
            texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
            Require(texture.GetPixel(30,30).a==0,"Hand-drawn route must retain transparency");
            foreach(var node in CourierStreetMap.Nodes)
            {
                int x=Mathf.RoundToInt(node.Point.X/1200f*texture.width),y=texture.height-1-Mathf.RoundToInt(node.Point.Y/820f*texture.height);
                bool found=false;
                for(int dy=-12;dy<=12&&!found;dy++)for(int dx=-12;dx<=12&&!found;dx++)
                {
                    var c=texture.GetPixel(Mathf.Clamp(x+dx,0,texture.width-1),Mathf.Clamp(y+dy,0,texture.height-1));
                    found=c.a>.8f&&c.r>.65f&&c.g>.4f&&c.b<.65f&&c.r>c.b*1.3f;
                }
                Require(found,"Hand-drawn node is not aligned with graph: "+node.Id);
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(texture);}
    }
    private static void Capture(Canvas canvas,CourierRoutePanel panel)
    {
        var camera=new GameObject("MapCheckCamera",typeof(Camera)).GetComponent<Camera>();
        camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=CourierColors.Night;
        var target=new RenderTexture(1920,1080,24);camera.targetTexture=target;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
        var rect=(RectTransform)panel.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        Canvas.ForceUpdateCanvases();panel.ApplyFullscreenLayout();Canvas.ForceUpdateCanvases();
        panel.mapGraphic.PreviewRoute.Clear();panel.mapGraphic.SetVerticesDirty();
        Canvas.ForceUpdateCanvases();camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
        Directory.CreateDirectory("ArtPreviews");File.WriteAllBytes("ArtPreviews/map-background-and-routes.png",image.EncodeToPNG());
        RenderTexture.active=previous;camera.targetTexture=null;target.Release();
        UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(camera.gameObject);
    }
}
