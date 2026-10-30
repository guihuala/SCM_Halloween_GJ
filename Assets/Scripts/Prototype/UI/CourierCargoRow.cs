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
            nameText.text=parcel.Name;
            destinationText.text=run.Stops[parcel.Destination].Name;weightText.text=parcel.Weight.ToString();
            storyIcon.gameObject.SetActive(parcel.Critical);damagedIcon.gameObject.SetActive(parcel.Damaged);
            fragileIcon.gameObject.SetActive(parcel.Fragile&&!parcel.Damaged);heavyIcon.gameObject.SetActive(parcel.Heavy&&!parcel.Damaged);dryIcon.gameObject.SetActive(parcel.Dry&&!parcel.Damaged);
            timeGroup.SetActive(parcel.TimeLimit>0&&!parcel.Damaged);
            // Plain parcels need no reserved tag row.
            bool hasTags=parcel.Critical||parcel.Damaged||parcel.Fragile||parcel.Heavy||parcel.Dry||parcel.TimeLimit>0;
            var layout=GetComponent<LayoutElement>();float height=hasTags?132:92;
            if(layout.preferredHeight!=height){layout.preferredHeight=height;layout.minHeight=height;}
            ((RectTransform)actionButton.transform).anchoredPosition=new Vector2(-64,-(height-56)/2);
            float remaining=parcel.Deadline>0?parcel.Deadline-run.Elapsed:parcel.TimeLimit;
            timeText.text=Mathf.CeilToInt(Mathf.Max(0,remaining)).ToString();timeHint.message=parcel.Deadline>0?"Time left: "+timeText.text+" min":"Deliver within "+timeText.text+" min of loading";
            actionButton.gameObject.SetActive(!loaded||!parcel.Critical);actionButton.interactable=loaded?!parcel.Critical:run.CanLoad(parcel);
            actionIcon.SetKind(loaded?CourierIconKind.Store:CourierIconKind.Load);
            actionHint.message=loaded?"Store at this stop":parcel.Heavy&&run.VehicleIndex==0?"Switch vehicles to carry heavy cargo":run.CanLoad(parcel)?"Load into trunk":"Not enough cargo space";
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
