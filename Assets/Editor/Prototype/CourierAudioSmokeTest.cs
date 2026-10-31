using System;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CourierAudioSmokeTest
{
    private const string Key="CourierAudio.Test";
    private static int step,cargoBefore,deliveredBefore,departureBefore,encounterBefore,clickBefore;
    private static double next,deadline;
    private static float master,sfx,bgm;
    private static bool saved;
    static CourierAudioSmokeTest(){EditorApplication.update+=Tick;}
    private static void Require(bool ok,string error){if(!ok)throw new Exception(error);}
    [MenuItem("Tools/Night Courier/Checks/Audio (exits editor)")]
    public static void Run()
    {
        Debug.Log(CourierChecks.RunAll());
        foreach(var cue in CourierSoundscape.CueNames)
        {
            var clip=Resources.Load<AudioClip>("Audio/Courier/"+cue);
            Require(clip!=null&&clip.length>0&&clip.length<4,"Invalid sound: "+cue);
            Require(clip.LoadAudioData(),"Cannot decode sound: "+cue);
        }
        EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    private static void Restore()
    {
        if(saved&&AudioManager.Instance!=null)
        {
            AudioManager.Instance.ChangeMainVolume(master);
            AudioManager.Instance.ChangeSfxVolume(sfx);
            AudioManager.Instance.ChangeBgmVolume(bgm);
        }
    }
    private static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        if(deadline==0){deadline=EditorApplication.timeSinceStartup+80;next=EditorApplication.timeSinceStartup+3;}
        try
        {
            Require(EditorApplication.timeSinceStartup<deadline,"Audio test timed out at "+step);
            if(EditorApplication.timeSinceStartup<next)return;
            var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();
            Require(owner!=null,"Gameplay missing");
            var audio=owner.GetComponent<CourierSoundscape>();
            Require(audio!=null,"Soundscape missing");
            var run=owner.Run;
            switch(step)
            {
                case 0:
                    owner.enabled=false;owner.Tutorial.Dismiss();
                    foreach(var cue in CourierSoundscape.CueNames)Require(audio.HasCue(cue),"Runtime cue missing: "+cue);
                    var buttonObject=new GameObject("AudioTestButton",typeof(RectTransform),typeof(Button),typeof(CourierButtonFeedback));
                    var button=buttonObject.GetComponent<Button>();
                    clickBefore=audio.PlayCount("click");
                    button.OnSubmit(new BaseEventData(EventSystem.current));
                    buttonObject.GetComponent<CourierButtonFeedback>().OnSubmit(new BaseEventData(EventSystem.current));
                    Require(audio.PlayCount("click")==clickBefore+1,"Keyboard submit played twice");
                    button.interactable=false;button.onClick.Invoke();
                    Require(audio.PlayCount("click")==clickBefore+1,"Disabled button played");
                    int hover=audio.PlayCount("hover");
                    CourierSoundscape.PlayInterface("hover");CourierSoundscape.PlayInterface("hover");
                    Require(audio.PlayCount("hover")==hover+1,"Hover throttling failed");
                    UnityEngine.Object.Destroy(buttonObject);
                    cargoBefore=audio.PlayCount("cargo");Require(run.LoadParcel(run.Parcels[0]),"Acceptance failed");break;
                case 1:
                    Require(audio.PlayCount("cargo")==cargoBefore+1,"Cargo acceptance cue missing");
                    Require(!run.LoadParcel(run.Parcels[0]),"Duplicate acceptance succeeded");
                    departureBefore=audio.PlayCount("departure");Require(run.StartJourney(3),"Departure failed");break;
                case 2:
                    Require(audio.PlayCount("departure")==departureBefore+1,"Departure cue missing");
                    encounterBefore=audio.PlayCount("encounter");run.State=ShiftState.Encounter;break;
                case 3:
                    Require(audio.PlayCount("encounter")==encounterBefore+1,"Encounter cue missing");
                    deliveredBefore=audio.PlayCount("delivered");
                    for(int i=0;i<100&&run.State!=ShiftState.Docked&&!run.Finished;i++)
                        if(run.State==ShiftState.Encounter)run.ResolveEncounter(true);else run.FastForwardTravel();
                    CourierChecks.Go(run,3);
                    Require(run.State==ShiftState.Docked,"Arrival failed");break;
                case 4:
                    Require(audio.PlayCount("delivered")==deliveredBefore+1,"Delivery cue missing");
                    run.UseGhost();CourierChecks.Go(run,5);break;
                case 5:
                    cargoBefore=audio.PlayCount("cargo");deliveredBefore=audio.PlayCount("delivered");
                    Require(run.LoadParcel(run.Parcels[9]),"Batch acceptance failed");break;
                case 6:
                    Require(audio.PlayCount("cargo")==cargoBefore+1,"Batch should play once");
                    CourierChecks.Go(run,3);break;
                case 7:
                    Require(audio.PlayCount("delivered")==deliveredBefore+1,"Batch delivery should play once");
                    clickBefore=audio.PlayCount("click");GameManager.Instance.PauseGame();
                    CourierSoundscape.PlayInterface("click");
                    Require(audio.PlayCount("click")==clickBefore,"Paused game played interaction cue");break;
                case 8:
                    GameManager.Instance.ResumeGame();
                    var manager=AudioManager.Instance;Require(manager!=null&&manager.audioMixer!=null,"Mixer missing");
                    master=manager.mainVolume;sfx=manager.sfxVolumeFactor;bgm=manager.bgmVolumeFactor;saved=true;
                    manager.ChangeMainVolume(0);
                    Require(manager.audioMixer.GetFloat("Sfx",out var muted)&&muted<=-79,"Master mute missed courier sources");
                    manager.ChangeMainVolume(.5f);manager.ChangeSfxVolume(.5f);
                    Require(manager.audioMixer.GetFloat("Sfx",out var gain)&&Mathf.Abs(gain-20*Mathf.Log10(.25f))<.01f,"Master/SFX gain wrong");
                    Restore();saved=false;cargoBefore=audio.PlayCount("cargo");
                    owner.RestartShift();break;
                case 9:
                    Require(audio.PlayCount("cargo")==cargoBefore,"Restart replayed old cargo cue");
                    SessionState.SetBool(Key,false);
                    Debug.Log("COURIER_AUDIO PASS: 12 decoded clips, keyboard/mouse/disabled feedback, hover throttle, acceptance, departure, encounter, delivery, single cue per batch, pause, master/SFX mute and restart.");
                    EditorApplication.Exit(0);return;
            }
            Debug.Log("COURIER_AUDIO step "+step+" passed");step++;next=EditorApplication.timeSinceStartup+.25;
        }
        catch(Exception error){Restore();SessionState.SetBool(Key,false);Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
