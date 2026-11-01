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
        private Text impactText;
        protected override void Awake()
        {
            base.Awake();safeButton.onClick.AddListener(()=>owner.ResolveEncounter(true));riskButton.onClick.AddListener(()=>owner.ResolveEncounter(false));
            // Keep compatibility with older prefabs without reserving an art slot.
            if(illustration!=null)
            {
                illustration.sprite=null;illustration.overrideSprite=null;
                illustration.gameObject.SetActive(false);
            }
            impactText=Instantiate(descriptionText,descriptionText.transform.parent);
            impactText.name="EventImpactText";impactText.raycastTarget=false;
        }
        private static void Place(RectTransform rect,float x,float y,float width,float height)
        {
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);
        }
        protected override void Refresh()
        {
            var e=Run.Events[Run.EncounterIndex];titleText.text=e.Title;
            // Text flows vertically on one shared alignment, with no reserved art space.
            Place(titleText.rectTransform,40,32,800,56);
            titleText.fontSize=40;titleText.alignment=TextAnchor.UpperLeft;
            titleText.color=CourierColors.Night;
            Place(descriptionText.rectTransform,40,112,800,64);
            descriptionText.fontSize=28;descriptionText.alignment=TextAnchor.UpperLeft;
            descriptionText.lineSpacing=1.12f;descriptionText.color=CourierColors.Night;
            descriptionText.text=e.Description;
            float contentHeight=Mathf.Max(64,Mathf.Ceil(descriptionText.preferredHeight/8)*8);
            descriptionText.rectTransform.sizeDelta=new Vector2(800,contentHeight);
            float buttonY=112+contentHeight+44;
            impactText.font=CourierTypography.Body;impactText.fontSize=26;
            impactText.alignment=TextAnchor.MiddleLeft;impactText.lineSpacing=1.1f;
            impactText.color=Color.Lerp(CourierColors.Night,CourierColors.Paper,.22f);
            impactText.text=Run.EventCost+".";
            Place(impactText.rectTransform,40,buttonY,688,64);
            float footerHeight=Mathf.Max(64,Mathf.Ceil(impactText.preferredHeight/8)*8);
            impactText.rectTransform.sizeDelta=new Vector2(688,footerHeight);
            ((RectTransform)descriptionText.transform.parent).sizeDelta=new Vector2(880,buttonY+footerHeight+32);
            Caption(safeButton,e.Safe.Split('·')[0].Trim());riskButton.gameObject.SetActive(false);
            Place((RectTransform)safeButton.transform,752,buttonY+(footerHeight-64)*.5f,88,64);
            CourierSymbolUI.Button(safeButton,CourierIconKind.Check,Run.SafeChoice);
            var buttonImage=safeButton.targetGraphic as Image;if(buttonImage!=null)buttonImage.color=CourierColors.Mint;
            var label=safeButton.GetComponentInChildren<Text>();label.color=CourierColors.Night;
            var hint=safeButton.GetComponent<CourierIconHint>();if(hint!=null)hint.message=Run.SafeChoice;
            safeCostText.text="";riskCostText.text="";
            var clock=safeButton.transform.Find("ClockIcon");if(clock!=null)clock.gameObject.SetActive(false);
            fragileRisk.gameObject.SetActive(false);dryRisk.gameObject.SetActive(false);
        }
    }
}
