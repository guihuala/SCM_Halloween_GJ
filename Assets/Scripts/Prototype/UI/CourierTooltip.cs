using UnityEngine;
using UnityEngine.UI;
namespace NightCourier
{
    public sealed class CourierTooltip : MonoBehaviour
    {
        public CourierHintView viewPrefab;
        public RectTransform bubble;
        public Text label;
        private CourierHintView view;
        private CourierIconHint source;
        private float showAt;
        private string shownMessage;
        private readonly Vector3[] corners=new Vector3[4];
        private void Awake()
        {
            if(viewPrefab==null)viewPrefab=Resources.Load<CourierHintView>("Feedback/CourierHint");
            if(viewPrefab!=null)
            {
                if(bubble!=null)Destroy(bubble.gameObject);
                view=Instantiate(viewPrefab,transform,false);bubble=(RectTransform)view.transform;label=view.label;
            }
            if(bubble!=null)bubble.gameObject.SetActive(false);
        }
        public void Show(CourierIconHint hint,bool immediate=false)
        {
            if(hint==null||string.IsNullOrWhiteSpace(hint.message)||bubble==null)return;
            if(source==hint){if(immediate)showAt=Time.unscaledTime;return;}
            source=hint;shownMessage=null;showAt=Time.unscaledTime+(immediate||view==null?0:view.hoverDelay);
            if(view!=null)view.group.alpha=0;
            bubble.gameObject.SetActive(true);bubble.SetAsLastSibling();RefreshContent();Position();
        }
        private void RefreshContent()
        {
            if(shownMessage==source.message)return;
            shownMessage=source.message;label.text=shownMessage;
            var parent=(RectTransform)bubble.parent;float padding=view!=null?view.padding:16;
            float width=Mathf.Min(view!=null?view.maxWidth:440,Mathf.Max(80,parent.rect.width-24));
            label.rectTransform.anchorMin=label.rectTransform.anchorMax=new Vector2(0,1);
            label.rectTransform.sizeDelta=new Vector2(width-padding*2,0);
            width=Mathf.Min(width,Mathf.Max(140,label.preferredWidth+padding*2));
            label.rectTransform.sizeDelta=new Vector2(width-padding*2,0);
            float height=Mathf.Min(parent.rect.height-24,Mathf.Max(52,label.preferredHeight+padding*2));
            bubble.sizeDelta=new Vector2(width,height);
            label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;
            label.rectTransform.offsetMin=new Vector2(padding,padding);label.rectTransform.offsetMax=new Vector2(-padding,-padding);
        }
        private void Position()
        {
            var parent=(RectTransform)bubble.parent;var target=source.transform as RectTransform;if(target==null)return;
            target.GetWorldCorners(corners);
            Vector3 lower=parent.InverseTransformPoint(corners[0]),upper=parent.InverseTransformPoint(corners[2]);
            float width=bubble.sizeDelta.x,height=bubble.sizeDelta.y;
            // Prefer beside the control; flip sides at the edge and stay in the panel.
            float x=upper.x+12;if(x+width>parent.rect.xMax-12)x=lower.x-width-12;
            float y=upper.y;
            bubble.anchorMin=bubble.anchorMax=parent.pivot;bubble.pivot=new Vector2(0,1);
            bubble.anchoredPosition=new Vector2(Mathf.Clamp(x,parent.rect.xMin+12,parent.rect.xMax-width-12),Mathf.Clamp(y,parent.rect.yMin+height+12,parent.rect.yMax-12));
        }
        private void LateUpdate()
        {
            if(source==null)return;
            if(!source.isActiveAndEnabled||string.IsNullOrWhiteSpace(source.message)){Hide(source);return;}
            RefreshContent();Position();bubble.SetAsLastSibling();
            if(view!=null&&Time.unscaledTime>=showAt)view.group.alpha=view.fadeDuration<=0?1:Mathf.MoveTowards(view.group.alpha,1,Time.unscaledDeltaTime/view.fadeDuration);
        }
        public void Hide(CourierIconHint hint){if(source!=hint)return;source=null;if(bubble!=null)bubble.gameObject.SetActive(false);}
        private void OnDisable(){if(bubble!=null)bubble.gameObject.SetActive(false);source=null;}
    }
}
