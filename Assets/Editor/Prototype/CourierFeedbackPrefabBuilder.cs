using System;
using System.IO;
using System.Linq;
using NightCourier;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class CourierFeedbackPrefabBuilder
{
    private const string Folder="Assets/Resources/Feedback";
    [MenuItem("Tools/Night Courier/Install Interaction Feedback (preserve layouts)")]
    public static void Install()
    {
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        BuildHint();BuildEffect();
        var hint=AssetDatabase.LoadAssetAtPath<CourierHintView>(Folder+"/CourierHint.prefab");
        var effect=AssetDatabase.LoadAssetAtPath<CourierInteractionEffect>(Folder+"/CourierInteractionEffect.prefab");
        var paths=Directory.GetFiles("Assets/Resources/Panel/Courier","*.prefab")
            .Concat(new[]{"Assets/Prefab/UI/CourierCargoRow.prefab"});
        foreach(var path in paths)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var tooltip in root.GetComponentsInChildren<CourierTooltip>(true))
                {
                    tooltip.viewPrefab=hint;
                    if(tooltip.bubble!=null)Object.DestroyImmediate(tooltip.bubble.gameObject);
                    tooltip.bubble=null;tooltip.label=null;
                }
                foreach(var button in root.GetComponentsInChildren<Button>(true))
                {
                    var feedback=button.GetComponent<CourierButtonFeedback>()??button.gameObject.AddComponent<CourierButtonFeedback>();feedback.effectPrefab=effect;
                    var old=button.GetComponent<CourierFocusRing>();
                    if(old!=null){if(old.ring!=null)old.ring.enabled=false;Object.DestroyImmediate(old);}
                    if(button.GetComponent<CourierIconHint>()==null)
                    {
                        var label=button.GetComponentInChildren<Text>(true);
                        button.gameObject.AddComponent<CourierIconHint>().message=label!=null?label.text:"Activate";
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();Debug.Log("COURIER_FEEDBACK_PREFABS PASS: reusable tooltip and interaction effects installed without rebuilding layouts.");
    }
    public static void InstallAndCheck(){Install();CourierFeedbackSmokeTest.Run();}
    private static RectTransform Rect(string name,Transform parent=null)
    {
        var root=new GameObject(name,typeof(RectTransform));root.transform.SetParent(parent,false);
        return root.GetComponent<RectTransform>();
    }
    private static void BuildHint()
    {
        string path=Folder+"/CourierHint.prefab";if(File.Exists(path))return;
        var rect=Rect("CourierHint");rect.pivot=new Vector2(0,1);rect.sizeDelta=new Vector2(360,100);
        var bg=rect.gameObject.AddComponent<Image>();bg.color=new Color(.025f,.04f,.055f,.98f);bg.raycastTarget=false;
        var view=rect.gameObject.AddComponent<CourierHintView>();view.group=rect.gameObject.AddComponent<CanvasGroup>();view.group.blocksRaycasts=false;view.group.interactable=false;
        var outline=rect.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.45f,.94f,.77f,.65f);outline.effectDistance=new Vector2(1,-1);
        var text=Rect("Hint",rect);text.anchorMin=Vector2.zero;text.anchorMax=Vector2.one;text.offsetMin=new Vector2(16,16);text.offsetMax=new Vector2(-16,-16);
        view.label=text.gameObject.AddComponent<Text>();view.label.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/NightVoyage/Fonts/Nunito-SemiBold.ttf");view.label.fontSize=24;
        view.label.color=new Color(.92f,.95f,.93f);view.label.lineSpacing=1.15f;view.label.raycastTarget=false;view.label.alignment=TextAnchor.MiddleLeft;
        PrefabUtility.SaveAsPrefabAsset(rect.gameObject,path);Object.DestroyImmediate(rect.gameObject);
    }
    private static void BuildEffect()
    {
        string path=Folder+"/CourierInteractionEffect.prefab";if(File.Exists(path))return;
        var rect=Rect("CourierInteractionEffect");rect.sizeDelta=new Vector2(64,64);
        var effect=rect.gameObject.AddComponent<CourierInteractionEffect>();effect.group=rect.gameObject.AddComponent<CanvasGroup>();
        effect.group.alpha=0;effect.group.blocksRaycasts=false;effect.group.interactable=false;
        effect.edges=new Image[4];
        for(int i=0;i<4;i++)
        {
            var edge=Rect(new[]{"Top","Bottom","Left","Right"}[i],rect);
            edge.anchorMin=i<2?new Vector2(0,i==0?1:0):new Vector2(i==2?0:1,0);
            edge.anchorMax=i<2?new Vector2(1,i==0?1:0):new Vector2(i==2?0:1,1);
            edge.pivot=i<2?new Vector2(.5f,i==0?1:0):new Vector2(i==2?0:1,.5f);
            edge.sizeDelta=i<2?new Vector2(0,2):new Vector2(2,0);edge.anchoredPosition=Vector2.zero;
            effect.edges[i]=edge.gameObject.AddComponent<Image>();effect.edges[i].color=effect.focusColor;effect.edges[i].raycastTarget=false;
        }
        PrefabUtility.SaveAsPrefabAsset(rect.gameObject,path);Object.DestroyImmediate(rect.gameObject);
    }
}
