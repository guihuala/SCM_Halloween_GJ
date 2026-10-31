using UnityEngine;

namespace NightCourier
{
    [CreateAssetMenu(menuName="Night Courier/Game Palette")]
    public sealed class CourierPalette : ScriptableObject
    {
        [Header("Night and paper")]
        public Color night=CourierColors.Hex(0x172128);
        public Color panel=CourierColors.Hex(0x24323A);
        public Color raised=CourierColors.Hex(0x32454B);
        public Color paper=CourierColors.Hex(0xF1E7D2);
        public Color muted=CourierColors.Hex(0xB5C3BA);
        [Header("Meaning and interaction")]
        public Color mint=CourierColors.Hex(0x9AD5BA);
        public Color gold=CourierColors.Hex(0xE9B46C);
        public Color heat=CourierColors.Hex(0xDE9185);
        public Color water=CourierColors.Hex(0x87BFCC);
        public Color rough=CourierColors.Hex(0xC4AA79);
        public Color lavender=CourierColors.Hex(0xB7A5CD);
        public Color road=CourierColors.Hex(0x7D9891);
    }
    public static class CourierColors
    {
        private static CourierPalette current;
        public static CourierPalette Palette
        {
            get
            {
                if(current==null)current=Resources.Load<CourierPalette>("NightVoyage/Palette");
                if(current==null)current=ScriptableObject.CreateInstance<CourierPalette>();
                return current;
            }
        }
        public static void Reload(){current=null;}
        public static Color Hex(uint rgb)=>new Color(((rgb>>16)&255)/255f,((rgb>>8)&255)/255f,(rgb&255)/255f,1);
        public static Color Alpha(Color color,float alpha){color.a=alpha;return color;}
        public static Color Night=>Palette.night;
        public static Color Panel=>Palette.panel;
        public static Color Raised=>Palette.raised;
        public static Color Paper=>Palette.paper;
        public static Color Muted=>Palette.muted;
        public static Color Mint=>Palette.mint;
        public static Color Gold=>Palette.gold;
        public static Color Heat=>Palette.heat;
        public static Color Water=>Palette.water;
        public static Color Rough=>Palette.rough;
        public static Color Lavender=>Palette.lavender;
        public static Color Road=>Palette.road;
        public static Color Hover=>Color.Lerp(Mint,Paper,.35f);
        public static Color Pressed=>Color.Lerp(Mint,Night,.18f);
        public static Color Overlay=>Alpha(Night,.84f);
    }
}
