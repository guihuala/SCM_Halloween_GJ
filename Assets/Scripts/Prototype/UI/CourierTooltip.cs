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
        private Vector2 homePosition;
        private Vector2 measuredSize;
        private float measuredPixelsPerUnit;
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
            var parent=(RectTransform)bubble.parent;
            if(shownMessage==source.message&&measuredSize==parent.rect.size&&Mathf.Approximately(measuredPixelsPerUnit,label.pixelsPerUnit))return;
            measuredSize=parent.rect.size;measuredPixelsPerUnit=label.pixelsPerUnit;
            shownMessage=source.message;label.text=view!=null?view.FormatMessage(shownMessage):shownMessage;
            float padding=view!=null?view.padding:16;
            float width=Mathf.Min(view!=null?view.maxWidth:440,Mathf.Max(80,parent.rect.width-24));
            label.rectTransform.anchorMin=label.rectTransform.anchorMax=new Vector2(0,1);
            label.rectTransform.sizeDelta=new Vector2(width-padding*2,0);
            width=Mathf.Min(width,Mathf.Max(140,label.preferredWidth+padding*2));
            label.rectTransform.sizeDelta=new Vector2(width-padding*2,0);
            // Long rule and cargo descriptions must remain readable without clipping or shrinking.
            float maximumWidth=Mathf.Min(900,Mathf.Max(80,parent.rect.width-48));
            while(label.preferredHeight+padding*2>parent.rect.height-64&&width<maximumWidth)
            {
                width=Mathf.Min(maximumWidth,width+160);
                label.rectTransform.sizeDelta=new Vector2(width-padding*2,0);
            }
            float height=Mathf.Min(parent.rect.height-24,Mathf.Max(52,Mathf.Ceil(label.preferredHeight)+padding*2+12));
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
            // Keep adjacent values readable: place outside the entire control row.
            float x=(lower.x+upper.x-width)*.5f;
            float y=upper.y+height+20;
            if(y>parent.rect.yMax-12)y=lower.y-20;
            bubble.anchorMin=bubble.anchorMax=parent.pivot;bubble.pivot=new Vector2(0,1);
            bubble.anchoredPosition=new Vector2(Mathf.Clamp(x,parent.rect.xMin+12,parent.rect.xMax-width-12),Mathf.Clamp(y,parent.rect.yMin+height+12,parent.rect.yMax-12));
            homePosition=bubble.anchoredPosition;
        }
        private void LateUpdate()
        {
            if(bubble==null||!bubble.gameObject.activeSelf)return;
            if(source!=null&&(!source.isActiveAndEnabled||string.IsNullOrWhiteSpace(source.message)))Hide(source);
            if(source!=null){RefreshContent();Position();bubble.SetAsLastSibling();}
            if(view!=null)
            {
                float target=source!=null&&Time.unscaledTime>=showAt?1:0;
                view.group.alpha=view.fadeDuration<=0?target:Mathf.MoveTowards(view.group.alpha,target,Time.unscaledDeltaTime/view.fadeDuration);
                float eased=1-Mathf.Pow(1-view.group.alpha,3);
                bubble.localScale=Vector3.one*Mathf.Lerp(.98f,1,eased);
                bubble.anchoredPosition=homePosition+new Vector2(0,-8*(1-eased));
                if(source==null&&view.group.alpha<=0)bubble.gameObject.SetActive(false);
            }
        }
        public void Hide(CourierIconHint hint){if(source!=hint)return;source=null;if(bubble!=null&&view==null)bubble.gameObject.SetActive(false);}
        private void OnDisable(){if(bubble!=null){bubble.gameObject.SetActive(false);bubble.localScale=Vector3.one;}source=null;}
    }
}
