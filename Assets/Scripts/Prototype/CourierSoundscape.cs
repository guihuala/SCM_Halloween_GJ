using System.Collections.Generic;
using UnityEngine;
namespace NightCourier
{
    // Licensed, imported loops and interaction cues. Attribution ships in AudioCredits.txt.
    public sealed class CourierSoundscape : MonoBehaviour
    {
        private NightCourierPrototype owner;
        private AudioSource music,wind,engine,cues;
        private ShiftState previousState;
        private int previousVehicle;
        private bool paused;
        private CourierRun previousRun;
        private int loaded, delivered, damaged;
        private float durability, nextHover;
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
            cues.PlayOneShot(effects[cue],gain);
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
            music.clip=authoredMusic!=null?authoredMusic:LoadLoop("night_waltz");
            wind.clip=LoadLoop("night_wind");engine.clip=LoadLoop("vehicle_engine");
            foreach(var source in new[]{music,wind,engine})
            {source.loop=true;if(source.clip!=null)source.Play();}
            Snapshot(owner.Run);
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
        private void Snapshot(CourierRun run)
        {
            previousRun=run;previousState=run.State;previousVehicle=run.VehicleIndex;durability=run.Durability;
            loaded=delivered=damaged=0;
            foreach(var p in run.Parcels)
            {
                if(p.State==ParcelState.Loaded)loaded++;
                if(p.State==ParcelState.Delivered)delivered++;
                if(p.Damaged)damaged++;
            }
        }
        // After simulation/UI updates so a single batch produces one cue, including drag-and-drop.
        private void LateUpdate()
        {
            if(owner==null||music==null)return;
            if(!owner.Active)
            {if(!paused){music.Pause();wind.Pause();engine.Pause();cues.Pause();paused=true;}return;}
            if(paused){music.UnPause();wind.UnPause();engine.UnPause();cues.UnPause();paused=false;}
            var run=owner.Run;
            if(run!=previousRun){cues.Stop();Snapshot(run);}
            bool driving=run.State==ShiftState.Driving;
            int nowLoaded=0,nowDelivered=0,nowDamaged=0;
            foreach(var p in run.Parcels)
            {
                if(p.State==ParcelState.Loaded)nowLoaded++;
                if(p.State==ParcelState.Delivered)nowDelivered++;
                if(p.Damaged)nowDamaged++;
            }
            if(run.Finished&&run.State!=previousState)PlayCue("ending");
            else if(nowDelivered>delivered)PlayCue("delivered",.8f);
            else if(run.State==ShiftState.Encounter&&previousState!=run.State)PlayCue("encounter",.65f);
            else if(run.State==ShiftState.Docked&&previousState!=run.State)PlayCue("arrival",.6f);
            else if(nowDamaged>damaged)PlayCue("damage",.65f);
            else if(run.VehicleIndex!=previousVehicle)PlayCue("possession",.55f);
            else if(nowLoaded>loaded)PlayCue("cargo",.8f);
            else if(driving&&previousState==ShiftState.Docked)PlayCue("departure",.7f);
            else if(run.VehicleIndex==previousVehicle&&run.Durability>durability)PlayCue("repair",.65f);
            bool storm=run.ActiveRoad!=null&&run.ActiveRoad.Kind==RoadKind.Storm;
            SetVolume(music,run.State==ShiftState.Docked&&owner.ConversationActive?.12f:run.Parcels[0].State==ParcelState.Delivered?.22f:.18f,true);
            SetVolume(wind,driving?(storm?.30f:.15f):.045f,false);
            // Vehicle 1 is a bicycle; it must never inherit the combustion-engine layer.
            SetVolume(engine,driving&&(run.VehicleIndex==0||run.VehicleIndex==2)?(run.VehicleIndex==2?.18f:.13f):0,false);
            engine.pitch=Mathf.MoveTowards(engine.pitch,run.VehicleIndex==0?1.35f:.8f,Time.unscaledDeltaTime*.5f);
            SetVolume(cues,.55f,false);
            Snapshot(run);
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
