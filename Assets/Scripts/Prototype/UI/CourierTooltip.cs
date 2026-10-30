using UnityEngine;
using UnityEngine.UI;
namespace NightCourier
{
    public sealed class CourierTooltip : MonoBehaviour
    {
        public RectTransform bubble;
        public Text label;
        private CourierIconHint source;
        public void Show(CourierIconHint hint)
        {
            if(string.IsNullOrEmpty(hint.message))return;
            source=hint;label.text=hint.message;float width=Mathf.Clamp(label.preferredWidth+28,140,430);
            label.rectTransform.sizeDelta=new Vector2(width-28,100);float height=Mathf.Clamp(label.preferredHeight+24,52,140);
            bubble.sizeDelta=new Vector2(width,height);label.rectTransform.sizeDelta=new Vector2(width-28,height-24);
            var parent=(RectTransform)bubble.parent;var position=parent.InverseTransformPoint(hint.transform.position);
            bubble.anchoredPosition=new Vector2(Mathf.Clamp(position.x+20,parent.rect.xMin+12,parent.rect.xMax-width-12),Mathf.Clamp(position.y-30,parent.rect.yMin+height+12,parent.rect.yMax-12));
            bubble.SetAsLastSibling();bubble.gameObject.SetActive(true);
        }
        public void Hide(CourierIconHint hint){if(source!=hint)return;source=null;bubble.gameObject.SetActive(false);}
        private void OnDisable(){if(bubble!=null)bubble.gameObject.SetActive(false);source=null;}
    }
}
