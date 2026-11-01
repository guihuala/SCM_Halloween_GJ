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
        private Sprite[] eventArtwork;
        protected override void Awake()
        {
            base.Awake();safeButton.onClick.AddListener(()=>owner.ResolveEncounter(true));riskButton.onClick.AddListener(()=>owner.ResolveEncounter(false));
            eventArtwork=Resources.LoadAll<Sprite>("NightVoyage/EventArtwork/event_silhouettes_atlas");
            if(illustration==null)
            {
                var art=new GameObject("EventIllustration",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
                art.transform.SetParent(descriptionText.transform.parent,false);
                illustration=art.GetComponent<Image>();
            }
            illustration.raycastTarget=false;illustration.preserveAspect=true;
            illustration.color=Color.white;illustration.overrideSprite=null;
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
            var e=Run.Events[Run.EncounterIndex];NightCourier.CourierLocalization.Set(titleText, e.Title);
            Sprite artwork=null;
            if(eventArtwork!=null)foreach(var sprite in eventArtwork)if(sprite.name==e.Illustration){artwork=sprite;break;}
            illustration.sprite=artwork;illustration.gameObject.SetActive(artwork!=null);
            // Give the illustration a full body column, balanced against the story text.
            float titleWidth=800;
            Place(titleText.rectTransform,40,32,titleWidth,56);
            titleText.fontSize=40;titleText.alignment=TextAnchor.UpperLeft;
            titleText.color=CourierColors.Night;
            float titleHeight=Mathf.Max(56,Mathf.Ceil(titleText.preferredHeight/8)*8);
            titleText.rectTransform.sizeDelta=new Vector2(titleWidth,titleHeight);
            float bodyY=Mathf.Max(112,32+titleHeight+24);
            float textX=artwork!=null?304:40;
            float textWidth=artwork!=null?536:800;
            Place(descriptionText.rectTransform,textX,bodyY,textWidth,64);
            descriptionText.fontSize=28;descriptionText.alignment=TextAnchor.UpperLeft;
            descriptionText.lineSpacing=1.12f;descriptionText.color=CourierColors.Night;
            NightCourier.CourierLocalization.Set(descriptionText, e.Description);
            float contentHeight=Mathf.Max(64,Mathf.Ceil(descriptionText.preferredHeight/8)*8);
            float bodyHeight=Mathf.Max(artwork!=null?232:64,contentHeight);
            Place(descriptionText.rectTransform,textX,bodyY+(bodyHeight-contentHeight)*.5f,textWidth,contentHeight);
            Place(illustration.rectTransform,40,bodyY+(bodyHeight-232)*.5f,232,232);
            float buttonY=bodyY+bodyHeight+24;
            impactText.font=CourierTypography.Body;impactText.fontSize=26;
            impactText.alignment=TextAnchor.MiddleLeft;impactText.lineSpacing=1.1f;
            impactText.color=Color.Lerp(CourierColors.Night,CourierColors.Paper,.22f);
            NightCourier.CourierLocalization.Set(impactText, e.Safe);
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
            NightCourier.CourierLocalization.Set(safeCostText, "");NightCourier.CourierLocalization.Set(riskCostText, "");
            var clock=safeButton.transform.Find("ClockIcon");if(clock!=null)clock.gameObject.SetActive(false);
            fragileRisk.gameObject.SetActive(false);dryRisk.gameObject.SetActive(false);
        }
    }
}
