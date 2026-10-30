using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace NightCourier
{
    public sealed class CourierFocusRing : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        public Outline ring;
        public void OnSelect(BaseEventData data) { if (ring != null) ring.enabled = true; }
        public void OnDeselect(BaseEventData data) { if (ring != null) ring.enabled = false; }
        private void OnDisable() { if (ring != null) ring.enabled = false; }
    }
}
