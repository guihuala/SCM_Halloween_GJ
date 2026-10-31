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
        private Parcel parcel;
        public Parcel Item {get{return parcel;}}
        private NightCourierPrototype owner;
        private GameObject dragVisual;
        public void Bind(NightCourierPrototype prototype,Parcel item)
        {
            owner=prototype;parcel=item;
            foreach(var t in GetComponentsInChildren<Text>(true))t.font=owner.UIFont;
            actionButton.onClick.AddListener(()=>{if(parcel.State==ParcelState.Loaded)owner.Run.Unload(parcel);else owner.Run.LoadParcel(parcel);});
        }
        public bool LoadByDrop(){return owner!=null&&owner.Active&&owner.Run.LoadParcel(parcel);}
        public void Refresh()
        {
            var run=owner.Run;bool loaded=parcel.State==ParcelState.Loaded;
            timeText.rectTransform.sizeDelta=new Vector2(96,38);
            ((RectTransform)timeGroup.transform).sizeDelta=new Vector2(144,32);
            nameText.resizeTextForBestFit=true;nameText.resizeTextMinSize=22;nameText.resizeTextMaxSize=32;
            destinationText.resizeTextForBestFit=true;destinationText.resizeTextMinSize=20;destinationText.resizeTextMaxSize=28;
            nameText.text=string.Join(" + ",run.Batch(parcel).Select(p=>p.Name));
            destinationText.text=run.Stops[parcel.Origin].Name+" > "+run.Stops[parcel.Destination].Name;weightText.gameObject.SetActive(false);
            var oldWeight=transform.Find("WeightIcon");if(oldWeight!=null)oldWeight.gameObject.SetActive(false);
            storyIcon.gameObject.SetActive(parcel.Critical);damagedIcon.gameObject.SetActive(parcel.Damaged);
            fragileIcon.gameObject.SetActive(parcel.Fragile&&!parcel.Damaged);heavyIcon.gameObject.SetActive(false);dryIcon.gameObject.SetActive(parcel.Dry&&!parcel.Damaged);
            timeGroup.SetActive(true);
            // Plain parcels need no reserved tag row.
            bool hasTags=parcel.Critical||parcel.Damaged||parcel.Fragile||parcel.Heavy||parcel.Dry||parcel.TimeLimit>0;
            var layout=GetComponent<LayoutElement>();float height=hasTags?132:92;
            float width=((RectTransform)transform).rect.width;
            if(width>300)
            {
                nameText.rectTransform.sizeDelta=new Vector2(width-100,nameText.rectTransform.sizeDelta.y);
                destinationText.rectTransform.sizeDelta=new Vector2(width-108,destinationText.rectTransform.sizeDelta.y);
                weightText.rectTransform.anchoredPosition=new Vector2(width-118,-44);
                var weightIcon=transform.Find("WeightIcon") as RectTransform;if(weightIcon!=null)weightIcon.anchoredPosition=new Vector2(width-154,-50);
            }
            if(layout.preferredHeight!=height){layout.preferredHeight=height;layout.minHeight=height;}
            ((RectTransform)actionButton.transform).anchoredPosition=new Vector2(-64,-(height-56)/2);
            int target=run.TargetTurns(parcel);
            timeText.text=(loaded?run.OrderTurns:0)+"/"+target;
            timeHint.message="Order turns / target: "+timeText.text+". Late or damaged cargo is always accepted, with a lower evaluation.";
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
            dragVisual.GetComponent<Image>().color=new Color(.45f,.94f,.77f,.9f);dragVisual.GetComponent<Image>().raycastTarget=false;
            dragVisual.GetComponent<CanvasGroup>().blocksRaycasts=false;
            var label=new GameObject("Name",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));label.transform.SetParent(rt,false);
            var text=label.GetComponent<Text>();text.font=owner.UIFont;text.fontSize=23;text.text=parcel.Name;text.color=Color.black;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
            var labelRect=label.GetComponent<RectTransform>();labelRect.anchorMin=Vector2.zero;labelRect.anchorMax=Vector2.one;labelRect.offsetMin=labelRect.offsetMax=Vector2.zero;
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
