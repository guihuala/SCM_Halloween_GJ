using System;
using System.Collections.Generic;
using UnityEngine;
namespace NightCourier
{
    [Serializable] public sealed class CourierAudioEntry { public string cue,path; public float gain=1; }
    [Serializable] public sealed class CourierAudioConfig { public CourierAudioEntry[] cues; }
    // Licensed, imported loops and interaction cues. Attribution ships in AudioCredits.txt.
    public sealed class CourierSoundscape : MonoBehaviour
    {
        private NightCourierPrototype owner;
        private AudioSource music,wind,engine,cues;
        private bool paused;
        private CourierRun previousRun;
        private float nextHover,nextCue,pauseStarted;
        private readonly Queue<string> pending=new Queue<string>();
        private readonly Dictionary<string,float> gains=new Dictionary<string,float>();
        private readonly Dictionary<string,AudioClip> effects=new Dictionary<string,AudioClip>();
        private readonly Dictionary<string,int> playCounts=new Dictionary<string,int>();
        public static readonly string[] CueNames={"click","hover","dialogue","cargo","departure","arrival","delivered","encounter","damage","possession","ending","repair"};
        public int PlayCount(string cue){return playCounts.TryGetValue(cue,out var count)?count:0;}
        public bool HasCue(string cue){return effects.TryGetValue(cue,out var clip)&&clip!=null;}
        public static void PlayInterface(string cue)
        {
            var owner=NightCourierPrototype.Current;
            if(owner==null||!owner.Active)return;
            var audio=owner.GetComponent<CourierSoundscape>();
            if(audio==null)return;
            if(cue=="hover")
            {
                if(Time.unscaledTime<audio.nextHover)return;
                audio.nextHover=Time.unscaledTime+.08f;
            }
            audio.PlayCue(cue,cue=="hover"?.12f:.35f);
        }
        private void PlayCue(string cue,float gain=1)
        {
            if(!owner.Active||!HasCue(cue))return;
            cues.PlayOneShot(effects[cue],gain*(gains.TryGetValue(cue,out var configuredGain)?configuredGain:1));
            playCounts[cue]=PlayCount(cue)+1;
        }
        public void Initialize(NightCourierPrototype prototype,AudioClip authoredMusic)
        {
            owner=prototype;
            music=Source("BGM",0);wind=Source("Sfx",0);engine=Source("Sfx",0);cues=Source("Sfx",.55f);
            foreach(var cue in CueNames)
            {
                effects[cue]=Resources.Load<AudioClip>("Audio/Courier/"+cue);
                if(effects[cue]==null)Debug.LogWarning("Missing courier sound: "+cue);
            }
            var config=Resources.Load<TextAsset>("NightVoyage/AudioCues");
            if(config!=null)
            {
                var data=JsonUtility.FromJson<CourierAudioConfig>(config.text);
                if(data!=null&&data.cues!=null)foreach(var entry in data.cues)
                {
                    if(string.IsNullOrEmpty(entry.cue)||string.IsNullOrEmpty(entry.path))continue;
                    var clip=Resources.Load<AudioClip>(entry.path);
                    if(clip==null){Debug.LogWarning("Missing configured courier sound: "+entry.path);continue;}
                    effects[entry.cue]=clip;gains[entry.cue]=Mathf.Clamp01(entry.gain);
                }
            }
            music.clip=authoredMusic!=null?authoredMusic:LoadLoop("night_waltz");
            wind.clip=LoadLoop("night_wind");engine.clip=LoadLoop("vehicle_engine");
            foreach(var source in new[]{music,wind,engine})
            {source.loop=true;if(source.clip!=null)source.Play();}
            previousRun=owner.Run;owner.Run.AudioCues.Clear();
        }
        private AudioSource Source(string group,float volume)
        {
            var source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.volume=volume;
            if(AudioManager.Instance!=null&&AudioManager.Instance.audioMixer!=null)
            {var groups=AudioManager.Instance.audioMixer.FindMatchingGroups(group);if(groups.Length>0)source.outputAudioMixerGroup=groups[0];}
            return source;
        }
        private static AudioClip LoadLoop(string name)
        {
            var clip=Resources.Load<AudioClip>("Audio/Courier/Loops/"+name);
            if(clip==null)Debug.LogWarning("Missing courier ambience: "+name);
            return clip;
        }
        // Deterministic model signals are drained once, including an entire skipped turn.
        private void LateUpdate()
        {
            if(owner==null||music==null)return;
            if(!owner.Active)
            {if(!paused){music.Pause();wind.Pause();engine.Pause();cues.Pause();paused=true;pauseStarted=Time.unscaledTime;}return;}
            if(paused){music.UnPause();wind.UnPause();engine.UnPause();cues.UnPause();paused=false;nextCue+=Time.unscaledTime-pauseStarted;}
            var run=owner.Run;
            if(run!=previousRun){cues.Stop();pending.Clear();previousRun=run;nextCue=0;}
            var thisFrame=new HashSet<string>();
            while(run.AudioCues.Count>0)
            {
                var cue=run.AudioCues.Dequeue();
                // One damaged batch gets one warning, even if several items were affected.
                if(cue=="damage"&&!thisFrame.Add(cue))continue;
                pending.Enqueue(cue);
            }
            if(pending.Count>0&&Time.unscaledTime>=nextCue)
            {
                var cue=pending.Dequeue();
                if(!HasCue(cue))cue="encounter";
                if(!HasCue(cue))return;
                PlayCue(cue,.75f);
                nextCue=Time.unscaledTime+Mathf.Clamp(effects[cue].length,.08f,.35f);
            }
            bool driving=run.State==ShiftState.Driving;
            bool storm=run.ActiveRoad!=null&&run.ActiveRoad.Kind==RoadKind.Storm;
            SetVolume(music,run.State==ShiftState.Docked&&owner.ConversationActive?.12f:run.Parcels[0].State==ParcelState.Delivered?.22f:.18f,true);
            SetVolume(wind,driving?(storm?.30f:.15f):.045f,false);
            // H, W and Q are all powered vehicles; the ghost has no engine.
            SetVolume(engine,driving&&!run.IsGhost?(run.VehicleIndex==1?.11f:run.VehicleIndex==2?.18f:.13f):0,false);
            engine.pitch=Mathf.MoveTowards(engine.pitch,run.VehicleIndex==0?1.35f:run.VehicleIndex==1?1.05f:.8f,Time.unscaledDeltaTime*.5f);
            SetVolume(cues,.55f,false);
        }
        private static void SetVolume(AudioSource source,float gain,bool bgm)
        {
            // The mixer already applies master/category gain. Fall back to settings if no mixer exists.
            var manager=AudioManager.Instance;
            float target=gain*(source.outputAudioMixerGroup!=null?1:manager!=null?
                manager.mainVolume*(bgm?manager.bgmVolumeFactor:manager.sfxVolumeFactor):1);
            source.volume=Mathf.MoveTowards(source.volume,target,Time.unscaledDeltaTime*.35f);
        }
    }
}
