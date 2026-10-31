using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
namespace NightCourier
{
    // Authored CC0 interaction cues, with procedural ambience until music/loops are supplied.
    public sealed class CourierSoundscape : MonoBehaviour
    {
        private NightCourierPrototype owner;
        private AudioSource music,wind,engine,cues;
        private readonly List<AudioClip> generated=new List<AudioClip>();
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
            music=Source("BGM",.055f);wind=Source("Sfx",.045f);engine=Source("Sfx",.025f);cues=Source("Sfx",.55f);
            foreach(var cue in CueNames)
            {
                effects[cue]=Resources.Load<AudioClip>("Audio/Courier/"+cue);
                if(effects[cue]==null)Debug.LogWarning("Missing courier sound: "+cue);
            }
            music.clip=authoredMusic!=null?authoredMusic:Generate("Temporary radio loop",8,t=>{
                float[] notes={110,130.8128f,146.8324f,98};float f=notes[Math.Min(3,(int)(t/2))];float phase=t%2;
                return (float)((Math.Sin(2*Math.PI*f*t)+.35*Math.Sin(2*Math.PI*f*1.5*t)+.20*Math.Sin(2*Math.PI*f*2*t))*Math.Exp(-phase*1.8)*.22);
            });
            var random=new System.Random(321);float filter=0;
            wind.clip=Generate("Temporary wind loop",4,t=>{filter=filter*.94f+((float)random.NextDouble()*2-1)*.06f;return filter;});
            engine.clip=Generate("Temporary engine loop",2,t=>(float)(Math.Sin(t*Math.PI*2*48)*.4+Math.Sin(t*Math.PI*2*96)*.15));
            music.loop=wind.loop=engine.loop=true;music.Play();wind.Play();engine.Play();
            Snapshot(owner.Run);
        }
        private AudioSource Source(string group,float volume)
        {
            var source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.volume=volume;
            if(AudioManager.Instance!=null&&AudioManager.Instance.audioMixer!=null)
            {var groups=AudioManager.Instance.audioMixer.FindMatchingGroups(group);if(groups.Length>0)source.outputAudioMixerGroup=groups[0];}
            return source;
        }
        private AudioClip Generate(string name,float seconds,Func<float,float> sample)
        {
            const int rate=22050;int length=(int)(seconds*rate);var data=new float[length];
            for(int i=0;i<length;i++)data[i]=Mathf.Clamp(sample(i/(float)rate),-1,1);
            var clip=AudioClip.Create(name,length,1,rate,false);clip.SetData(data,0);generated.Add(clip);return clip;
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
            SetVolume(music,run.Parcels[0].State==ParcelState.Delivered?.075f:.045f,true);
            SetVolume(wind,driving?(storm?.16f:.07f):.025f,false);
            SetVolume(engine,driving&&!run.IsGhost?(run.VehicleIndex==2?.05f:.02f):0,false);
            SetVolume(cues,.55f,false);
            Snapshot(run);
        }
        private static void SetVolume(AudioSource source,float gain,bool bgm)
        {
            // The mixer already applies master/category gain. Fall back to settings if no mixer exists.
            var manager=AudioManager.Instance;
            source.volume=gain*(source.outputAudioMixerGroup!=null?1:manager!=null?
                manager.mainVolume*(bgm?manager.bgmVolumeFactor:manager.sfxVolumeFactor):1);
        }
        private void OnDestroy(){foreach(var clip in generated)if(clip!=null)Destroy(clip);}
    }
}
