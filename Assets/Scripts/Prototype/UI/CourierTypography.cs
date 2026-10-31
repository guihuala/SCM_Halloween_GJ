using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    // Two roles shared by serialized UI, prefab builders and runtime-created labels.
    public static class CourierTypography
    {
        private static Font body, display;
        public static Font Body => body != null ? body : (body = Resources.Load<Font>("NightVoyage/Fonts/Itim-Regular"));
        public static Font Display => display != null ? display : (display = Resources.Load<Font>("NightVoyage/Fonts/ShortStack-Regular"));

        public static Font For(Text text)
        {
            string name = text.name.ToLowerInvariant();
            bool heading = (name.Contains("title") && !name.Contains("subtitle")) || name.Contains("heading") || name.StartsWith("streetlabel_");
            bool caption = (name == "label" || name == "text" || name == "text (legacy)") && text.GetComponentInParent<Button>(true) != null;
            return heading || caption ? Display : Body;
        }

        public static void Apply(GameObject root)
        {
            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                text.font = For(text);
                text.fontStyle = FontStyle.Normal;
            }
        }
    }
}
