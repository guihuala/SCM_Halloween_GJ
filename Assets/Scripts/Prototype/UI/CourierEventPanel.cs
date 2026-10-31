using UnityEngine;
using UnityEngine.UI;
namespace NightCourier
{
    public sealed class CourierEventPanel : CourierPanel
    {
        public Text titleText,descriptionText,safeCostText,riskCostText;
        public Button safeButton,riskButton;
        public CourierIconGraphic fragileRisk,dryRisk;
        public Image illustration;
        private string illustrationKey;
        protected override void Awake()
        {
            base.Awake();safeButton.onClick.AddListener(()=>owner.ResolveEncounter(true));riskButton.onClick.AddListener(()=>owner.ResolveEncounter(false));
            if(illustration==null)
            {
                var go=new GameObject("EventIllustration",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
                go.transform.SetParent(descriptionText.transform.parent,false);illustration=go.GetComponent<Image>();
            }
            illustration.preserveAspect=true;illustration.raycastTarget=false;
        }
        private static void Place(RectTransform rect,float x,float y,float width,float height)
        {
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);
        }
        protected override void Refresh()
        {
            var e=Run.Events[Run.EncounterIndex];titleText.text=e.Title;
            ((RectTransform)descriptionText.transform.parent).sizeDelta=new Vector2(1000,600);
            Place(titleText.rectTransform,32,24,936,58);
            Place(illustration.rectTransform,32,104,400,300);
            Place(descriptionText.rectTransform,464,104,504,344);
            descriptionText.fontSize=28;descriptionText.alignment=TextAnchor.UpperLeft;
            descriptionText.text=e.Description+"\n\n"+Run.EventCost+".";
            if(illustrationKey!=e.Illustration)
            {illustrationKey=e.Illustration;illustration.sprite=Resources.Load<Sprite>("NightVoyage/EventIllustrations/"+illustrationKey);}
            illustration.color=Color.white;
            illustration.gameObject.SetActive(illustration.sprite!=null);
            Caption(safeButton,e.Safe.Split('·')[0].Trim());riskButton.gameObject.SetActive(false);
            Place((RectTransform)safeButton.transform,856,492,112,76);
            CourierSymbolUI.Button(safeButton,CourierIconKind.Check,Run.SafeChoice);
            var buttonImage=safeButton.targetGraphic as Image;if(buttonImage!=null)buttonImage.color=CourierColors.Mint;
            var label=safeButton.GetComponentInChildren<Text>();label.color=CourierColors.Night;label.rectTransform.sizeDelta=new Vector2(880,label.rectTransform.sizeDelta.y);
            var hint=safeButton.GetComponent<CourierIconHint>();if(hint!=null)hint.message=Run.SafeChoice;
            safeCostText.text="";riskCostText.text="";
            var clock=safeButton.transform.Find("ClockIcon");if(clock!=null)clock.gameObject.SetActive(false);
            fragileRisk.gameObject.SetActive(false);dryRisk.gameObject.SetActive(false);
        }
    }
}
