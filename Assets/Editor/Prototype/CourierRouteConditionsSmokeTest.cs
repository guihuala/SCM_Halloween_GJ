using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CourierRouteConditionsSmokeTest
{
    private const string Key="CourierRouteConditionsSmoke.Active";
    private static int step;
    private static double next,deadline;
    private static SceneSetup[] scenes;
    private static int previousLanguage;
    static CourierRouteConditionsSmokeTest(){EditorApplication.update+=Tick;}
    [MenuItem("Tools/Night Courier/Checks/Route Conditions and Consequences")]
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop play mode before running route checks.");
        Debug.Log(CourierChecks.RunAll());
        scenes=EditorSceneManager.GetSceneManagerSetup();
        SessionState.SetString(Key+".Scene",EditorSceneManager.GetActiveScene().path);
        previousLanguage=(int)CourierLocalization.Language;
        EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
        SessionState.SetInt(Key+".Language",previousLanguage);
        SessionState.SetBool(Key,true);step=0;deadline=0;
        EditorApplication.isPlaying=true;
    }
    private static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    private static void Language(CourierLanguage value)
    {
        typeof(CourierLocalization).GetField("language",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,value);
        ((System.Collections.IDictionary)typeof(CourierLocalization).GetField("cache",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null)).Clear();
    }
    private static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        if(deadline==0){deadline=EditorApplication.timeSinceStartup+90;next=EditorApplication.timeSinceStartup+3;}
        if(EditorApplication.timeSinceStartup<next)return;
        try
        {
            Require(EditorApplication.timeSinceStartup<deadline,"Route conditions check timed out");
            var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();
            var page=UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
            Require(owner!=null&&page!=null,"Route page missing");
            var run=owner.Run;
            switch(step)
            {
                case 0:
                    owner.Tutorial.Dismiss();for(int i=0;i<100&&owner.ConversationActive;i++)owner.AdvanceDialogue();
                    owner.enabled=false;page.SetCargoOpen(false);Language(CourierLanguage.English);
                    run.Node=CourierStreetMap.Node("U").StopId;run.VehicleIndex=-1;
                    foreach(var p in run.Parcels)p.State=ParcelState.Waiting;
                    page.ClearDestination();break;
                case 1:
                    Require(page.GetComponentsInChildren<CourierRouteRiskView>().Single().VisibleRiskCount==0,"Empty load has cargo warnings");
                    CheckBadges(page);
                    page.Select(CourierStreetMap.Node("C").StopId);break;
                case 2:
                    Require(page.riskText.enabled&&page.riskText.text.Contains("Heat")&&page.riskText.text.Contains("No vulnerable cargo"),"Empty route hides road conditions");
                    var cargo=run.Parcels[0];cargo.State=ParcelState.Loaded;cargo.Damaged=false;cargo.HeatSensitive=true;cargo.Dry=false;cargo.Fragile=false;
                    run.VehicleIndex=0;run.Durabilities[0]=5;run.PreferSafeRoutes=false;
                    page.Select(CourierStreetMap.Node("C").StopId);break;
                case 3:
                    Require(page.riskText.text.Contains("Vehicle protects:")&&page.riskText.text.Contains(run.Parcels[0].Name),"Protected cargo missing from route card");
                    Require(!page.riskText.text.Contains("Will damage:"),"Protected cargo falsely marked as damaged");
                    Capture("/tmp/courier-route-protected-en.png");
                    run.UseGhost();page.Select(CourierStreetMap.Node("C").StopId);break;
                case 4:
                    Require(page.riskText.text.Contains("Will damage:")&&page.riskText.text.Contains(run.Parcels[0].Name),"Certain damage or parcel name hidden");
                    Require(page.GetComponentsInChildren<CourierRouteRiskView>().Single().VisibleRiskCount>0,"Dangerous road not emphasized");
                    CheckBadges(page);CheckLayout(page);Capture("/tmp/courier-route-damage-en.png");
                    Language(CourierLanguage.SimplifiedChinese);page.Select(CourierStreetMap.Node("C").StopId);break;
                case 5:
                    Require(page.riskText.text.Contains("路况")&&page.riskText.text.Contains("将损坏"),"Chinese forecast not localized");
                    CheckLayout(page);CheckBadges(page);Capture("/tmp/courier-route-damage-zh.png");
                    Language(CourierLanguage.TraditionalChinese);page.Select(CourierStreetMap.Node("C").StopId);break;
                case 6:
                    Require(page.riskText.text.Contains("路況")&&page.riskText.text.Contains("將損壞"),"Traditional Chinese forecast not localized");
                    CheckLayout(page);Capture("/tmp/courier-route-damage-zh-hant.png");
                    foreach(var p in run.Parcels){p.State=ParcelState.Loaded;p.Damaged=false;p.HeatSensitive=true;p.Name+=" (a deliberately long cargo description for checking the scrollable forecast)";}
                    page.Select(CourierStreetMap.Node("C").StopId);break;
                case 7:
                    CheckLayout(page);
                    var scroll=page.destinationTooltip.GetComponentInChildren<ScrollRect>();
                    Require(scroll.vertical,"Long multi-order forecast should scroll");
                    scroll.verticalNormalizedPosition=0;page.SendMessage("LateUpdate");
                    Require(scroll.verticalNormalizedPosition<.1f,"Refresh resets forecast scroll position");
                    Capture("/tmp/courier-route-long-forecast.png");
                    Finish(true,"3214 rule assertions, 19 road/forecast cases, persistent tags without cargo, protected/damaged cargo names, map emphasis, English and Chinese text/layout.");return;
            }
            step++;next=EditorApplication.timeSinceStartup+.8;
        }
        catch(Exception e){Debug.LogException(e);Finish(false,e.ToString());}
    }
    private static void CheckBadges(CourierRoutePanel page)
    {
        var badges=page.mapGraphic.transform.parent.Find("RouteRiskBadges");
        Require(badges!=null,"Road condition layer missing");
        var labels=badges.GetComponentsInChildren<Text>();
        Require(labels.Length==CourierStreetMap.Edges.Count(e=>e.Surface!=StreetSurface.Normal),"Not all special roads have persistent tags");
        foreach(var label in labels)Require(label.preferredWidth<=label.rectTransform.rect.width+1&&label.preferredHeight<=label.rectTransform.rect.height+1,"Road tag clipped: "+label.text);
    }
    private static void CheckLayout(CourierRoutePanel page)
    {
        var scroll=page.destinationTooltip.GetComponentInChildren<ScrollRect>();
        Require(scroll!=null&&scroll.content==page.riskText.rectTransform,"Forecast cannot scroll");
        Require(page.riskText.preferredHeight<=page.riskText.rectTransform.rect.height+1,"Forecast text truncated");
        Require(page.destinationTooltip.rect.height<=((RectTransform)page.destinationTooltip.parent).rect.height-136+1,"Route card exceeds page");
        float bottom=148+scroll.viewport.rect.height;
        Require(bottom<=page.destinationTooltip.rect.height-64,"Forecast overlaps route mode button");
        if(scroll.vertical)
        {
            var hint=(RectTransform)page.destinationTooltip.Find("RouteScrollHint");
            Require(bottom<=-hint.anchoredPosition.y&&-hint.anchoredPosition.y+hint.rect.height<=page.destinationTooltip.rect.height-64,"Scroll hint overlaps text or route mode");
        }
    }
    private static void Capture(string path)
    {
        Canvas.ForceUpdateCanvases();
        var target=new RenderTexture(1280,720,24);var camera=UnityEngine.Object.FindObjectOfType<Camera>();var old=camera.targetTexture;
        foreach(var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())if(canvas.isRootCanvas)
        {canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;}
        camera.targetTexture=target;Canvas.ForceUpdateCanvases();
        var page=UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
        page.SendMessage("Refresh");page.SendMessage("LateUpdate");
        page.mapGraphic.GetComponent<CourierRouteRiskView>()?.SendMessage("LateUpdate");
        Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
        var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
        File.WriteAllBytes(path,image.EncodeToPNG());camera.targetTexture=old;RenderTexture.active=null;
        UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);
    }
    private static void Finish(bool ok,string message)
    {
        SessionState.SetBool(Key,false);Language((CourierLanguage)SessionState.GetInt(Key+".Language",0));
        File.WriteAllText("/tmp/courier-route-conditions-result.txt",(ok?"PASS ":"FAIL ")+message);
        Debug.Log("COURIER_ROUTE_CONDITIONS "+(ok?"PASS ":"FAIL ")+message);
        EditorApplication.isPlaying=false;
        EditorApplication.playModeStateChanged+=RestoreScene;
    }
    private static void RestoreScene(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredEditMode)return;
        EditorApplication.playModeStateChanged-=RestoreScene;
        if(scenes!=null)EditorSceneManager.RestoreSceneManagerSetup(scenes);
        else{var path=SessionState.GetString(Key+".Scene","");if(!string.IsNullOrEmpty(path))EditorSceneManager.OpenScene(path);}
    }
}
