#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace NightCourier
{
    // Editor-only test driver. Never included in a player build.
    [DefaultExecutionOrder(20000)]
    public sealed class CourierFullFlowRecorder : MonoBehaviour
    {
        public Action CaptureFrame;
        private FileStream audio;
        private readonly object audioLock=new object();
        public int Channels {get;private set;}=2;
        public float Peak {get;private set;}
        public long AudioBytes {get;private set;}
        public void BeginAudio(string path)
        {lock(audioLock){audio=new FileStream(path,FileMode.Create,FileAccess.Write,FileShare.Read);}}
        public void EndAudio(){lock(audioLock){if(audio!=null){audio.Dispose();audio=null;}}}
        private void LateUpdate(){CaptureFrame?.Invoke();}
        private void OnAudioFilterRead(float[] data,int channels)
        {
            lock(audioLock)
            {
                if(audio==null)return;
                Channels=channels;var bytes=new byte[data.Length*2];
                for(int i=0;i<data.Length;i++)
                {
                    float value=Mathf.Clamp(data[i],-1,1);Peak=Mathf.Max(Peak,Mathf.Abs(value));
                    short sample=(short)(value*32767);bytes[i*2]=(byte)sample;bytes[i*2+1]=(byte)(sample>>8);
                }
                audio.Write(bytes,0,bytes.Length);AudioBytes+=bytes.Length;
            }
        }
        private void OnDestroy(){EndAudio();}
    }
}
#endif
