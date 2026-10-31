using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
namespace NightCourier
{
    // Shared presentation settings live on the prefab, not on individual buttons.
    public sealed class CourierHintView : MonoBehaviour
    {
        private void Awake(){CourierTheme.Apply(gameObject);}
        public void ApplyReadingStyle()
        {
            var image=GetComponent<Image>();
            if(image!=null){image.color=CourierColors.Paper;image.sprite=Resources.Load<Sprite>("NightVoyage/HUD/Panel");image.type=Image.Type.Sliced;image.raycastTarget=false;}
            var outline=GetComponent<Outline>();if(outline!=null)outline.enabled=false;
            if(label!=null){label.color=CourierColors.Night;label.font=Resources.Load<Font>("NightVoyage/Fonts/Nunito-SemiBold");label.fontSize=Mathf.Max(28,label.fontSize);label.supportRichText=true;label.lineSpacing=1.18f;label.alignment=TextAnchor.UpperLeft;}
            maxWidth=Mathf.Max(620,maxWidth);padding=Mathf.Max(28,padding);
        }
        public string FormatMessage(string message)
        {
            // Semantic colors are darkened for legibility against the existing paper asset.
            string green=ColorUtility.ToHtmlStringRGB(Color.Lerp(CourierColors.Night,CourierColors.Mint,.38f));
            string amber=ColorUtility.ToHtmlStringRGB(Color.Lerp(CourierColors.Night,CourierColors.Gold,.42f));
            string red=ColorUtility.ToHtmlStringRGB(Color.Lerp(CourierColors.Night,CourierColors.Heat,.52f));
            var lines=message.Replace("<","‹").Replace(">","›").Split('\n');
            if(lines.Length<2)return lines[0];
            lines[0]="<size=34><b><color=#"+green+">"+lines[0]+"</color></b></size>\n";
            for(int i=1;i<lines.Length;i++)
            {
                string text=lines[i];string lower=text.ToLowerInvariant();
                string tint=lower.Contains("damaged")||lower.Contains("failed")||lower.Contains("late")?red:
                    lower.Contains("expires")||lower.Contains("ends")||lower.Contains("exchange")||lower.Contains("cost")?amber:
                    lower.Contains("protect")||lower.Contains("keep dry")||lower.Contains("unlimited")?green:null;
                text=Regex.Replace(text,@"(?<!\w)[+\-]?\d+(?:[./]\d+)?%?",m=>"<b>"+m.Value+"</b>");
                if(tint!=null)text="<color=#"+tint+">"+text+"</color>";
                lines[i]=text;
            }
            return string.Join("\n",lines);
        }
        public Text label;
        public CanvasGroup group;
        [Min(0)] public float hoverDelay=.22f;
        [Min(0)] public float fadeDuration=.12f;
        [Min(120)] public float maxWidth=440;
        [Min(0)] public float padding=16;
    }
}
