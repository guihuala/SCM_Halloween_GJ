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
    private static AudioSource musicLoop,windLoop,engineLoop;
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
        foreach(var name in new[]{"night_waltz","night_wind","vehicle_engine"})
        {
            var clip=Resources.Load<AudioClip>("Audio/Courier/Loops/"+name);
            Require(clip!=null&&clip.length>3&&clip.LoadAudioData(),"Invalid ambience: "+name);
            Debug.Log("COURIER_LOOP "+name+": "+clip.length+" seconds, "+clip.channels+" channels");
        }
        Require(Resources.Load<TextAsset>("Audio/Courier/AudioCredits")!=null,"Missing shipped attribution");
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
                    foreach(var source in owner.GetComponents<AudioSource>())
                    {
                        if(source.clip==null)continue;
                        if(source.clip.name=="night_waltz")musicLoop=source;
                        if(source.clip.name=="night_wind")windLoop=source;
                        if(source.clip.name=="vehicle_engine")engineLoop=source;
                    }
                    Require(musicLoop!=null&&windLoop!=null&&engineLoop!=null,"Imported loops not bound");
                    Require(musicLoop.loop&&windLoop.loop&&engineLoop.loop,"Loop mode missing");
                    Require(musicLoop.outputAudioMixerGroup.name=="BGM"&&windLoop.outputAudioMixerGroup.name=="Sfx"&&engineLoop.outputAudioMixerGroup.name=="Sfx","Ambience mixer routing wrong");
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
                    run.VehicleIndex=1;run.ActiveRoad=run.Roads[0];run.State=ShiftState.Driving;break;
                case 10:
                    Require(engineLoop.volume==0,"Bicycle has an engine sound");
                    run.VehicleIndex=0;break;
                case 11:
                    Require(engineLoop.volume>.1f&&engineLoop.pitch>1,"Scooter engine did not fade in");
                    run.VehicleIndex=2;break;
                case 12:
                    Require(engineLoop.volume>.15f&&engineLoop.pitch<1.1f,"Pickup engine did not change tone");
                    run.VehicleIndex=-1;break;
                case 13:
                    Require(engineLoop.volume==0,"Ghost has an engine sound");
                    run.ActiveRoad=Array.Find(run.Roads,r=>r.Kind==RoadKind.Storm);break;
                case 14:
                    Require(windLoop.volume>.25f,"Storm wind did not strengthen");
                    var settings=UIManager.Instance.OpenPanel("SettingPanel",null,UIPanelLayer.Top);
                    Require(settings!=null,"Settings missing");break;
                case 15:
                    var panel=UIManager.Instance.GetPanel("SettingPanel");
                    var credit=panel.transform.Find("AudioCredits").GetComponent<Text>();
                    Require(credit.text.Contains("Wolfgang_")&&credit.text.Contains("qubodup")&&!credit.raycastTarget,"Credits missing or blocking input");
                    Canvas.ForceUpdateCanvases();
                    Debug.Log("COURIER_CREDITS size="+credit.rectTransform.rect.size+" preferredHeight="+credit.preferredHeight);
                    Require(credit.preferredHeight<=credit.rectTransform.rect.height,"Audio credits clipped vertically");
                    ScreenCapture.CaptureScreenshot("/tmp/courier-audio-credits.png");break;
                case 16:
                    SessionState.SetBool(Key,false);
                    Debug.Log("COURIER_AUDIO PASS: 12 cues, 3 imported loops, mixer routing, acceptance/delivery, batch deduplication, pause/master mute/restart, bicycle/ghost silent engine, scooter/pickup tone, storm wind and readable attribution.");
                    EditorApplication.Exit(0);return;
            }
            Debug.Log("COURIER_AUDIO step "+step+" passed");step++;next=EditorApplication.timeSinceStartup+.75;
        }
        catch(Exception error){Restore();SessionState.SetBool(Key,false);Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
