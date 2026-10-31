using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace NightCourier
{
    public sealed class CourierCargoRow : MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        public Text nameText,destinationText,weightText,timeText;
        public CourierIconGraphic actionIcon,storyIcon,fragileIcon,heavyIcon,dryIcon,damagedIcon;
        public GameObject timeGroup;
        public CourierIconHint actionHint,timeHint;
        public Button actionButton;
        [Header("Cargo Artwork")]
        public CourierCargoIconCatalog iconCatalog;
        public Image cargoArtwork, secondaryArtwork;
        private Parcel parcel;
        public void PrepareArtwork()
        {
            if(iconCatalog==null)iconCatalog=Resources.Load<CourierCargoIconCatalog>("NightVoyage/CargoIcons");
            cargoArtwork=EnsureArtwork(cargoArtwork,"CargoArtwork",12,16,88);
            secondaryArtwork=EnsureArtwork(secondaryArtwork,"SecondaryCargoArtwork",56,65,48);
            if(cargoArtwork.sprite==null&&iconCatalog!=null)cargoArtwork.sprite=iconCatalog.ForParcel(0);
            secondaryArtwork.gameObject.SetActive(false);
            var pin=transform.Find("RecipientIcon");if(pin!=null)pin.gameObject.SetActive(false);
        }
        private Image EnsureArtwork(Image current,string objectName,float x,float y,float size)
        {
            if(current==null)
            {
                var child=transform.Find(objectName);
                current=child!=null?child.GetComponent<Image>():null;
                if(current==null){var go=new GameObject(objectName,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(transform,false);current=go.GetComponent<Image>();}
            }
            var rt=current.rectTransform;rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);
            rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(size,size);
            current.preserveAspect=true;current.raycastTarget=false;current.color=Color.white;
            return current;
        }
        private void RefreshArtwork()
        {
            if(cargoArtwork==null||secondaryArtwork==null)PrepareArtwork();
            var items=owner.Run.Batch(parcel).ToArray();
            cargoArtwork.sprite=iconCatalog!=null?iconCatalog.ForParcel(owner.Run.Parcels.IndexOf(parcel)):null;
            cargoArtwork.gameObject.SetActive(cargoArtwork.sprite!=null);
            secondaryArtwork.sprite=items.Length>1&&iconCatalog!=null?iconCatalog.ForParcel(owner.Run.Parcels.IndexOf(items[1])):null;
            secondaryArtwork.gameObject.SetActive(secondaryArtwork.sprite!=null);
        }
        public Parcel Item {get{return parcel;}}
        private NightCourierPrototype owner;
        private GameObject dragVisual;
        public Vector3? DropFeedbackOrigin { get; private set; }
        public void Bind(NightCourierPrototype prototype,Parcel item)
        {
            owner=prototype;parcel=item;DropFeedbackOrigin=null;PrepareArtwork();RefreshArtwork();
            CourierTypography.Apply(gameObject);
            actionButton.onClick.AddListener(()=>{if(parcel.State==ParcelState.Loaded)owner.Run.Unload(parcel);else owner.Run.LoadParcel(parcel);});
        }
        public bool LoadByDrop()
        {
            if(owner==null||!owner.Active||!owner.Run.LoadParcel(parcel))return false;
            if(dragVisual!=null)DropFeedbackOrigin=dragVisual.transform.position;
            return true;
        }
        public void Refresh()
        {
            var run=owner.Run;bool loaded=parcel.State==ParcelState.Loaded;
            timeText.rectTransform.sizeDelta=new Vector2(108,48);
            ((RectTransform)timeGroup.transform).sizeDelta=new Vector2(132,48);
            nameText.resizeTextForBestFit=false;nameText.fontSize=28;nameText.lineSpacing=1.08f;nameText.alignment=TextAnchor.UpperLeft;
            destinationText.resizeTextForBestFit=false;destinationText.fontSize=26;destinationText.lineSpacing=1.12f;destinationText.alignment=TextAnchor.UpperLeft;
            nameText.text=string.Join(" + ",run.Batch(parcel).Select(p=>p.Name));
            destinationText.text=run.Stops[parcel.Origin].Name+" > "+run.Stops[parcel.Destination].Name;weightText.gameObject.SetActive(false);
            var oldWeight=transform.Find("WeightIcon");if(oldWeight!=null)oldWeight.gameObject.SetActive(false);
            var batch=run.Batch(parcel).ToArray();
            storyIcon.gameObject.SetActive(parcel.Critical);damagedIcon.gameObject.SetActive(batch.Any(p=>p.Damaged));
            fragileIcon.gameObject.SetActive(batch.Any(p=>p.Fragile)&&!batch.Any(p=>p.Damaged));heavyIcon.gameObject.SetActive(false);dryIcon.gameObject.SetActive(batch.Any(p=>p.Dry)&&!batch.Any(p=>p.Damaged));
            timeGroup.SetActive(true);
            var tagsRoot=(RectTransform)timeGroup.transform.parent;
            var heat=CourierSymbolUI.Icon(tagsRoot,"HeatSymbol",CourierIconKind.Heat,0,0,28,CourierColors.Night,"Keep cool · heatproof vehicle H");heat.gameObject.SetActive(batch.Any(p=>p.HeatSensitive)&&!batch.Any(p=>p.Damaged));heat.transform.SetSiblingIndex(0);
            CourierSymbolUI.Hint(cargoArtwork.transform,CourierInformation.Order(run,parcel));cargoArtwork.raycastTarget=true;
            CourierSymbolUI.Hint(nameText.transform,CourierInformation.Order(run,parcel));nameText.raycastTarget=true;
            CourierSymbolUI.Hint(damagedIcon.transform,"Damaged · delivery failed, but still accepted. Damage cannot be repaired.");
            RefreshArtwork();
            float width=((RectTransform)transform).rect.width;
            float textWidth=Mathf.Max(100,width-232);
            nameText.rectTransform.sizeDelta=new Vector2(textWidth,1000);
            float nameHeight=Mathf.Max(36,nameText.preferredHeight);
            nameText.rectTransform.anchoredPosition=new Vector2(128,-16);
            nameText.rectTransform.sizeDelta=new Vector2(textWidth,nameHeight);
            destinationText.rectTransform.sizeDelta=new Vector2(textWidth,1000);
            float destinationHeight=Mathf.Max(32,destinationText.preferredHeight);
            destinationText.rectTransform.anchoredPosition=new Vector2(128,-(28+nameHeight));
            destinationText.rectTransform.sizeDelta=new Vector2(textWidth,destinationHeight);
            var tags=timeGroup.transform.parent as RectTransform;
            tags.anchoredPosition=new Vector2(128,-(40+nameHeight+destinationHeight));
            tags.sizeDelta=new Vector2(Mathf.Max(180,width-144),48);
            float height=Mathf.Max(184,108+nameHeight+destinationHeight);
            var layout=GetComponent<LayoutElement>();
            if(layout.preferredHeight!=height){layout.preferredHeight=height;layout.minHeight=height;}
            ((RectTransform)actionButton.transform).anchoredPosition=new Vector2(-64,-(height-56)/2);
            var tagLayout=tags.GetComponent<HorizontalLayoutGroup>();
            if(tagLayout!=null){tagLayout.childAlignment=TextAnchor.MiddleLeft;tagLayout.spacing=16;}
            timeText.rectTransform.anchoredPosition=new Vector2(44,0);
            timeText.fontSize=26;timeText.alignment=TextAnchor.MiddleLeft;
            foreach(var icon in tags.GetComponentsInChildren<CourierIconGraphic>(true))
            {
                var rect=icon.rectTransform;rect.sizeDelta=Vector2.one*28;
                rect.anchoredPosition=new Vector2(rect.anchoredPosition.x,-10);
            }
            int target=run.TargetTurns(parcel);
            timeText.text=(loaded?run.OrderTurns:0)+"/"+target;
            timeHint.message="Order turns / target: "+timeText.text+". Late or damaged cargo is always accepted, rated Delivery failed.";
            actionButton.gameObject.SetActive(!loaded);actionButton.interactable=run.CanLoad(parcel);
            actionIcon.SetKind(CourierIconKind.Load);
            actionHint.message=loaded?"Complete this order before accepting another":run.State!=ShiftState.Docked?"Accept orders after arrival":run.ActiveOrder!=null?"Deliver the active order first":parcel.Origin!=run.Node?"Collect this batch at "+run.Stops[parcel.Origin].Name:"Accept the whole batch. Deliver to "+run.Stops[parcel.Destination].Name+". Click or drag into On Board.";
        }
        public void OnBeginDrag(PointerEventData data)
        {
            if(owner==null||!owner.Active||!owner.Run.CanLoad(parcel))return;
            var canvas=GetComponentInParent<Canvas>();
            dragVisual=new GameObject("DraggedParcel",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(CanvasGroup));
            dragVisual.transform.SetParent(canvas.rootCanvas.transform,false);dragVisual.transform.SetAsLastSibling();
            var rt=dragVisual.GetComponent<RectTransform>();rt.sizeDelta=new Vector2(280,60);
            dragVisual.GetComponent<Image>().color=CourierColors.Alpha(CourierColors.Mint,.9f);dragVisual.GetComponent<Image>().raycastTarget=false;
            dragVisual.GetComponent<CanvasGroup>().blocksRaycasts=false;
            var iconObject=new GameObject("CargoArtwork",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));iconObject.transform.SetParent(rt,false);
            var icon=iconObject.GetComponent<Image>();icon.sprite=cargoArtwork.sprite;icon.preserveAspect=true;icon.raycastTarget=false;
            var iconRect=icon.rectTransform;iconRect.anchorMin=iconRect.anchorMax=iconRect.pivot=new Vector2(0,.5f);iconRect.anchoredPosition=new Vector2(8,0);iconRect.sizeDelta=new Vector2(48,48);
            var label=new GameObject("Name",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));label.transform.SetParent(rt,false);
            var text=label.GetComponent<Text>();text.font=owner.UIFont;text.fontSize=23;text.text=parcel.Name;text.color=CourierColors.Night;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
            var labelRect=label.GetComponent<RectTransform>();labelRect.anchorMin=Vector2.zero;labelRect.anchorMax=Vector2.one;labelRect.offsetMin=new Vector2(64,0);labelRect.offsetMax=new Vector2(-8,0);
            OnDrag(data);
        }
        public void OnDrag(PointerEventData data)
        {
            if(dragVisual==null)return;Vector2 local;var parent=(RectTransform)dragVisual.transform.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,data.position,data.pressEventCamera,out local);dragVisual.GetComponent<RectTransform>().anchoredPosition=local;
        }
        public void OnEndDrag(PointerEventData data){if(dragVisual!=null)Destroy(dragVisual);}
        private void OnDisable(){if(dragVisual!=null)Destroy(dragVisual);}
    }
}
