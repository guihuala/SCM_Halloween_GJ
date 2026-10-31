using UnityEngine;
using UnityEngine.UI;
namespace NightCourier
{
    // This prefab animates its own border, leaving the button's layout and hit area fixed.
    public sealed class CourierInteractionEffect : MonoBehaviour
    {
        public CanvasGroup group;
        public Image[] edges;
        public Color focusColor=new Color(.45f,.94f,.77f),blockedColor=new Color(1,.66f,.34f);
        [Min(0)] public float fadeDuration=.12f;
        [Min(0)] public float pulseDuration=.22f;
        public bool reducedMotion;
        private bool focused,pressed,blocked;
        private float pulse;
        public void SetState(bool focus,bool down,bool unavailable)
        {focused=focus;pressed=down;blocked=unavailable;}
        public void Play(){pulse=pulseDuration;}
        private void Update()
        {
            pulse=Mathf.Max(0,pulse-Time.unscaledDeltaTime);
            float target=focused||pressed||pulse>0?1:0;
            group.alpha=reducedMotion||fadeDuration<=0?target:Mathf.MoveTowards(group.alpha,target,Time.unscaledDeltaTime/fadeDuration);
            float emphasis=reducedMotion?0:(pressed?.55f:pulseDuration>0?pulse/pulseDuration:0);
            transform.localScale=Vector3.one*(1+emphasis*.045f);
            Color color=blocked?blockedColor:focusColor;
            foreach(var edge in edges)edge.color=Color.Lerp(color,Color.white,emphasis*.55f);
        }
        public void ResetEffect(){focused=pressed=false;pulse=0;if(group!=null)group.alpha=0;transform.localScale=Vector3.one;}
        private void OnDisable(){ResetEffect();}
    }
}
