using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace NightCourier
{
    public sealed class CourierResultPanel : CourierPanel
    {
        public Text titleText,summaryText;
        public Button restartButton,menuButton;
        public ScrollRect orderHistory;
        public RectTransform orderCards;
        private CourierRun displayedRun;
        private int displayedCount=-1;
        private CourierCargoIconCatalog cargoCatalog;
        private CourierCharacterCatalog characters;
        private Sprite paper;
        private float contentWidth;
        protected override void Awake()
        {
            base.Awake();
            restartButton.onClick.AddListener(()=>owner.RestartShift());
            menuButton.onClick.AddListener(()=>GameManager.Instance.ReturnToMainMenu());
        }
        protected override void Refresh()
        {
            if(orderHistory==null)PrepareHistory();
            if(displayedRun==Run&&displayedCount==Run.Results.Count)return;
            displayedRun=Run;displayedCount=Run.Results.Count;
            titleText.text="Deliveries complete";
            summaryText.text=Run.CompletedBatches+" orders delivered  ·  "+Run.Results.Count(r=>!r.Late&&!r.Damaged)+" on time & intact";
            foreach(Transform child in orderCards){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            for(int i=0;i<Run.Results.Count;i++)BuildCard(Run.Results[i],i);
            orderCards.sizeDelta=new Vector2(contentWidth,Mathf.Max(orderHistory.viewport.rect.height,Mathf.Ceil(Run.Results.Count/2f)*180));
            orderHistory.StopMovement();orderHistory.verticalNormalizedPosition=1;
        }
        private static void Place(RectTransform rect,float x,float y,float width,float height)
        {
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);
        }
        private static RectTransform Rect(string name,Transform parent,float x,float y,float width,float height)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            Place(rect,x,y,width,height);return rect;
        }
        private Text Label(string name,Transform parent,string text,float x,float y,float width,float height,int size=24)
        {
            var rect=Rect(name,parent,x,y,width,height);var label=rect.gameObject.AddComponent<Text>();
            label.font=owner.UIFont;label.fontSize=size;label.text=text;label.color=CourierColors.Night;
            label.alignment=TextAnchor.UpperLeft;label.raycastTarget=false;label.horizontalOverflow=HorizontalWrapMode.Wrap;
            return label;
        }
        private Image Image(string name,Transform parent,Sprite sprite,float x,float y,float width,float height,Color tint,bool sliced=false)
        {
            var rect=Rect(name,parent,x,y,width,height);var image=rect.gameObject.AddComponent<Image>();
            image.sprite=sprite;image.color=tint;image.raycastTarget=false;image.preserveAspect=!sliced;
            if(sliced)image.type=UnityEngine.UI.Image.Type.Sliced;return image;
        }
        private void Icon(string name,Transform parent,CourierIconKind kind,float x,float y,float size,Color color)
        {
            var rect=Rect(name,parent,x,y,size,size);var icon=rect.gameObject.AddComponent<CourierIconGraphic>();
            icon.SetKind(kind);icon.color=color;icon.raycastTarget=false;
        }
        private void PrepareHistory()
        {
            cargoCatalog=Resources.Load<CourierCargoIconCatalog>("NightVoyage/CargoIcons");
            characters=Resources.Load<CourierCharacterCatalog>("NightVoyage/Characters");
            paper=Resources.Load<Sprite>("NightVoyage/HUD/Panel");
            var window=(RectTransform)summaryText.transform.parent;window.sizeDelta=new Vector2(1280,800);
            Place(titleText.rectTransform,32,28,1216,60);titleText.fontSize=44;titleText.alignment=TextAnchor.UpperLeft;
            Place(summaryText.rectTransform,32,94,1216,36);summaryText.fontSize=26;summaryText.resizeTextForBestFit=false;
            summaryText.alignment=TextAnchor.UpperLeft;
            var viewport=Rect("OrderHistory",window,32,156,1216,536);
            var background=viewport.gameObject.AddComponent<Image>();background.color=CourierColors.Paper;background.raycastTarget=true;
            viewport.gameObject.AddComponent<RectMask2D>();
            orderHistory=viewport.gameObject.AddComponent<ScrollRect>();orderHistory.viewport=viewport;orderHistory.horizontal=false;
            orderHistory.movementType=ScrollRect.MovementType.Clamped;orderHistory.scrollSensitivity=48;
            contentWidth=1168;orderCards=Rect("DeliveryCards",viewport,0,0,contentWidth,536);orderHistory.content=orderCards;
            var scrollbarSprite=Resources.Load<Sprite>("NightVoyage/HUD/Button");
            var track=Rect("ScrollTrack",viewport,1184,0,32,536);
            var trackImage=track.gameObject.AddComponent<Image>();trackImage.sprite=scrollbarSprite;
            trackImage.type=UnityEngine.UI.Image.Type.Sliced;trackImage.color=Color.Lerp(CourierColors.Paper,CourierColors.Night,.12f);
            var scrollbar=track.gameObject.AddComponent<Scrollbar>();scrollbar.direction=Scrollbar.Direction.BottomToTop;
            var handle=Rect("Handle",track,0,0,32,64);var handleImage=handle.gameObject.AddComponent<Image>();
            handleImage.sprite=scrollbarSprite;handleImage.type=UnityEngine.UI.Image.Type.Sliced;
            handleImage.color=Color.Lerp(CourierColors.Mint,CourierColors.Night,.18f);
            scrollbar.handleRect=handle;scrollbar.targetGraphic=handleImage;orderHistory.verticalScrollbar=scrollbar;
            PrepareButton(restartButton,"Another shift",792);
            PrepareButton(menuButton,"Back home",1028);
        }
        private void PrepareButton(Button button,string caption,float x)
        {
            Place((RectTransform)button.transform,x,712,220,64);
            var icon=button.GetComponentInChildren<CourierIconGraphic>(true);
            if(icon!=null)Place(icon.rectTransform,16,12,40,40);
            var image=button.targetGraphic as Image;if(image!=null)image.color=button==restartButton?CourierColors.Mint:Color.Lerp(CourierColors.Paper,CourierColors.Muted,.28f);
            var label=Label("ActionLabel",button.transform,caption,64,12,144,40,26);label.alignment=TextAnchor.MiddleLeft;
        }
        private void BuildCard(DeliveryResult result,int index)
        {
            float width=(contentWidth-16)/2;
            bool success=!result.Late&&!result.Damaged;
            Color accent=success?CourierColors.Mint:result.Damaged?CourierColors.Heat:CourierColors.Gold;
            var card=Image("DeliveryCard_"+(index+1),orderCards,paper,index%2*(width+16),index/2*180,width,164,
                Color.Lerp(CourierColors.Paper,CourierColors.Muted,.14f),true).rectTransform;
            var items=Run.Parcels.Where(p=>p.Origin==result.Origin&&p.Destination==result.Destination).ToArray();
            var sprite=items.Length>0&&cargoCatalog!=null?cargoCatalog.ForParcel(Run.Parcels.IndexOf(items[0])):null;
            if(sprite!=null)Image("CargoArtwork",card,sprite,16,28,96,96,Color.white);
            else Icon("CargoFallback",card,CourierIconKind.Box,24,36,80,CourierColors.Night);
            if(items.Length>1&&cargoCatalog!=null)
            {
                var second=cargoCatalog.ForParcel(Run.Parcels.IndexOf(items[1]));
                if(second!=null)Image("SecondaryCargoArtwork",card,second,66,88,48,48,Color.white);
            }
            var name=Label("CargoName",card,result.Cargo,128,16,width-220,64,28);
            name.resizeTextForBestFit=true;name.resizeTextMinSize=22;name.resizeTextMaxSize=28;
            var recipient=characters!=null?characters.ForStop(result.Destination):null;
            var portrait=characters!=null?characters.PortraitFor(recipient):null;
            if(portrait!=null)Image("RecipientPortrait",card,portrait,width-76,16,56,64,Color.white);
            Label("RecipientName",card,"For "+(recipient!=null?recipient.displayName:Run.Stops[result.Destination].Name),128,80,width-148,32,23);
            Icon("OutcomeIcon",card,success?CourierIconKind.Check:CourierIconKind.Warning,128,124,24,Color.Lerp(CourierColors.Night,accent,.25f));
            var status=Label("Outcome",card,success?"On time & intact":result.Late&&result.Damaged?"Late + damaged":result.Damaged?"Damaged":"Late delivery",160,120,240,32,23);
            status.alignment=TextAnchor.MiddleLeft;
            var turns=Label("Turns",card,result.Turns+" / "+result.TargetTurns+" turns",width-170,120,150,32,23);
            turns.alignment=TextAnchor.MiddleRight;
            // Keep detailed damage attribution available without turning the album into a log.
            var hint=card.gameObject.AddComponent<CourierIconHint>();hint.message=result.Cargo+"\n"+Run.Stops[result.Origin].Name+" > "+Run.Stops[result.Destination].Name+"\n"+result.OutcomeDetail;
            card.GetComponent<Image>().raycastTarget=true;

        }
    }
}
