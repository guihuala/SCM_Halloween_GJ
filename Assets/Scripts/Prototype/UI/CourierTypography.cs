using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    // Two roles shared by serialized UI, prefab builders and runtime-created labels.
    public static class CourierTypography
    {
        private static Font body, display, chinese;
        public static Font Chinese => chinese != null ? chinese : (chinese = Resources.Load<Font>("NightVoyage/Fonts/Xiaolai-Regular"));
        public static Font Body => CourierLocalization.IsChinese ? Chinese : (body != null ? body : (body = Resources.Load<Font>("NightVoyage/Fonts/Itim-Regular")));
        public static Font Display => CourierLocalization.IsChinese ? Chinese : (display != null ? display : (display = Resources.Load<Font>("NightVoyage/Fonts/ShortStack-Regular")));

        public static Font For(Text text)
        {
            string name = text.name.ToLowerInvariant();
            bool heading = (name.Contains("title") && !name.Contains("subtitle")) || name.Contains("heading") || name.StartsWith("streetlabel_");
            bool caption = (name == "label" || name == "text" || name == "text (legacy)") && text.GetComponentInParent<Button>(true) != null;
            return heading || caption ? Display : Body;
        }

        public static void Apply(GameObject root)
        {
            CourierLocalization.Apply(root);
        }
    }
}
