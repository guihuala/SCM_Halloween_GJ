using UnityEngine.EventSystems;

namespace NightCourier
{
    // A focused language row uses the same left/right gesture as the volume sliders.
    public sealed class CourierLanguageSelector : UnityEngine.MonoBehaviour, IMoveHandler
    {
        public void OnMove(AxisEventData data)
        {
            if (data.moveDir != MoveDirection.Left && data.moveDir != MoveDirection.Right) return;
            CourierLocalization.Cycle(data.moveDir == MoveDirection.Left ? -1 : 1);
            data.Use();
        }
    }
}
