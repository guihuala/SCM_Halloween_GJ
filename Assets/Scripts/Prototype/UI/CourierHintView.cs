using UnityEngine;
using UnityEngine.UI;
namespace NightCourier
{
    // Shared presentation settings live on the prefab, not on individual buttons.
    public sealed class CourierHintView : MonoBehaviour
    {
        private void Awake(){CourierTheme.Apply(gameObject);}
        public void ApplyReadingStyle()
        {
            var image=GetComponent<UnityEngine.UI.Image>();if(image!=null)image.color=CourierColors.Paper;
            if(label!=null){label.color=CourierColors.Night;label.font=Resources.Load<Font>("NightVoyage/Fonts/Nunito-SemiBold");label.fontSize=Mathf.Max(28,label.fontSize);}
            maxWidth=Mathf.Max(520,maxWidth);padding=Mathf.Max(20,padding);
        }
        public Text label;
        public CanvasGroup group;
        [Min(0)] public float hoverDelay=.22f;
        [Min(0)] public float fadeDuration=.12f;
        [Min(120)] public float maxWidth=440;
        [Min(0)] public float padding=16;
    }
}
