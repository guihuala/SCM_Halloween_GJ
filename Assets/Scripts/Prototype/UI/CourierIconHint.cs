using UnityEngine;
using UnityEngine.EventSystems;
namespace NightCourier
{
    public sealed class CourierIconHint : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler,IPointerClickHandler
    {
        [TextArea] public string message;
        private CourierTooltip tooltip;
        private void Show(){tooltip=GetComponentInParent<CourierTooltip>();if(tooltip!=null)tooltip.Show(this);}
        private void Hide(){if(tooltip!=null)tooltip.Hide(this);}
        public void OnPointerEnter(PointerEventData data){Show();}
        public void OnPointerExit(PointerEventData data){Hide();}
        public void OnSelect(BaseEventData data){if(data is AxisEventData||Input.GetKey(KeyCode.Tab)||Input.GetKey(KeyCode.LeftArrow)||Input.GetKey(KeyCode.RightArrow)||Input.GetKey(KeyCode.UpArrow)||Input.GetKey(KeyCode.DownArrow))Show();}
        public void OnDeselect(BaseEventData data){Hide();}
        public void OnPointerClick(PointerEventData data){Hide();}
        private void OnDisable(){Hide();}
    }
}
