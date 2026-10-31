using UnityEngine;
using UnityEngine.EventSystems;

namespace NightCourier
{
    // Only the map moves. HUD, destination cards and orders retain their readable size.
    [DefaultExecutionOrder(-20)]
    public sealed class CourierMapCanvas : MonoBehaviour, IPointerClickHandler, IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform content;
        public CourierRoutePanel panel;
        public float zoom = 1.16f;
        private Vector2 pan, renderedPan, velocity;
        private Vector2 size;
        private float renderedZoom, lastDragTime;
        private bool dragging, initialized;
        private RectTransform Viewport => (RectTransform)transform;
        private bool ReducedMotion => panel!=null&&panel.MapReducedMotion;
        public void ApplyLayout()
        {
            if(content==null)return;
            if(size!=Viewport.rect.size){size=Viewport.rect.size;pan=renderedPan=velocity=Vector2.zero;}
            if(!initialized){renderedZoom=zoom;renderedPan=pan;initialized=true;}
            pan=ClampPan(pan,zoom);
            Render();
        }
        private Vector2 ClampPan(Vector2 offset,float scale)
        {
            // Leave a margin so edge landmarks can always be brought comfortably into view.
            Vector2 limit=new Vector2(Mathf.Max(0,(content.rect.width*scale-size.x)*.5f)+size.x*.24f,
                Mathf.Max(0,(content.rect.height*scale-size.y)*.5f)+size.y*.24f);
            return new Vector2(Mathf.Clamp(offset.x,-limit.x,limit.x),Mathf.Clamp(offset.y,-limit.y,limit.y));
        }
        private void Render()
        {
            content.localScale=Vector3.one*renderedZoom;
            content.anchoredPosition=new Vector2((size.x-content.rect.width*renderedZoom)*.5f,-(size.y-content.rect.height*renderedZoom)*.5f)+renderedPan;
        }
        private void LateUpdate()
        {
            if(content==null)return;
            if(!initialized)ApplyLayout();
            float dt=Mathf.Min(Time.unscaledDeltaTime,.05f);
            if(panel==null||!panel.CanNavigateMap)
            {dragging=false;velocity=Vector2.zero;zoom=renderedZoom;pan=renderedPan;return;}
            if(ReducedMotion){velocity=Vector2.zero;renderedZoom=zoom;renderedPan=pan;Render();return;}
            if(!dragging&&velocity.sqrMagnitude>1)
            {
                // Integrate exponential friction, independent of the frame rate.
                float decay=Mathf.Exp(-9f*dt);
                Vector2 proposed=pan+velocity*((1-decay)/9f);
                pan=ClampPan(proposed,zoom);velocity*=decay;
                if(!Mathf.Approximately(pan.x,proposed.x))velocity.x=0;
                if(!Mathf.Approximately(pan.y,proposed.y))velocity.y=0;
            }
            float blend=1-Mathf.Exp(-20f*dt);
            renderedZoom=Mathf.Lerp(renderedZoom,zoom,blend);
            renderedPan=dragging?pan:Vector2.Lerp(renderedPan,pan,blend);
            if(Mathf.Abs(renderedZoom-zoom)<.0001f)renderedZoom=zoom;
            if((renderedPan-pan).sqrMagnitude<.001f)renderedPan=pan;
            Render();
        }
        public void OnScroll(PointerEventData data)
        {
            if(panel==null||!panel.CanNavigateMap)return;
            if(!initialized)ApplyLayout();
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(Viewport,data.position,data.pressEventCamera,out var point))return;
            velocity=Vector2.zero;
            // Interrupt from the visible pose so changing the wheel's direction never jumps.
            pan=renderedPan;zoom=renderedZoom;
            float next=Mathf.Clamp(zoom*Mathf.Pow(1.12f,data.scrollDelta.y),.85f,2.4f);
            pan=(point-Viewport.rect.center)-(point-Viewport.rect.center-pan)*(next/zoom);
            zoom=next;pan=ClampPan(pan,zoom);
            if(ReducedMotion){renderedZoom=zoom;renderedPan=pan;Render();}
        }
        public void OnBeginDrag(PointerEventData data)
        {
            dragging=panel!=null&&panel.CanNavigateMap&&(data.button==PointerEventData.InputButton.Left||data.button==PointerEventData.InputButton.Middle);
            if(!dragging)return;
            if(!initialized)ApplyLayout();
            zoom=renderedZoom;pan=renderedPan;velocity=Vector2.zero;
        }
        public void OnDrag(PointerEventData data)
        {
            if(!dragging||!panel.CanNavigateMap)return;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(Viewport,data.position,data.pressEventCamera,out var current)&&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(Viewport,data.position-data.delta,data.pressEventCamera,out var previous))
            {
                Vector2 delta=current-previous;
                pan=ClampPan(pan+delta,zoom);renderedPan=pan;Render();
                velocity=ReducedMotion?Vector2.zero:Vector2.ClampMagnitude(delta/Mathf.Max(.008f,Time.unscaledDeltaTime),1400);
                lastDragTime=Time.unscaledTime;
            }
        }
        public void OnEndDrag(PointerEventData data){dragging=false;if(Time.unscaledTime-lastDragTime>.08f)velocity=Vector2.zero;}
        public void OnPointerClick(PointerEventData data)
        {
            if(panel==null||!panel.CanNavigateMap||data.button!=PointerEventData.InputButton.Left||
                (data.position-data.pressPosition).sqrMagnitude>36)return;
            velocity=Vector2.zero;panel.ClearDestination();
        }
        private void OnDisable(){dragging=false;velocity=Vector2.zero;initialized=false;}
    }
}
