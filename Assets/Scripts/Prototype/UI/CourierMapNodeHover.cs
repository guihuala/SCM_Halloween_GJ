using UnityEngine;
using UnityEngine.EventSystems;

namespace NightCourier
{
    // Lives on the original node Button so landmark artwork and name share one hover target.
    public sealed class CourierMapNodeHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ICanvasRaycastFilter
    {
        public CourierRoutePanel panel;
        public int node;
        public void OnPointerEnter(PointerEventData data) { if(panel!=null)panel.PreviewRoute(node); }
        public void OnPointerExit(PointerEventData data) { if(panel!=null)panel.ClearRoutePreview(node); }
        // Longer labels must never steal clicks from a neighbouring drawn junction.
        public bool IsRaycastLocationValid(Vector2 screenPoint,Camera eventCamera)
        {
            if(panel==null||panel.mapGraphic==null)return true;
            var map=panel.mapGraphic.rectTransform;
            Vector2 local;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(map,screenPoint,eventCamera,out local))return true;
            var point=new Vector2(local.x-map.rect.xMin,map.rect.yMax-local.y);
            foreach(var other in CourierStreetMap.Nodes)
                if(other.StopId!=node&&(CourierRouteGraphic.MapPoint(other.Point,map.rect.size)-point).sqrMagnitude<28*28)return false;
            return true;
        }
        private void OnDisable() { if(panel!=null)panel.ClearRoutePreview(node); }
    }
}
