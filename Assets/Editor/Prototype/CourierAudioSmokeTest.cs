using System;
using System.IO;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class CourierAudioSmokeTest
{
    private const string Key="CourierAudio.V24";
    private static int step;
    private static double next,deadline;
    private static int cargo,departure,arrival,damage,delivery,possession,repair,wait;
    private static float master,sfx,bgm;
    private static bool saved;
    static CourierAudioSmokeTest(){EditorApplication.update+=Attach;}
    private static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    [MenuItem("Tools/Night Courier/Checks/Audio (keep editor open)")]
    public static void Run()
    {
        try
        {
            File.WriteAllText("/tmp/courier-audio-v24.txt","Running");
            foreach(var cue in CourierSoundscape.CueNames)Validate("Audio/Courier/"+cue);
            var config=JsonUtility.FromJson<CourierAudioConfig>(Resources.Load<TextAsset>("NightVoyage/AudioCues").text);
            Require(config.cues.Length==12,"Expected 12 new cues");
            foreach(var cue in config.cues)Validate(cue.path);
            foreach(string name in new[]{"night_waltz","night_wind","vehicle_engine"})Validate("Audio/Courier/Loops/"+name);
            var configured=new CourierRun();CourierContentOverrides.Apply(configured);
            foreach(var e in configured.Events)Require(Array.Exists(config.cues,c=>c.cue==e.SoundCue),"Unmapped event sound "+e.Title);
            Debug.Log(CourierChecks.RunAll());
            SessionState.SetString(Key+".Scene",EditorSceneManager.GetActiveScene().path);
            EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
            step=0;next=0;deadline=EditorApplication.timeSinceStartup+90;saved=false;
            SessionState.SetBool(Key,true);Application.runInBackground=true;EditorApplication.isPlaying=true;
        }
        catch(Exception e){File.WriteAllText("/tmp/courier-audio-v24.txt","FAIL "+e);Debug.LogException(e);}
    }
    private static void Validate(string path)
    {var clip=Resources.Load<AudioClip>(path);Require(clip!=null&&clip.length>0&&clip.LoadAudioData(),"Cannot decode "+path);}
    private static void Attach()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();if(owner==null)return;
        var pump=owner.GetComponent<CourierStreetMapCheckPump>()??owner.gameObject.AddComponent<CourierStreetMapCheckPump>();pump.Check=Tick;
    }
    private static AudioSource Engine(NightCourierPrototype owner)
    {return Array.Find(owner.GetComponents<AudioSource>(),s=>s.clip!=null&&s.clip.name=="vehicle_engine");}
    private static void Record(CourierSoundscape a)
    {cargo=a.PlayCount("cargo");departure=a.PlayCount("departure");arrival=a.PlayCount("arrival");damage=a.PlayCount("damage");delivery=a.PlayCount("delivered");possession=a.PlayCount("possession");repair=a.PlayCount("repair");wait=a.PlayCount("event_wait");}
    private static void Finish(bool ok,string message)
    {
        if(GameManager.Instance!=null&&GameManager.Instance.CurrentState==GameManager.GameState.Paused)GameManager.Instance.ResumeGame();
        if(saved&&AudioManager.Instance!=null){AudioManager.Instance.ChangeMainVolume(master);AudioManager.Instance.ChangeSfxVolume(sfx);AudioManager.Instance.ChangeBgmVolume(bgm);}
        SessionState.SetBool(Key,false);File.WriteAllText("/tmp/courier-audio-v24.txt",(ok?"PASS ":"FAIL ")+message);
        EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>{var path=SessionState.GetString(Key+".Scene","");if(!string.IsNullOrEmpty(path))EditorSceneManager.OpenScene(path);};
    }
    private static void Tick()
    {
        if(deadline==0)deadline=EditorApplication.timeSinceStartup+90;
        if(EditorApplication.timeSinceStartup<next)return;
        try
        {
            Require(EditorApplication.timeSinceStartup<deadline,"Audio check timed out at "+step);
            var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();if(owner==null||!owner.HasPlanner)return;
            var a=owner.GetComponent<CourierSoundscape>();var r=owner.Run;
            Require(a!=null,"Soundscape not initialized");
            switch(step)
            {
                case 0:
                    owner.enabled=false;owner.Tutorial.Dismiss();
                    foreach(var e in r.Events)Require(a.HasCue(e.SoundCue),"Runtime event cue missing");
                    master=AudioManager.Instance.mainVolume;sfx=AudioManager.Instance.sfxVolumeFactor;bgm=AudioManager.Instance.bgmVolumeFactor;saved=true;
                    Record(a);r.EventProbability=0;r.LoadParcel(r.Parcels[0]);
                    var edge=new StreetEdge(CourierStreetMap.Nodes[0],CourierStreetMap.Nodes[3],StreetSurface.Water,null);
                    r.Roads[0]=new Road(0,3,6,0){StreetPath=new[]{edge}};
                    Require(r.Depart(r.Roads[0]),"Cannot depart");r.FastForwardTravel();
                    Require(r.Results.Count==1&&r.Results[0].Damaged,"Damage/delivery fixture failed");break;
                case 1:
                    Require(a.PlayCount("cargo")==cargo+1&&a.PlayCount("departure")==departure+1&&a.PlayCount("damage")==damage+1&&a.PlayCount("delivered")==delivery+1,"Same-frame pickup/skip/damage/delivery lost a cue");
                    Record(a);r.Node=6;r.Possess(0);r.Possess(0);break;
                case 2:
                    Require(a.PlayCount("possession")==possession+2&&a.PlayCount("repair")==repair,"Same-type exchange played repair or lost swap");
                    Record(a);r.UseGhost();r.Node=5;r.LoadParcel(r.Parcels[8]);r.Parcels[8].Dry=r.Parcels[9].Dry=true;
                    var wet=new StreetEdge(CourierStreetMap.Nodes[5],CourierStreetMap.Nodes[3],StreetSurface.Water,null);
                    r.Roads[0]=new Road(5,3,6,0){StreetPath=new[]{wet}};r.Depart(r.Roads[0]);r.FastForwardTravel();break;
                case 3:
                    Require(a.PlayCount("damage")==damage+1&&a.PlayCount("delivered")==delivery+1,"Batch warning was duplicated or suppressed");
                    Record(a);r.Node=0;r.Parcels[0].State=ParcelState.Waiting;r.LoadParcel(r.Parcels[0]);r.EventProbability=1;
                    var c=CourierStreetMap.Node("C");r.Roads[0]=new Road(0,c.StopId,6,0){StreetPath=new[]{new StreetEdge(CourierStreetMap.Nodes[0],c,StreetSurface.Normal,null)}};
                    r.Depart(r.Roads[0]);r.FastForwardTravel();Require(r.State==ShiftState.Encounter,"No configured event");
                    // Confirm in this same frame: both the event and its resolution must still play.
                    r.ResolveEncounter(true);break;
                case 4:
                    Require(a.PlayCount("event_wait")==wait+1&&a.PlayCount("arrival")==arrival+1,"Skipped event resolution/arrival lost");
                    r.Node=6;r.Possess(0);r.StartJourney(0);break;
                case 5:
                    Require(Engine(owner).volume>.02f,"H engine silent");r.State=ShiftState.Docked;r.Node=7;r.Possess(1);r.StartJourney(0);break;
                case 6:
                    Require(Engine(owner).volume>.02f&&Mathf.Abs(Engine(owner).pitch-1.05f)<.05f,"W still treated as bicycle");r.State=ShiftState.Docked;r.Node=8;r.Possess(2);r.StartJourney(0);break;
                case 7:
                    Require(Engine(owner).volume>.02f&&Mathf.Abs(Engine(owner).pitch-.8f)<.05f,"Q engine wrong");r.State=ShiftState.Docked;r.UseGhost();r.StartJourney(0);break;
                case 8:
                    Require(Engine(owner).volume<.005f,"Ghost engine not silent");Record(a);
                    GameManager.Instance.PauseGame();r.AudioCues.Enqueue("event_wait");break;
                case 9:
                    Require(a.PlayCount("event_wait")==wait,"Paused cue played");GameManager.Instance.ResumeGame();break;
                case 10:
                    Require(a.PlayCount("event_wait")==wait+1,"Paused cue lost on resume");
                    Record(a);r.AudioCues.Enqueue("event_wait");owner.RestartShift();break;
                case 11:
                    Require(a.PlayCount("event_wait")==wait,"Restart replayed stale audio");
                    Require(a.PlayCount("cargo")==cargo&&a.PlayCount("delivered")==delivery,"Restart replayed cargo/delivery");
                    Finish(true,"24 cues + 3 loops decoded; 12 event mappings; fast-forward damage/delivery; batch deduplication; same-type exchange; H/W/Q and ghost engines; paused queue/resume; restart. Rules passed.");return;
            }
            step++;next=EditorApplication.timeSinceStartup+2.2;
        }
        catch(Exception e){Debug.LogException(e);Finish(false,e.ToString());}
    }
}
