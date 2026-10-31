using UnityEngine;
using UnityEngine.EventSystems;
namespace NightCourier
{
    public sealed class CourierIconHint : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler,IPointerClickHandler
    {
        [TextArea] public string message;
        private CourierTooltip tooltip;
        private bool hovered,selected;
        private void Show(bool immediate=false){tooltip=GetComponentInParent<CourierTooltip>();if(tooltip!=null)tooltip.Show(this,immediate);}
        private void Hide(){if(!hovered&&!selected&&tooltip!=null)tooltip.Hide(this);}
        public void OnPointerEnter(PointerEventData data){hovered=true;Show();}
        public void OnPointerExit(PointerEventData data){hovered=false;Hide();}
        public void OnSelect(BaseEventData data){selected=data is AxisEventData||Input.GetKey(KeyCode.Tab)||Input.GetKey(KeyCode.LeftArrow)||Input.GetKey(KeyCode.RightArrow)||Input.GetKey(KeyCode.UpArrow)||Input.GetKey(KeyCode.DownArrow);if(selected)Show(true);}
        public void OnDeselect(BaseEventData data){selected=false;Hide();}
        public void OnPointerClick(PointerEventData data){if(tooltip!=null)tooltip.Hide(this);}
        private void OnDisable(){hovered=selected=false;Hide();}
    }
}
