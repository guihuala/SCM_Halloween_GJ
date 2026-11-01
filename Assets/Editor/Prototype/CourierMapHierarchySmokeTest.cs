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
public static class CourierMapHierarchySmokeTest
{
    const string Flag="/tmp/courier-map-hierarchy-run",Result="/tmp/courier-map-hierarchy-result.txt",Key="CourierMapHierarchy.Active";
    static int step;static double next,deadline;static SceneSetup[] scenes;
    static CourierMapHierarchySmokeTest(){EditorApplication.update+=Tick;}
    [MenuItem("Tools/Night Courier/Checks/Map Visual Hierarchy")]
    public static void Run()
    {
        if(EditorApplication.isPlaying)return;
        scenes=EditorSceneManager.GetSceneManagerSetup();
        SessionState.SetInt(Key+"Language",(int)CourierLocalization.Language);
        EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
        step=0;next=0;deadline=0;
        File.WriteAllText(Result,"Running");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Tick()
    {
        if(File.Exists(Flag)&&!EditorApplication.isCompiling&&!EditorApplication.isPlaying){File.Delete(Flag);Run();}
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        Application.runInBackground=true;EditorApplication.QueuePlayerLoopUpdate();
        var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();if(owner==null||!owner.HasPlanner)return;
        var pump=owner.GetComponent<CourierVehicleStationCheckPump>()??owner.gameObject.AddComponent<CourierVehicleStationCheckPump>();pump.Check=Check;
    }
    static void Check()
    {
        if(!SessionState.GetBool(Key,false))return;
        if(deadline==0){deadline=EditorApplication.timeSinceStartup+90;next=EditorApplication.timeSinceStartup+4;}
        if(EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+2;
        try
        {
            Require(EditorApplication.timeSinceStartup<deadline,"Map hierarchy check timed out");
            var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();
            var page=UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
            if(!owner.Active)return;
            if(GameManager.Instance!=null)GameManager.Instance.ResumeGame();
            if(step==0)
            {
                owner.Tutorial.Dismiss();for(int i=0;i<100&&owner.ConversationActive;i++)owner.AdvanceDialogue();
                owner.enabled=false;
                typeof(CourierLocalization).GetField("language",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,CourierLanguage.SimplifiedChinese);
                ((System.Collections.IDictionary)typeof(CourierLocalization).GetField("cache",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null)).Clear();
                var fixture=new CourierRun(31){Node=CourierStreetMap.Node("S").StopId};
                CourierContentOverrides.Apply(fixture);
                typeof(NightCourierPrototype).GetProperty("Run").SetValue(owner,fixture);
                page.SetCargoOpen(false);page.ClearDestination();page.Bind(owner);
            }
            else if(step==1)
            {
                Require(page.mapArtwork.terrainImage.color.a<page.mapArtwork.routeImage.color.a,"Terrain competes with road network");
                var badges=page.mapGraphic.transform.parent.Find("RouteRiskBadges").GetComponentsInChildren<Text>();
                Require(badges.Length>0&&badges.All(t=>!t.enabled),"Idle road badges still display repeated text");
                Require(badges.All(t=>t.GetComponentInParent<CourierIconHint>().message.Length>0),"Compact road badges lost their details");
                ScreenCapture.CaptureScreenshot("/tmp/courier-map-hierarchy-idle.png");
            }
            else if(step==2)
            {
                int target=Enumerable.Range(0,owner.Run.Stops.Length).First(id=>owner.Run.FindRoute(id).Any(r=>r.StreetPath!=null&&r.StreetPath.Any(e=>e.Surface!=StreetSurface.Normal)));
                page.Select(target);
            }
            else if(step==3)
            {
                Require(page.mapGraphic.SelectedRoute.Count>0,"Selected route missing");
                var badges=page.mapGraphic.transform.parent.Find("RouteRiskBadges").GetComponentsInChildren<Text>();
                Require(badges.Any(t=>t.enabled)&&badges.Any(t=>!t.enabled),"Route details do not distinguish selected roads");
                ScreenCapture.CaptureScreenshot("/tmp/courier-map-hierarchy-selected.png");
            }
            else {Finish(true,"Idle road details compact; selected road labels expanded; all road hints retained; terrain below road contrast; route selection intact.");return;}
            step++;
        }
        catch(Exception e){Finish(false,e.ToString());}
    }
    static void Finish(bool ok,string message)
    {
        SessionState.SetBool(Key,false);File.WriteAllText(Result,(ok?"PASS ":"FAIL ")+message);
        typeof(CourierLocalization).GetField("language",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,(CourierLanguage)SessionState.GetInt(Key+"Language",0));
        ((System.Collections.IDictionary)typeof(CourierLocalization).GetField("cache",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null)).Clear();
        EditorApplication.isPlaying=false;
        EditorApplication.delayCall+=()=>{if(scenes!=null)EditorSceneManager.RestoreSceneManagerSetup(scenes);};
    }
}
