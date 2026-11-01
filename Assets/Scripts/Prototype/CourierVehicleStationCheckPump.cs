#if UNITY_EDITOR
using System;
using UnityEngine;
namespace NightCourier
{
    // Keep station raycasts in Game View coordinates and independent of other smoke checks.
    public sealed class CourierVehicleStationCheckPump : MonoBehaviour
    {
        public Action Check;
        private void Update(){Check?.Invoke();}
    }
}
#endif
