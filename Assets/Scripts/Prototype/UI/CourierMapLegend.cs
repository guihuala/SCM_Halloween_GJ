using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NightCourier
{
    // A page overlay: stays readable while the street map pans and zooms.
    public sealed class CourierMapLegend : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler, IPointerClickHandler, ISubmitHandler
    {
        private CourierRoutePanel panel;
        private RectTransform card;
        private Button trigger;
        private CanvasGroup visibility;
        private bool hovered, focused, pinned, dismissed;
        private float alpha;
        private CourierMapLegendEntry activeEntry;
        public const int Junctions=-2,DelayEvents=-3,RetreatEvents=-4;
        public bool IsExpanded => card!=null&&card.gameObject.activeSelf&&visibility.blocksRaycasts;
        public bool HighlightsNode(int id)
        {
            if(!IsExpanded||panel==null||!panel.CanNavigateMap||activeEntry==null)return false;
            int target=activeEntry.target;
            if(target>=0)return target==id;
            var node=System.Array.Find(CourierStreetMap.Nodes,n=>n.StopId==id);
            if(node==null)return false;
            if(target==Junctions)return id>=9&&node.Id!="S";
            var owner=panel.mapGraphic.Owner;if(owner==null)return false;
            var events=target==DelayEvents?owner.Run.DelayEventNodes:owner.Run.RetreatEventNodes;
            return System.Array.IndexOf(events,node.Id)>=0;
        }
        public void PreviewEntry(CourierMapLegendEntry entry)
        {
            if(activeEntry==entry)return;
            if(activeEntry!=null)activeEntry.SetHighlighted(false);
            activeEntry=entry;
            if(activeEntry!=null)activeEntry.SetHighlighted(true);
            if(panel!=null&&panel.mapArtwork!=null&&panel.mapArtwork.travelOverlay!=null)panel.mapArtwork.travelOverlay.SetVerticesDirty();
        }
        public void ClearEntry(CourierMapLegendEntry entry){if(activeEntry==entry)PreviewEntry(null);}

        public static CourierMapLegend Create(RectTransform parent,CourierRoutePanel owner,Font font)
        {
            var root=new GameObject("MapLegend",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button),typeof(CourierMapLegend));
            root.transform.SetParent(parent,false);
            var view=root.GetComponent<CourierMapLegend>();view.panel=owner;
            var background=root.GetComponent<Image>();background.sprite=Resources.Load<Sprite>("NightVoyage/HUD/Button");
            background.type=Image.Type.Sliced;background.color=CourierColors.Raised;
            view.trigger=root.GetComponent<Button>();view.trigger.targetGraphic=background;
            var colors=view.trigger.colors;colors.highlightedColor=colors.selectedColor=CourierColors.Mint;view.trigger.colors=colors;
            CourierSymbolUI.Icon(root.transform,"LegendSymbol",CourierIconKind.Pin,16,14,32,CourierColors.Mint);
            Label(root.transform,"Label","Map legend",font,60,0,148,60,24,CourierColors.Paper);

            view.card=new GameObject("LegendCard",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(CanvasGroup)).GetComponent<RectTransform>();
            view.card.SetParent(root.transform,false);
            var paper=view.card.GetComponent<Image>();paper.sprite=Resources.Load<Sprite>("NightVoyage/HUD/Panel");
            paper.type=Image.Type.Sliced;paper.color=CourierColors.Paper;
            view.visibility=view.card.GetComponent<CanvasGroup>();
            Label(view.card,"Title","MAP LEGEND",font,24,16,352,38,28,CourierColors.Night);
            Label(view.card,"DeliveryHeading","Delivery stops · collect / deliver / talk",font,24,56,352,30,18,CourierColors.Night);
            var catalog=owner.mapArtwork!=null?owner.mapArtwork.catalog:null;
            if(catalog==null)catalog=Resources.Load<CourierMapArtCatalog>("NightVoyage/MapArtwork");
            var stops=new CourierRun().Stops;
            for(int i=0;i<6;i++)
            {
                float y=92+i*42;
                var sprite=catalog!=null?catalog.ForStop(i)?.sprite:null;
                if(sprite!=null)
                {
                    var art=new GameObject("Landmark_"+i,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();
                    art.transform.SetParent(view.card,false);Place(art.rectTransform,24,y,38,38);
                    art.sprite=sprite;art.preserveAspect=true;art.raycastTarget=false;
                }
                else CourierSymbolUI.Icon(view.card,"Landmark_"+i,CourierIconKind.Pin,24,y,34,CourierColors.Night);
                Label(view.card,"Stop_"+i,stops[i].Name,font,76,y,300,38,22,CourierColors.Night);
                view.Entry("StopEntry_"+i,i,y,42,"Landmark_"+i,"Stop_"+i);
            }
            Label(view.card,"OtherHeading","Facilities & junctions",font,24,350,352,30,18,CourierColors.Night);
            for(int i=0;i<3;i++)
            {
                float y=390+i*42;
                var vehicle=CourierVehicleIconView.Ensure(view.card,"Station_"+i,new Vector2(24,-y),new Vector2(38,38));vehicle.SetVehicle(i);
                Label(view.card,"StationLabel_"+i,(i==0?"H · Heat shield":i==1?"W · Waterproof":"Q · Suspension")+" station",font,76,y,300,38,22,CourierColors.Night);
                view.Entry("StationEntry_"+i,6+i,y,42,"Station_"+i,"StationLabel_"+i);
            }
            // Both depot and ordinary junctions are drawn as dots with letter/number labels.
            CourierSymbolUI.Icon(view.card,"Depot",CourierIconKind.Dot,24,516,34,CourierColors.Night);
            Label(view.card,"DepotLabel","S · Parcel depot",font,76,516,300,38,22,CourierColors.Night);
            view.Entry("DepotEntry",CourierStreetMap.Node("S").StopId,516,42,"Depot","DepotLabel");
            CourierSymbolUI.Icon(view.card,"Junction",CourierIconKind.Dot,24,558,34,CourierColors.Night);
            Label(view.card,"JunctionLabel","1a, 1b… · Road junction",font,76,558,300,38,22,CourierColors.Night);
            view.Entry("JunctionEntry",Junctions,558,42,"Junction","JunctionLabel");
            Label(view.card,"DelayLabel","+1  Delay event",font,24,606,170,32,18,CourierColors.Night);
            Label(view.card,"RetreatLabel","<  Retreat event",font,208,606,168,32,18,CourierColors.Night);
            view.Entry("DelayEntry",DelayEvents,606,32,"DelayLabel");
            view.Entry("RetreatEntry",RetreatEvents,606,32,"RetreatLabel");
            var delay=(RectTransform)view.card.Find("DelayEntry");delay.sizeDelta=new Vector2(180,32);
            var retreat=(RectTransform)view.card.Find("RetreatEntry");retreat.anchoredPosition=new Vector2(200,-606);retreat.sizeDelta=new Vector2(184,32);
            ((RectTransform)retreat.Find("RetreatLabel")).anchoredPosition=new Vector2(8,0);
            view.card.gameObject.SetActive(false);view.visibility.alpha=0;
            return view;
        }

        public void ApplyLayout(Vector2 size)
        {
            var rect=(RectTransform)transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,0);
            // Bottom-right page overlay; the card opens upward and meets the hover target.
            rect.anchoredPosition=new Vector2(-32,32);rect.sizeDelta=new Vector2(220,60);
            card.anchorMin=card.anchorMax=Vector2.one;card.pivot=new Vector2(1,0);
            card.anchoredPosition=Vector2.zero;card.sizeDelta=new Vector2(400,654);
            float scale=Mathf.Min(1,(size.y-124)/654f,(size.x-64)/400f);
            card.localScale=Vector3.one*Mathf.Max(.1f,scale);
        }
        private void Update()
        {
            bool available=panel!=null&&panel.CanNavigateMap;
            trigger.interactable=available;
            if(!available)hovered=focused=pinned=false;
            if(Input.GetKeyDown(KeyCode.Escape)){pinned=focused=false;dismissed=true;}
            bool open=available&&!dismissed&&(hovered||focused||pinned);
            if(!open)PreviewEntry(null);
            if(panel!=null&&panel.mapArtwork!=null&&panel.mapArtwork.travelOverlay!=null)panel.mapArtwork.travelOverlay.Legend=this;
            alpha=panel!=null&&panel.MapReducedMotion?(open?1:0):Mathf.MoveTowards(alpha,open?1:0,Time.unscaledDeltaTime/(open?.14f:.10f));
            visibility.alpha=alpha;visibility.blocksRaycasts=open;visibility.interactable=open;
            card.gameObject.SetActive(alpha>0);
        }
        public void OnPointerEnter(PointerEventData data){hovered=true;dismissed=false;}
        public void OnPointerExit(PointerEventData data){hovered=false;}
        public void OnSelect(BaseEventData data)
        {focused=data is AxisEventData||Input.GetKey(KeyCode.Tab);dismissed=false;}
        public void OnDeselect(BaseEventData data){focused=pinned=false;}
        public void OnPointerClick(PointerEventData data)
        {
            if(data.button!=PointerEventData.InputButton.Left||!trigger.interactable||
                !RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform,data.position,data.pressEventCamera))return;
            pinned=!pinned;dismissed=!pinned;
        }
        public void OnSubmit(BaseEventData data){if(trigger.interactable){pinned=!pinned;dismissed=!pinned;}}
        private void OnDisable()
        {PreviewEntry(null);hovered=focused=pinned=dismissed=false;alpha=0;if(card!=null){card.gameObject.SetActive(false);visibility.alpha=0;visibility.blocksRaycasts=false;}}
        private void Entry(string name,int target,float y,float height,params string[] children)
        {
            var entry=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(CourierMapLegendEntry)).GetComponent<CourierMapLegendEntry>();
            entry.transform.SetParent(card,false);Place((RectTransform)entry.transform,16,y,368,height);
            entry.legend=this;entry.target=target;entry.SetHighlighted(false);
            foreach(string childName in children)
            {
                var child=card.Find(childName) as RectTransform;
                var position=child.anchoredPosition;child.SetParent(entry.transform,false);child.anchoredPosition=position-new Vector2(16,-y);
            }
        }
        private static Text Label(Transform parent,string name,string value,Font font,float x,float y,float width,float height,int size,Color color)
        {
            var label=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(parent,false);Place(label.rectTransform,x,y,width,height);
            label.font=font;label.text=value;label.fontSize=size;label.color=color;label.alignment=TextAnchor.MiddleLeft;label.raycastTarget=false;
            return label;
        }
        private static void Place(RectTransform rect,float x,float y,float width,float height)
        {rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);}
    }
}
