using System;
using System.IO;
using System.Linq;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Run in an isolated project copy: enters play mode and exits the editor on completion.
[InitializeOnLoad]
public static class CourierOptimizationSmokeTest
{
    private const string Key="CourierOptimizationSmoke.Active";
    private static int step;
    private static double next,deadline;
    static CourierOptimizationSmokeTest(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        Debug.Log(CourierChecks.RunAll());
        EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    private static void Require(bool valid,string message){if(!valid)throw new Exception(message);}
    private static void Click(Button button)
    {
        Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;
        var canvas=button.GetComponentInParent<Canvas>().rootCanvas;
        var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
        var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"Blocked button: "+button.name+" hits "+string.Join(",",hits.Select(h=>h.gameObject.name)));
        ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
    }
    private static void Capture(string name,Component panel)
    {
        var canvas=panel.GetComponentInParent<Canvas>().rootCanvas;var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float distance=canvas.planeDistance;
        var go=new GameObject("OptimizationPreviewCamera",typeof(Camera));var camera=go.GetComponent<Camera>();
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=CourierColors.Night;camera.orthographic=true;
        var target=new RenderTexture(1920,1080,24);camera.targetTexture=target;var old=RenderTexture.active;
        var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        try
        {
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
            Canvas.ForceUpdateCanvases();
            var page=panel as CourierRoutePanel;if(page!=null){page.ApplyFullscreenLayout();Canvas.ForceUpdateCanvases();}
            camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();
            Directory.CreateDirectory("ArtPreviews/Optimization");File.WriteAllBytes("ArtPreviews/Optimization/"+name+".png",texture.EncodeToPNG());
        }
        finally{canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=distance;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(texture);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(go);}
    }
    private static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        if(deadline==0)deadline=EditorApplication.timeSinceStartup+120;
        if(EditorApplication.timeSinceStartup>deadline){Finish(false,"Timeout at step "+step);return;}
        if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.8;
        try
        {
            var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();
            if(owner==null||!owner.HasPlanner)return;
            var page=UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
            var hud=UIManager.Instance.GetPanel("CourierHudPanel") as CourierHudPanel;
            var run=owner.Run;
            switch(step)
            {
                case 0:
                    typeof(NightCourierPrototype).GetField("cruisePlaybackSpeed",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(owner,.01f);
                    for(int i=0;i<200&&owner.ConversationActive;i++)owner.AdvanceDialogue();
                    run.Story=null;run.EventProbability=0;run.Node=2;run.VehicleIndex=0;run.LoadParcel(run.Parcels[4]);page.Select(5);break;
                case 1:
                    Require(page.routeModeButton!=null&&page.riskText.text.Contains("Cargo intact"),"Safe forecast missing");
                    var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                    var routeField=typeof(CourierRoutePanel).GetField("route",flags);var refresh=typeof(CourierRoutePanel).GetMethod("Refresh",flags);
                    refresh.Invoke(page,null);var cachedRoute=routeField.GetValue(page);
                    for(int i=0;i<60;i++)refresh.Invoke(page,null);
                    Require(ReferenceEquals(cachedRoute,routeField.GetValue(page)),"Idle frames keep rebuilding route presentation");
                    Capture("safe-route",page);Click(page.routeModeButton);Require(!run.PreferSafeRoutes,"Route mode toggle failed");break;
                case 2:
                    Require(page.riskText.text.Contains("Will damage: Spring Mead"),"Fast route did not explain damage");Capture("fastest-route",page);
                    Click(page.routeModeButton);Click(page.departButton);break;
                case 3:
                    Require(run.State==ShiftState.Driving,"Depart failed");
                    run.Tick(2);break;
                case 4:
                    Require(hud.progressFill.fillAmount>0&&hud.progressFill.fillAmount<1,"HUD progress did not animate");
                    owner.SkipRide();Require(!owner.Tutorial.Arrived,"Tutorial completed at a junction");break;
                case 5:
                    CourierChecks.Go(run,5);Require(!run.Results[0].Damaged,"Safe route damaged cargo");
                    CourierChecks.CompleteAll(run);
                    for(int i=0;i<200&&owner.ConversationActive;i++)owner.AdvanceDialogue();break;
                case 6:
                    var result=UIManager.Instance.GetPanel("CourierResultPanel") as CourierResultPanel;
                    Require(result!=null&&result.orderCards!=null,"Result album missing");
                    Require(result.orderCards.childCount==11,"Not all delivery cards listed");
                    Require(result.orderCards.GetComponentsInChildren<Image>().Count(i=>i.name=="CargoArtwork"&&i.sprite!=null)==11,"Cargo illustrations missing");
                    Require(result.orderCards.GetComponentsInChildren<Image>().Count(i=>i.name=="RecipientPortrait"&&i.sprite!=null)==11,"Recipient portraits missing");
                    Require(result.orderHistory.content.rect.height>result.orderHistory.viewport.rect.height,"Result history cannot scroll");
                    Capture("order-history-top",result);result.orderHistory.verticalNormalizedPosition=0;break;
                case 7:
                    var end=UIManager.Instance.GetPanel("CourierResultPanel") as CourierResultPanel;
                    Require(end.orderHistory.verticalNormalizedPosition<.05f,"Cannot reach final order");Capture("order-history-bottom",end);
                    Click(end.restartButton);break;
                case 8:
                    Require(owner.Run.Results.Count==0&&!owner.Tutorial.Finished&&!owner.Tutorial.Arrived,"Restart retained tutorial/results");
                    Require(UIManager.Instance.GetPanel("CourierTutorialPanel")!=null,"Tutorial overlay not restored");
                    Finish(true,"Rules, safe/fastest raycast clicks, cargo forecast, departure, progress, waypoint tutorial, all deliveries, scrollable result history and restart.");return;
            }
            step++;
        }
        catch(Exception e){Debug.LogException(e);Finish(false,e.ToString());}
    }
    private static void Finish(bool pass,string message)
    {
        SessionState.SetBool(Key,false);Debug.Log((pass?"OPTIMIZATION PASS: ":"OPTIMIZATION FAIL: ")+message);EditorApplication.Exit(pass?0:1);
    }
}
