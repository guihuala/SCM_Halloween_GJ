using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
namespace NightCourier
{
    // Procedural temp audio; an authored music clip can be assigned on NightCourierPrototype.
    public sealed class CourierSoundscape : MonoBehaviour
    {
        private NightCourierPrototype owner;
        private AudioSource music,wind,engine,cues;
        private AudioClip tuning,chime;
        private readonly List<AudioClip> generated=new List<AudioClip>();
        private ShiftState previousState;
        private int previousVehicle;
        private bool paused;
        public void Initialize(NightCourierPrototype prototype,AudioClip authoredMusic)
        {
            owner=prototype;
            music=Source("BGM",.055f);wind=Source("Sfx",.045f);engine=Source("Sfx",.025f);cues=Source("Sfx",.13f);
            music.clip=authoredMusic!=null?authoredMusic:Generate("Temporary radio loop",8,t=>{
                float[] notes={110,130.8128f,146.8324f,98};float f=notes[Math.Min(3,(int)(t/2))];float phase=t%2;
                return (float)((Math.Sin(2*Math.PI*f*t)+.35*Math.Sin(2*Math.PI*f*1.5*t)+.20*Math.Sin(2*Math.PI*f*2*t))*Math.Exp(-phase*1.8)*.22);
            });
            var random=new System.Random(321);float filter=0;
            wind.clip=Generate("Temporary wind loop",4,t=>{filter=filter*.94f+((float)random.NextDouble()*2-1)*.06f;return filter;});
            engine.clip=Generate("Temporary engine loop",2,t=>(float)(Math.Sin(t*Math.PI*2*48)*.4+Math.Sin(t*Math.PI*2*96)*.15));
            tuning=Generate("Radio tuning",.5f,t=>((float)random.NextDouble()*2-1)*(1-t/.5f)*.35f);
            chime=Generate("Possession chime",.7f,t=>(float)((Math.Sin(2*Math.PI*660*t)+Math.Sin(2*Math.PI*990*t))*.2*Math.Exp(-t*6)));
            music.loop=wind.loop=engine.loop=true;music.Play();wind.Play();engine.Play();
            previousState=owner.Run.State;previousVehicle=owner.Run.VehicleIndex;
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
        private void Update()
        {
            if(owner==null||music==null)return;
            if(!owner.Active)
            {if(!paused){music.Pause();wind.Pause();engine.Pause();cues.Pause();paused=true;}return;}
            if(paused){music.UnPause();wind.UnPause();engine.UnPause();cues.UnPause();paused=false;}
            var run=owner.Run;bool driving=run.State==ShiftState.Driving;
            if(driving&&previousState==ShiftState.Docked)cues.PlayOneShot(tuning);
            if(run.VehicleIndex!=previousVehicle)cues.PlayOneShot(chime);
            float master=AudioManager.Instance!=null?AudioManager.Instance.mainVolume:1;
            bool storm=run.ActiveRoad!=null&&run.ActiveRoad.Kind==RoadKind.Storm;
            music.volume=(run.TruthRevealed?.075f:.045f)*master;
            wind.volume=(driving?(storm?.16f:.07f):.025f)*master;
            engine.volume=(driving&&run.VehicleIndex==2?.05f:.005f)*master;
            previousState=run.State;previousVehicle=run.VehicleIndex;
        }
        private void OnDestroy(){foreach(var clip in generated)if(clip!=null)Destroy(clip);}
    }
}
