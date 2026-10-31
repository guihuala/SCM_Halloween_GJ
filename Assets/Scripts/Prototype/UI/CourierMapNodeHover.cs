using UnityEngine;
using UnityEngine.EventSystems;

namespace NightCourier
{
    // Lives on the original node Button so landmark artwork and name share one hover target.
    public sealed class CourierMapNodeHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public CourierRoutePanel panel;
        public int node;
        public void OnPointerEnter(PointerEventData data) { if(panel!=null)panel.PreviewRoute(node); }
        public void OnPointerExit(PointerEventData data) { if(panel!=null)panel.ClearRoutePreview(node); }
        private void OnDisable() { if(panel!=null)panel.ClearRoutePreview(node); }
    }
}
