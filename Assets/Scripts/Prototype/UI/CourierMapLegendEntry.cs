using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NightCourier
{
    // The whole row, including its icon and label, is one hover target.
    public sealed class CourierMapLegendEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public CourierMapLegend legend;
        public int target;
        public void SetHighlighted(bool value)
        {GetComponent<Image>().color=CourierColors.Alpha(CourierColors.Mint,value?.42f:0);}
        public void OnPointerEnter(PointerEventData data){if(legend!=null)legend.PreviewEntry(this);}
        public void OnPointerExit(PointerEventData data){if(legend!=null)legend.ClearEntry(this);}
        private void OnDisable(){if(legend!=null)legend.ClearEntry(this);}
    }
}
