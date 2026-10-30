using UnityEngine;
using UnityEngine.EventSystems;
namespace NightCourier
{
    public sealed class CourierTrunkDropZone : MonoBehaviour,IDropHandler
    {
        public NightCourierPrototype Owner;
        public void OnDrop(PointerEventData data)
        {
            if(Owner==null||!Owner.Active||data.pointerDrag==null)return;
            var row=data.pointerDrag.GetComponent<CourierCargoRow>();
            if(row!=null)row.LoadByDrop();
        }
    }
}
