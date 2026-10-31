using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NightCourier;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Common menu surfaces use the HUD's hand-drawn sprites; scene art and baked illustrations stay authored.
public static class CourierSharedArtInstaller
{
    private static readonly Color Ink=new Color(.035f,.055f,.085f);
    private static int changed;
    private static string[] Paths()=>Directory.GetFiles("Assets/Resources/Panel","*.prefab",SearchOption.AllDirectories)
        .Concat(Directory.GetFiles("Assets/Prefab/UI","*.prefab",SearchOption.AllDirectories)).ToArray();
    private static Sprite Load(string name)
    {
        var sprite=Resources.Load<Sprite>("NightVoyage/HUD/"+name);
        if(sprite==null)throw new Exception("Missing hand-drawn UI sprite: "+name);
        return sprite;
    }
    private static bool Builtin(Sprite sprite)=>sprite!=null&&AssetDatabase.GetAssetPath(sprite).Contains("unity_builtin_extra");
    [MenuItem("Tools/Night Courier/Install Hand-drawn Shared UI (preserve layouts)")]
    public static void Install()
    {
        changed=0;AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach(var path in Paths())
        {
            var root=PrefabUtility.LoadPrefabContents(path);bool dirty=false;
            try
            {
                foreach(var image in root.GetComponentsInChildren<Image>(true))
                {
                    string skin=null;bool knob=false;
                    string spritePath=image.sprite!=null?AssetDatabase.GetAssetPath(image.sprite):"";
                    if(spritePath.StartsWith("Assets/Art/Sprite/Icon/",StringComparison.Ordinal))
                    {
                        var names=new Dictionary<string,string>{{"gear","Settings"},{"home","Home"},{"right","Arrow"},{"audioOn","Volume"},{"musicOn","Music"},{"exclamation","Warning"}};
                        if(names.TryGetValue(Path.GetFileNameWithoutExtension(spritePath),out skin))
                        {image.sprite=Load(skin);image.type=Image.Type.Simple;image.preserveAspect=true;dirty=true;changed++;}
                        continue;
                    }
                    bool legacyClose=image.GetComponent<Button>()!=null&&(image.name=="back"||image.name=="BackButton")&&spritePath=="Assets/Art/Sprite/UI/UI_atlas_1.png";
                    if(legacyClose)
                    {
                        image.sprite=Load("Button");image.type=Image.Type.Sliced;image.preserveAspect=false;
                        var rect=new GameObject("HandDrawnCloseIcon",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<RectTransform>();
                        rect.SetParent(image.transform,false);rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
                        rect.anchoredPosition=Vector2.zero;rect.sizeDelta=Vector2.one*Mathf.Min(image.rectTransform.rect.width,image.rectTransform.rect.height)*.62f;
                        var icon=rect.GetComponent<Image>();icon.sprite=Load("Close");icon.color=Ink;icon.preserveAspect=true;icon.raycastTarget=false;
                        dirty=true;changed++;continue;
                    }
                    bool builtin=Builtin(image.sprite);
                    if(builtin)
                    {
                        if(image.GetComponent<Button>()!=null)skin="Button";
                        else if(image.name=="Handle"){skin="Dot";knob=true;}
                        else if(image.name=="Fill")skin="ProgressFill";
                        else if(image.name=="Background")skin="ProgressTrack";
                        else if(root.name=="LoadingScreen")skin="ProgressTrack";
                        else skin="Panel";
                    }
                    else if(image.sprite==null)
                    {
                        if(image.name=="Scrollbar"||image.name=="Handle")skin="ProgressTrack";
                        else if(image.name=="WaitingParcels"||image.name=="LoadedParcels")skin="Card";
                        else if(root.name=="LoadingScreen"&&image.name=="Progress")skin="ProgressTrack";
                    }
                    if(skin==null)continue;
                    var type=image.type;image.sprite=Load(skin);
                    image.type=knob?Image.Type.Simple:type==Image.Type.Filled?Image.Type.Filled:skin=="ProgressFill"?Image.Type.Simple:Image.Type.Sliced;
                    if(knob){image.color=Ink;image.preserveAspect=true;}
                    dirty=true;changed++;
                }
                if(dirty)PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();Debug.Log("COURIER_SHARED_ART installed "+changed+" hand-drawn replacements.");
    }
    public static void InstallAndCheck()
    {
        Install();
        foreach(var path in Paths())
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach(var image in root.GetComponentsInChildren<Image>(true))
            {
                if(Builtin(image.sprite))throw new Exception("Built-in sprite remains: "+path+"/"+image.name);
                if(image.sprite!=null&&AssetDatabase.GetAssetPath(image.sprite).StartsWith("Assets/Art/Sprite/Icon/",StringComparison.Ordinal))
                    throw new Exception("Legacy standalone icon remains: "+path+"/"+image.name);
            }
            foreach(var icon in root.GetComponentsInChildren<CourierIconGraphic>(true))
                if(icon.artwork!=null&&!AssetDatabase.GetAssetPath(icon.artwork).StartsWith("Assets/Resources/NightVoyage/HUD/",StringComparison.Ordinal))
                    throw new Exception("Courier icon uses old art: "+path+"/"+icon.name);
        }
        Debug.Log("COURIER_SHARED_ART PASS: shared and courier prefabs contain no built-in sprites or legacy standalone icons; existing custom illustrations preserved.");
    }
    public static void InstallAndPlayCheck(){InstallAndCheck();GameFlowSmokeTest.Run();}
}
