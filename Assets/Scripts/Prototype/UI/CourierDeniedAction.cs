using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace NightCourier
{
    // Disabled buttons still explain themselves, without enabling their game action.
    public sealed class CourierDeniedAction : MonoBehaviour, IPointerClickHandler, ISubmitHandler
    {
        private Button button;
        private NightCourierPrototype owner;
        private Func<string> reason;
        public static void Attach(Button target, NightCourierPrototype prototype, Func<string> message)
        {
            if (target == null) return;
            var hint = target.GetComponent<CourierDeniedAction>() ?? target.gameObject.AddComponent<CourierDeniedAction>();
            hint.button = target; hint.owner = prototype; hint.reason = message;
        }
        private void Explain()
        {
            if (button == null || button.interactable || owner == null || !owner.Active || owner.Run.Finished) return;
            owner.RejectAction(reason != null ? reason() : "", transform as RectTransform);
        }
        public void OnPointerClick(PointerEventData data) { if (data.button == PointerEventData.InputButton.Left) Explain(); }
        public void OnSubmit(BaseEventData data) { Explain(); }
    }
}
