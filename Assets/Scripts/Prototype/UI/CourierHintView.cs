using UnityEngine;
using UnityEngine.UI;
namespace NightCourier
{
    // Shared presentation settings live on the prefab, not on individual buttons.
    public sealed class CourierHintView : MonoBehaviour
    {
        public Text label;
        public CanvasGroup group;
        [Min(0)] public float hoverDelay=.22f;
        [Min(0)] public float fadeDuration=.12f;
        [Min(120)] public float maxWidth=440;
        [Min(0)] public float padding=16;
    }
}
