using UnityEngine;
using UnityEngine.EventSystems;

namespace NightCourier
{
    // Only the map moves. HUD, destination cards and orders retain their readable size.
    public sealed class CourierMapCanvas : MonoBehaviour, IPointerClickHandler, IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform content;
        public CourierRoutePanel panel;
        public float zoom = 1.16f;
        private Vector2 pan;
        private Vector2 size;
        private bool dragging;
        private RectTransform Viewport => (RectTransform)transform;
        public void ApplyLayout()
        {
            if(content==null)return;
            if(size!=Viewport.rect.size){size=Viewport.rect.size;pan=Vector2.zero;}
            ClampPan();
            content.localScale=Vector3.one*zoom;
            content.anchoredPosition=new Vector2((size.x-content.rect.width*zoom)*.5f,-(size.y-content.rect.height*zoom)*.5f)+pan;
        }
        private void ClampPan()
        {
            // Leave a margin so edge landmarks can always be brought comfortably into view.
            Vector2 limit=new Vector2(Mathf.Max(0,(content.rect.width*zoom-size.x)*.5f)+size.x*.24f,
                Mathf.Max(0,(content.rect.height*zoom-size.y)*.5f)+size.y*.24f);
            pan=new Vector2(Mathf.Clamp(pan.x,-limit.x,limit.x),Mathf.Clamp(pan.y,-limit.y,limit.y));
        }
        public void OnScroll(PointerEventData data)
        {
            if(panel==null||!panel.CanNavigateMap)return;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(Viewport,data.position,data.pressEventCamera,out var point))return;
            float next=Mathf.Clamp(zoom*Mathf.Pow(1.12f,data.scrollDelta.y),.85f,2.4f);
            Vector2 center=Viewport.rect.center;
            pan=(point-center)-(point-center-pan)*(next/zoom);zoom=next;ApplyLayout();
        }
        public void OnBeginDrag(PointerEventData data)
        {dragging=panel!=null&&panel.CanNavigateMap&&(data.button==PointerEventData.InputButton.Left||data.button==PointerEventData.InputButton.Middle);}
        public void OnDrag(PointerEventData data)
        {
            if(!dragging||!panel.CanNavigateMap)return;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(Viewport,data.position,data.pressEventCamera,out var current)&&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(Viewport,data.position-data.delta,data.pressEventCamera,out var previous))
            {pan+=current-previous;ApplyLayout();}
        }
        public void OnEndDrag(PointerEventData data){dragging=false;}
        public void OnPointerClick(PointerEventData data)
        {
            if(panel==null||!panel.CanNavigateMap||data.button!=PointerEventData.InputButton.Left||
                (data.position-data.pressPosition).sqrMagnitude>36)return;
            panel.ClearDestination();
        }
        private void OnDisable(){dragging=false;}
    }
}
