using System;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class NightCourierSmokeTest
{
    private const string Key="NightCourierSmoke.Active";
    private static double ready;
    private static int stage;
    private static double deadline;
    static NightCourierSmokeTest()
    {
        EditorApplication.update+=Tick;
        Application.logMessageReceived+=(message,stack,type)=>
        {
            if(SessionState.GetBool(Key,false) && (type==LogType.Exception || type==LogType.Error || type==LogType.Assert))
            { SessionState.SetBool(Key,false); EditorApplication.Exit(1); }
        };
    }
    [MenuItem("Tools/Night Courier/Run Rule Checks")]
    public static void CheckRules() { Debug.Log("COURIER_RULES PASS "+CourierChecks.RunAll()); }
    [MenuItem("Tools/Night Courier/Open Prototype")]
    public static void Open()
    {
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
    }
    public static void Run()
    {
        try { CheckRules(); }
        catch(Exception ex) { Debug.LogException(ex);EditorApplication.Exit(1);return; }
        SessionState.SetBool(Key,true);
        EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
        EditorApplication.isPlaying=true;
    }
    private static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying) return;
        if(deadline==0) deadline=EditorApplication.timeSinceStartup+60;
        if(EditorApplication.timeSinceStartup>deadline)
        { SessionState.SetBool(Key,false);Debug.LogError("Courier smoke timed out at stage "+stage);EditorApplication.Exit(1);return; }
        if(ready==0) ready=EditorApplication.timeSinceStartup+4;
        if(EditorApplication.timeSinceStartup<ready) return;
        try
        {
            var prototype=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();
            if(prototype==null) throw new Exception("Prototype missing from GamePlay scene");
            if(stage==0)
            {
                ScreenCapture.CaptureScreenshot("/tmp/night-courier-dock.png");
                stage++;ready=EditorApplication.timeSinceStartup+1;
            }
            else if(stage==1)
            {
                prototype.Run.LoadParcel(prototype.Run.Parcels[0]);
                prototype.Run.Depart(prototype.Run.Roads[0]);
                GameManager.Instance.PauseGame();
                stage++;ready=EditorApplication.timeSinceStartup+1;
            }
            else if(stage==2)
            {
                if(prototype.Run.Elapsed!=0) throw new Exception("Pause did not freeze simulation");
                GameManager.Instance.ResumeGame();stage++;ready=EditorApplication.timeSinceStartup+1;
            }
            else if(stage==3)
            {
                if(prototype.Run.Elapsed<=0){ready=EditorApplication.timeSinceStartup+.5;return;}
                ScreenCapture.CaptureScreenshot("/tmp/night-courier-road.png");
                stage++;ready=EditorApplication.timeSinceStartup+4;
            }
            else if(stage==4)
            {
                if(prototype.Run.State!=ShiftState.Encounter) { ready=EditorApplication.timeSinceStartup+.5; return; }
                ScreenCapture.CaptureScreenshot("/tmp/night-courier-event.png");
                stage++;ready=EditorApplication.timeSinceStartup+1;
            }
            else
            {
                SessionState.SetBool(Key,false);Debug.Log("COURIER_PLAYMODE PASS scene, pause, resume, road, event");EditorApplication.Exit(0);
            }
        }
        catch(Exception ex) { SessionState.SetBool(Key,false);Debug.LogException(ex);EditorApplication.Exit(1); }
    }
}
