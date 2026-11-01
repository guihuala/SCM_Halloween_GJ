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
        public bool reducedMotion;
        private CourierRoundedRing roundedRing;
        private bool focused,pressed,blocked;
        private float pressAmount;
        private void Awake()
        {
            focusColor=CourierColors.Mint;blockedColor=CourierColors.Gold;
            // Upgrade existing four-edge prefabs without changing their references or hit areas.
            if(edges!=null)foreach(var edge in edges)if(edge!=null)edge.enabled=false;
            var outline=new GameObject("RoundedOutline",typeof(RectTransform),typeof(CanvasRenderer),typeof(CourierRoundedRing));
            outline.layer=gameObject.layer;outline.transform.SetParent(transform,false);
            roundedRing=outline.GetComponent<CourierRoundedRing>();roundedRing.raycastTarget=false;
            var rect=roundedRing.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
            rect.offsetMin=rect.offsetMax=Vector2.zero;
        }
        public void SetState(bool focus,bool down,bool unavailable)
        {
            focused=focus;pressed=down;blocked=unavailable;
        }
        private void Update()
        {
            pressAmount=Mathf.MoveTowards(pressAmount,pressed?1:0,Time.unscaledDeltaTime/.08f);
            float target=focused||pressed?1:0;
            group.alpha=reducedMotion||fadeDuration<=0?target:Mathf.MoveTowards(group.alpha,target,Time.unscaledDeltaTime/fadeDuration);
            float emphasis=pressAmount*.55f;
            transform.localScale=Vector3.one;
            Color color=blocked?blockedColor:focusColor;
            if(roundedRing!=null)roundedRing.color=Color.Lerp(color,CourierColors.Paper,emphasis*.55f);
        }
        public void ResetEffect(){focused=pressed=blocked=false;pressAmount=0;if(group!=null)group.alpha=0;transform.localScale=Vector3.one;}
        private void OnDisable(){ResetEffect();}
    }
}
