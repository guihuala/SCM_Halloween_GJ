using System;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CourierHudMotionSmokeTest
{
    private const string Key = "CourierHudMotionSmoke.Active";
    private static int step;
    private static double next, deadline;
    private static GameObject fixture;
    private static CourierHudMotion motion;
    private static Vector2 home = new Vector2(100, 200);
    static CourierHudMotionSmokeTest() { EditorApplication.update += Tick; }
    public static void Run()
    {
        step=0;deadline=0;
        EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    private static void Require(bool ok, string message) { if(!ok)throw new Exception(message); }
    private static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        if(deadline==0){deadline=EditorApplication.timeSinceStartup+50;next=EditorApplication.timeSinceStartup+3;}
        try
        {
            Require(EditorApplication.timeSinceStartup<deadline,"HUD motion timed out");
            if(EditorApplication.timeSinceStartup<next)return;
            var hud=UnityEngine.Object.FindObjectOfType<CourierHudPanel>();
            Require(hud!=null,"Missing HUD");
            switch(step)
            {
                case 0:
                    Require(hud.advanceButton.gameObject.activeSelf,"Dialogue control not visible");
                    var group=hud.advanceButton.GetComponent<CanvasGroup>();
                    Require(group!=null&&group.alpha>.99f&&group.interactable,"HUD entrance did not settle");
                    fixture=new GameObject("HudMotionProbe",typeof(RectTransform));
                    ((RectTransform)fixture.transform).anchoredPosition=home;
                    motion=new CourierHudMotion();motion.Present(fixture,true,Vector2.up*40,true);
                    Time.timeScale=0;
                    motion.RestoreLayout();motion.Present(fixture,false,Vector2.up*40,false);
                    var probe=fixture.GetComponent<CanvasGroup>();
                    Require(!probe.interactable&&!probe.blocksRaycasts,"Outgoing HUD still receives input");
                    Require(fixture.activeSelf&&probe.alpha>0,"HUD disappeared before its exit");
                    break;
                case 1:
                    // Reverse a transition at its current pose, without accumulating layout offsets.
                    var rect=(RectTransform)fixture.transform;
                    var before=rect.anchoredPosition;
                    motion.RestoreLayout();motion.Present(fixture,true,Vector2.up*40,false);
                    Require(Vector2.Distance(before,rect.anchoredPosition)<10,"Reversing motion jumped");
                    for(int i=0;i<120;i++){motion.RestoreLayout();motion.Present(fixture,true,Vector2.up*40,false,1f/60);}
                    Require(Vector2.Distance(rect.anchoredPosition,home)<.01f,"Repeated refresh drifted from home");
                    for(int i=0;i<120;i++){motion.RestoreLayout();motion.Present(fixture,false,Vector2.up*40,false,1f/60);}
                    Require(!fixture.activeSelf,"Exit never deactivated the HUD at timeScale zero");
                    motion.RestoreLayout();motion.Present(fixture,true,Vector2.up*40,true);
                    Require(fixture.activeSelf&&rect.anchoredPosition==home,"Reduced motion did not settle immediately");
                    var feedback=hud.advanceButton.GetComponent<CourierButtonFeedback>();
                    Require(feedback!=null,"Missing button feedback");feedback.Play();
                    var burst=hud.advanceButton.GetComponentInChildren<CourierClickBurst>(true);
                    Require(burst!=null&&burst.enabled&&!burst.raycastTarget,"Click burst missing or intercepts pointer");
                    hud.advanceButton.gameObject.SetActive(false);
                    Require(!burst.enabled,"Disabling button left a live burst");
                    break;
                case 2:
                    Time.timeScale=1;UnityEngine.Object.Destroy(fixture);
                    SessionState.SetBool(Key,false);
                    Debug.Log("COURIER_HUD_MOTION PASS: entrance, inert exit, interrupted reversal, fixed home pose, pause, reduced motion, click burst and disable cleanup.");
                    EditorApplication.Exit(0);return;
            }
            step++;next=EditorApplication.timeSinceStartup+.1;
        }
        catch(Exception error)
        {
            Time.timeScale=1;SessionState.SetBool(Key,false);Debug.LogException(error);EditorApplication.Exit(1);
        }
    }
}
