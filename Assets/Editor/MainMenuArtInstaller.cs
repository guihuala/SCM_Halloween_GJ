using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MainMenuArtInstaller
{
    private const string Art = "Assets/Art/Sprite/UI/Mainmenu/";
    private const string Scene = "Assets/Scenes/MainMenu.unity";
    private static readonly string[] Names = { "mainmenu_background", "mainmenu_title", "mainmenu_goolie", "mainmenu_stamps_right", "mainmenu_stampString_right", "mainmenu_QRCode" };
    private static readonly Dictionary<string, RectInt> Bounds = new Dictionary<string, RectInt>();

    private static Sprite Crop(string name)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(File.ReadAllBytes(Art + name + ".png"));
        int x0=texture.width, y0=texture.height, x1=-1, y1=-1;
        var pixels=texture.GetPixels32();
        for(int y=0;y<texture.height;y++) for(int x=0;x<texture.width;x++)
            if(pixels[y*texture.width+x].a>0) { x0=Math.Min(x0,x);y0=Math.Min(y0,y);x1=Math.Max(x1,x);y1=Math.Max(y1,y); }
        if(x1<0) throw new Exception("Empty menu layer: "+name);
        var bounds=new RectInt(x0,y0,x1-x0+1,y1-y0+1);Bounds[name]=bounds;
        var cropped=new Texture2D(bounds.width,bounds.height,TextureFormat.RGBA32,false);
        cropped.SetPixels(texture.GetPixels(bounds.x,bounds.y,bounds.width,bounds.height));cropped.Apply();
        Directory.CreateDirectory(Art+"Cropped");
        string path=Art+"Cropped/"+name+".png";
        File.WriteAllBytes(path,cropped.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(cropped);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.maxTextureSize=2048;importer.spritePixelsPerUnit=100;importer.SaveAndReimport();
        Debug.Log("MENU_CROP "+name+" "+bounds);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.gameObject.layer=5;
        r.SetParent(parent,false);Place(r,position,size);return r;
    }
    private static void Place(RectTransform r,Vector2 position,Vector2 size)
    { r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=position;r.sizeDelta=size; }

    [MenuItem("Tools/Night Courier/Install Main Menu Artwork")]
    public static void Install()
    {
        var sprites=Names.ToDictionary(n=>n,Crop);
        var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
        var controller=UnityEngine.Object.FindObjectOfType<TitleUIController>();
        if(controller==null)throw new Exception("Main menu controller missing");
        var bg=controller.GetComponent<RectTransform>();
        var canvas=controller.GetComponentInParent<Canvas>();
        canvas.GetComponent<CanvasScaler>().screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        var background=bg.GetComponent<Image>();background.color=new Color(.57f,.62f,.57f);background.sprite=null;
        var old=bg.Find("MainMenuArtwork");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var board=Rect("MainMenuArtwork",bg,Vector2.zero,new Vector2(1920,1080));board.SetAsFirstSibling();
        board.anchorMin=Vector2.zero;board.anchorMax=Vector2.one;board.offsetMin=board.offsetMax=Vector2.zero;
        var layers=new List<MainMenuMotion.Layer>();
        for(int i=0;i<Names.Length;i++)
        {
            string name=Names[i];var bounds=Bounds[name];
            var position=new Vector2(bounds.x+bounds.width*.5f-960,bounds.y+bounds.height*.5f-540);
            var rect=Rect(name,board,position,new Vector2(bounds.width,bounds.height));
            var image=rect.gameObject.AddComponent<Image>();image.sprite=sprites[name];image.raycastTarget=false;
            if(i==0)
            {
                rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
                var fit=rect.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectRatio=1920f/1080f;fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;
                continue;
            }
            if(i==3) {rect.anchorMin=rect.anchorMax=new Vector2(0,.5f);position.x+=960;}
            if(i==4) {rect.anchorMin=rect.anchorMax=new Vector2(1,0);position+=new Vector2(-960,540);}
            if(i==5) {rect.anchorMin=rect.anchorMax=Vector2.one;position-=new Vector2(960,540);}
            rect.anchoredPosition=position;
            var group=rect.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
            bool ghost=i==2;
            layers.Add(new MainMenuMotion.Layer {rect=rect,group=group,origin=position,
                drift=ghost?new Vector2(8,14):new Vector2(0,3),angle=ghost?1.4f:(i==1?.35f:1.1f),
                depth=ghost?9:3,phase=i*.8f,delay=i*.08f});
        }
        var motion=bg.GetComponent<MainMenuMotion>();if(motion==null)motion=bg.gameObject.AddComponent<MainMenuMotion>();
        var buttons=bg.Find("BtnBox").GetComponent<RectTransform>();
        var layout=buttons.GetComponent<VerticalLayoutGroup>();layout.enabled=false;
        Place(buttons,new Vector2(-535,-235),new Vector2(420,362));
        var ordered=new[]{controller.newGameButton,controller.settingsButton,controller.creditsButton,controller.exitButton};
        for(int i=0;i<ordered.Length;i++)
        {
            var button=ordered[i];var rect=(RectTransform)button.transform;
            Place(rect,new Vector2(0,140-i*92),new Vector2(420,76));
            var image=button.GetComponent<Image>();image.sprite=Resources.Load<Sprite>("NightVoyage/HUD/Button");
            image.type=Image.Type.Sliced;image.color=i==0?new Color(.78f,.91f,.85f):new Color(.87f,.88f,.81f);
            var label=button.GetComponentInChildren<Text>();label.fontSize=i==0?44:36;
            label.color=new Color(.08f,.16f,.15f);label.raycastTarget=false;
            var hover=button.GetComponent<ButtonHoverAnimation>();
            if(hover!=null){hover.animationType=AnimationType.Combined;hover.hoverScale=1.035f;hover.hoverMoveOffset=new Vector2(8,0);hover.hoverRotation=-.6f;hover.enableShake=false;}
            var nav=new Navigation {mode=Navigation.Mode.Explicit,selectOnUp=ordered[(i+3)%4],selectOnDown=ordered[(i+1)%4]};button.navigation=nav;
        }
        var buttonGroup=buttons.GetComponent<CanvasGroup>();if(buttonGroup==null)buttonGroup=buttons.gameObject.AddComponent<CanvasGroup>();
        layers.Add(new MainMenuMotion.Layer{rect=buttons,group=buttonGroup,origin=buttons.anchoredPosition,delay=.4f,interactive=true});
        motion.layers=layers.ToArray();
        motion.background=board.Find("mainmenu_background").GetComponent<Image>();
        motion.background.material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/UI/Materials/MainMenuAtmosphere.mat");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }

    public static void InstallAndCheck()
    {
        try { Install();Verify();Debug.Log("MAINMENU_ART PASS: crops, layer motion, reduced motion, button navigation and 3 aspect ratios");EditorApplication.Exit(0); }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    private static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    private static void Verify()
    {
        var controller=UnityEngine.Object.FindObjectOfType<TitleUIController>();
        var motion=controller.GetComponent<MainMenuMotion>();
        Require(motion.layers.Length==6,"Menu motion layers incomplete");
        motion.Evaluate(3,Vector2.zero);var ghost=motion.layers[1];var first=ghost.rect.anchoredPosition;
        motion.Evaluate(5,Vector2.one);Require(ghost.rect.anchoredPosition!=first,"Ghost did not float");
        motion.reducedMotion=true;motion.Evaluate(6,Vector2.one);
        Require(ghost.rect.anchoredPosition==ghost.origin&&ghost.rect.localRotation==Quaternion.identity,"Reduced motion did not settle artwork");
        motion.reducedMotion=false;motion.Evaluate(0,Vector2.zero);Require(ghost.group.alpha==0,"Entry fade did not reset");
        Require(!motion.layers.Last().group.blocksRaycasts&&!motion.layers.Last().group.interactable,"Invisible buttons accept input during arrival");
        motion.reducedMotion=true;motion.Evaluate(3,Vector2.zero);
        foreach(var layer in motion.layers)Require(layer.group.alpha==1,"Menu layer remains hidden");
        Require(motion.layers.Last().group.interactable,"Visible menu buttons remain disabled");
        foreach(var graphic in controller.transform.Find("MainMenuArtwork").GetComponentsInChildren<Graphic>())Require(!graphic.raycastTarget,"Artwork blocks menu input");
        Require(controller.newGameButton.navigation.selectOnDown==controller.settingsButton,"Keyboard navigation broken");
        var camera=new GameObject("MenuPreviewCamera",typeof(Camera)).GetComponent<Camera>();
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.57f,.62f,.57f);camera.orthographic=true;
        camera.nearClipPlane=.1f;camera.farClipPlane=20;
        var canvas=controller.GetComponentInParent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,800),new Vector2Int(2560,1080)})
        {
            var target=new RenderTexture(size.x,size.y,24);camera.targetTexture=target;
            Canvas.ForceUpdateCanvases();
            foreach(var button in new[]{controller.newGameButton,controller.settingsButton,controller.creditsButton,controller.exitButton})
            {
                var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);
                foreach(var corner in corners){var p=camera.WorldToViewportPoint(corner);Require(p.x>=0&&p.x<=1&&p.y>=0&&p.y<=1,"Menu button outside "+size);}
            }
            camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
            var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();
            Directory.CreateDirectory("ArtPreviews");File.WriteAllBytes("ArtPreviews/mainmenu-"+size.x+"x"+size.y+".png",image.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
        }
        UnityEngine.Object.DestroyImmediate(camera.gameObject);
    }
}
