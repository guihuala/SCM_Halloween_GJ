#if UNITY_EDITOR
using System;
using UnityEngine;
namespace NightCourier
{
    // UI raycasts must run in the player's frame: EditorApplication.update exposes editor
    // Screen dimensions, which differ from fixed-resolution Game View canvas coordinates.
    public sealed class CourierStreetMapCheckPump : MonoBehaviour
    {
        public Action Check;
        private void Update(){if(Check!=null)Check();}
    }
}
#endif
