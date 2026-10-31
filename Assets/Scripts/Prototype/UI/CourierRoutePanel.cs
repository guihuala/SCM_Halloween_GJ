using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace NightCourier
{
    public sealed class CourierRoutePanel : CourierPanel
    {
        public Button[] stopButtons;
        public Text[] stopLabels,stopCounts;
        public CourierIconGraphic[] stopIcons;
        public CourierIconHint[] stopHints;
        public CourierRouteGraphic mapGraphic;
        public CourierMapArtwork mapArtwork;
        public void PrepareMapArtwork()
        {
            var container=mapGraphic.transform.parent;
            if(mapArtwork==null)mapArtwork=container.GetComponent<CourierMapArtwork>()??container.gameObject.AddComponent<CourierMapArtwork>();
        }
        public Text objectiveText,destinationText,timeCostText,wearCostText,riskText,vehicleText,durabilityText,loadText;
        public Image durabilityFill;
        public CourierVehicleIconView vehicleArtwork, parkedVehicleArtwork;
        public void PrepareVehicleArtwork()
        {
            vehicleArtwork=CourierVehicleIconView.Ensure(vehicleText.transform.parent,"CurrentVehicleArtwork",new Vector2(32,-14),new Vector2(68,68));
            parkedVehicleArtwork=CourierVehicleIconView.Ensure(possessButton.transform,"ParkedVehicleArtwork",new Vector2(10,-6),new Vector2(40,40));
            parkedVehicleArtwork.SetVehicle(1);possessIcon.gameObject.SetActive(false);
            vehicleText.rectTransform.anchoredPosition=new Vector2(112,-24);vehicleText.rectTransform.sizeDelta=new Vector2(140,44);
        }
        public Button departButton,possessButton,repairButton;
        public CourierIconGraphic possessIcon;
        public CourierIconHint departHint,possessHint;
        public GameObject costGroup;
        public RectTransform waitingSection,loadedSection,loadedScroll;
        public Transform waitingContent,loadedContent;
        public CourierCargoRow rowPrefab;
        public CourierTrunkDropZone dropZone;
        [Header("Map Page Presentation")]
        public Text pageTitle,mapHeading,cargoHeading;
        public RectTransform cargoDrawer;
        public Button cargoToggleButton,cargoCloseButton;
        public Text cargoToggleText,waitingEmptyText,loadedEmptyText;
        public Button nodePromptButton;
        public Text nodePromptText;
        public bool CargoOpen { get; private set; }
        private bool tutorialCargoLoaded;
        public void SetCargoOpen(bool open)
        {
            CargoOpen=open;
            if(cargoDrawer!=null)cargoDrawer.gameObject.SetActive(open);
        }
        public void ToggleCargoDrawer(){if(owner!=null&&owner.Active)SetCargoOpen(!CargoOpen);}
        private readonly List<CourierCargoRow> rows=new List<CourierCargoRow>();
        private int selected=-1;
        private List<Road> route=new List<Road>();
        private RectTransform page,map;
        private Vector2 pageSize;
        private float layoutBottom;
        private readonly Vector3[] dialogueCorners=new Vector3[4];
        private float sidebarX=756,contentBottom=620;
        public void ApplyFullscreenLayout()
        {
            if(page==null)page=transform.Find("Window") as RectTransform;
            if(page==null)return;
            page.anchorMin=Vector2.zero;page.anchorMax=Vector2.one;page.offsetMin=page.offsetMax=Vector2.zero;
            if(map==null)map=mapGraphic.transform.parent as RectTransform;
            Vector2 size=page.rect.size;
            if(size.x<100||size.y<100)size=new Vector2(1920,1080);
            sidebarX=32;
            contentBottom=size.y-24;
            if(owner!=null)
            {
                var hud=UIManager.Instance.GetPanel("CourierHudPanel") as CourierHudPanel;
                var bar=hud!=null?hud.subtitleText.transform.parent.Find("SubtitleBar") as RectTransform:null;
                if(bar!=null&&owner.ConversationActive&&Run.State==ShiftState.Docked){bar.GetWorldCorners(dialogueCorners);contentBottom=page.rect.yMax-page.InverseTransformPoint(dialogueCorners[1]).y-16;}
            }
            float mapX=32,mapWidth=size.x-64,mapHeight=Mathf.Max(260,contentBottom-344);
            if(pageSize!=size||Mathf.Abs(layoutBottom-contentBottom)>.5f)
            {
                pageSize=size;layoutBottom=contentBottom;
                if(pageTitle!=null)pageTitle.gameObject.SetActive(false);
                if(mapHeading!=null)Place(mapHeading.rectTransform,32,184,mapWidth,32);
                Place(map,mapX,224,mapWidth,mapHeight);
                var edges=mapGraphic.rectTransform;edges.anchorMin=Vector2.zero;edges.anchorMax=Vector2.one;edges.offsetMin=edges.offsetMax=Vector2.zero;
                Place(destinationText.rectTransform,32,contentBottom-100,Mathf.Max(360,size.x*.44f),40);
                Place((RectTransform)costGroup.transform,size.x*.5f,contentBottom-100,580,42);
                Place(riskText.rectTransform,32,contentBottom-48,size.x-168,44);
                Place((RectTransform)departButton.transform,size.x-96,contentBottom-100,64,64);
                Place(vehicleText.rectTransform,112,24,140,44);
                Move("DurabilityIcon",260,28);Move("DurabilityFillBackground",308,44);Move("DurabilityFill",308,44);
                Place(durabilityText.rectTransform,444,28,120,36);
                Move("LoadIcon",580,28);Place(loadText.rectTransform,628,28,172,36);
                Move("PossessButton",32,84);Move("RepairButton",304,84);
                if(cargoToggleButton!=null)Place((RectTransform)cargoToggleButton.transform,size.x-660,24,220,56);
                LayoutDecoration("PageHeaderRule",32,144,size.x-64,2);
                LayoutDecoration("SummaryRule",32,contentBottom-116,mapWidth,2);
                if(cargoDrawer!=null)
                {
                    float height=Mathf.Min(360,contentBottom-244),column=(size.x-136)*.5f;
                    Place(cargoDrawer,32,contentBottom-height,size.x-64,height);
                    if(cargoHeading!=null)Place(cargoHeading.rectTransform,24,12,size.x-320,36);
                    if(cargoCloseButton!=null)Place((RectTransform)cargoCloseButton.transform,size.x-280,12,192,48);
                    Place(waitingSection,24,64,column,height-80);
                    Place(loadedSection,48+column,64,column,height-80);
                    var waitingScroll=waitingContent.parent.parent as RectTransform;
                    Place(waitingScroll,0,38,column,height-118);
                    Place(loadedScroll,0,38,column,height-118);
                    foreach(var section in new[]{waitingSection,loadedSection})
                    {
                        var title=section.GetComponentInChildren<Text>(true);if(title!=null)title.rectTransform.sizeDelta=new Vector2(column,30);
                    }
                    if(waitingEmptyText!=null)Place(waitingEmptyText.rectTransform,0,54,column,48);
                    if(loadedEmptyText!=null)Place(loadedEmptyText.rectTransform,0,54,column,48);
                }
            }
            var stops=owner!=null?Run.Stops:new CourierRun().Stops;
            for(int i=0;i<stopButtons.Length;i++)
                {
                    var point=CourierRouteGraphic.NodePoint(stops[i],new Vector2(mapWidth,mapHeight));
                    ((RectTransform)stopButtons[i].transform).anchoredPosition=new Vector2(point.x-90,-point.y+36);
                }
            if(mapArtwork==null)PrepareMapArtwork();
            if(mapArtwork.isActiveAndEnabled)mapArtwork.Apply(this,stops);
            if(owner!=null)RefreshNodePrompt();
        }
        private void Move(string name,float x,float y)
        {var rect=page.Find(name) as RectTransform;if(rect!=null)rect.anchoredPosition=new Vector2(x,-y);}
        private void LayoutDecoration(string name,float x,float y,float w,float h)
        {var rect=page.Find(name) as RectTransform;if(rect!=null)Place(rect,x,y,w,h);}
        private static void Place(RectTransform rect,float x,float y,float w,float h)
        {rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);}
        private void RefreshNodePrompt()
        {
            if(nodePromptButton==null||nodePromptText==null)return;
            bool docked=Run.State==ShiftState.Docked;
            bool moving=Run.State==ShiftState.Driving||Run.State==ShiftState.Pushing;
            bool visible=!Run.Finished&&(moving||(docked&&!owner.ConversationActive));
            nodePromptButton.gameObject.SetActive(visible);
            if(!visible)return;
            int node=moving&&Run.ActiveRoad!=null?Run.TravelDestination:Run.Node;
            nodePromptText.text=docked?(Run.HasJourney?"Continue to "+Run.Stops[Run.PlannedDestination].Name+"  [E]":"Talk  [E]")
                :Run.State==ShiftState.Pushing?"Pushing to this stop":"Heading to this stop";
            nodePromptButton.interactable=docked&&owner.Active;
            float width=docked&&Run.HasJourney?360:240,height=docked&&Run.HasJourney?68:48;
            stopLabels[node].rectTransform.GetWorldCorners(dialogueCorners);
            var lower=map.InverseTransformPoint(dialogueCorners[0]);var upper=map.InverseTransformPoint(dialogueCorners[2]);
            var point=CourierRouteGraphic.NodePoint(Run.Stops[node],map.rect.size);
            float labelTop=map.rect.yMax-upper.y,labelBottom=map.rect.yMax-lower.y;
            float top=labelBottom<point.y?labelTop-height-8:labelBottom+8;
            var placement=new Rect(Mathf.Clamp(point.x-width*.5f,0,map.rect.width-width),Mathf.Clamp(top,0,map.rect.height-height),width,height);
            for(int candidate=0;candidate<4;candidate++)
            {
                Rect option;
                if(candidate==0)option=new Rect(point.x-width*.5f,top,width,height);
                else if(candidate==1)option=new Rect(point.x-126-width,labelTop+(labelBottom-labelTop-height)*.5f,width,height);
                else if(candidate==2)option=new Rect(point.x+126,labelTop+(labelBottom-labelTop-height)*.5f,width,height);
                else option=new Rect(point.x-width*.5f,labelTop-height-8,width,height);
                if(option.xMin<0||option.yMin<0||option.xMax>map.rect.width||option.yMax>map.rect.height)continue;
                bool overlaps=false;
                foreach(var label in stopLabels)
                {
                    label.rectTransform.GetWorldCorners(dialogueCorners);
                    var lo=map.InverseTransformPoint(dialogueCorners[0]);var hi=map.InverseTransformPoint(dialogueCorners[2]);
                    var labelBounds=Rect.MinMaxRect(lo.x-map.rect.xMin,map.rect.yMax-hi.y,hi.x-map.rect.xMin,map.rect.yMax-lo.y);
                    if(option.Overlaps(labelBounds)){overlaps=true;break;}
                }
                if(!overlaps){placement=option;break;}
            }
            Place((RectTransform)nodePromptButton.transform,placement.x,placement.y,width,height);
        }
        protected override void Awake()
        {
            base.Awake();PrepareVehicleArtwork();
            if(cargoToggleButton!=null)cargoToggleButton.onClick.AddListener(ToggleCargoDrawer);
            if(cargoCloseButton!=null)cargoCloseButton.onClick.AddListener(()=>SetCargoOpen(false));
            if(nodePromptButton!=null)nodePromptButton.onClick.AddListener(()=>{if(owner==null||!owner.Active||Run.State!=ShiftState.Docked)return;if(Run.HasJourney)owner.AdvanceDialogue();else owner.BeginConversation();});
            for(int i=0;i<stopButtons.Length;i++){int node=i;stopButtons[i].onClick.AddListener(()=>Select(node));}
            departButton.onClick.AddListener(()=>{if(selected>=0)owner.DepartTo(selected);});
            possessButton.onClick.AddListener(()=>owner.PossessParkedVehicle());repairButton.onClick.AddListener(()=>Run.Repair());
        }
        public override void Bind(NightCourierPrototype prototype)
        {
            mapGraphic.Owner=prototype;dropZone.Owner=prototype;SetCargoOpen(false);
            foreach(var row in rows)Destroy(row.gameObject);rows.Clear();
            foreach(var p in prototype.Run.Parcels){var row=Instantiate(rowPrefab,waitingContent);row.Bind(prototype,p);rows.Add(row);}
            selected=prototype.Run.HasJourney?prototype.Run.PlannedDestination:-1;
            base.Bind(prototype);
        }
        private void Select(int node){if(Run.State!=ShiftState.Docked)return;if(node==Run.Node){owner.BeginConversation();return;}selected=node;if(Run.DestinationBlockReason(node)=="")owner.Tutorial.ChooseRoute();Refresh();}
        protected override void Refresh()
        {
            ApplyFullscreenLayout();
            bool docked=Run.State==ShiftState.Docked;
            if(objectiveText!=null)objectiveText.text=Run.HasJourney?"Waypoint stop - Destination: "+Run.Stops[Run.PlannedDestination].Name:Run.ObjectiveText;
            route=Run.FindRoute(selected);mapGraphic.SelectedRoute=route;mapGraphic.SetVerticesDirty();
            for(int i=0;i<stopButtons.Length;i++)
            {
                var parcels=Run.Parcels.Where(p=>p.State==ParcelState.Loaded&&p.Destination==i).ToArray();
                var pending=Run.Parcels.Where(p=>p.State==ParcelState.Waiting&&p.Origin==i).GroupBy(p=>p.Destination).ToArray();
                bool current=i==Run.Node,locked=false;
                stopButtons[i].interactable=docked;
                stopLabels[i].text=Run.Stops[i].Name;stopCounts[i].text=parcels.Length>0?"Deliver":pending.Length>0?pending.Length+" orders":"";
                stopIcons[i].SetKind(locked?CourierIconKind.Lock:parcels.Length>0?CourierIconKind.Box:current&&docked?CourierIconKind.Pin:CourierIconKind.Dot);
                Color c=current?new Color(1,.66f,.34f):parcels.Length>0?new Color(.45f,.94f,.77f):new Color(.68f,.74f,.76f);
                stopIcons[i].color=stopLabels[i].color=stopCounts[i].color=c;
                stopHints[i].message=Run.Stops[i].Name+(current?" - You are here":"")+(locked?" - Story locked":"")+
                    (parcels.Length>0?"\nDeliveries: "+string.Join(", ",parcels.Select(p=>p.Name)):"")+string.Concat(pending.Select(g=>"\nPick up: "+string.Join(" + ",g.Select(p=>p.Name))+" > "+Run.Stops[g.Key].Name));
            }
            if(vehicleArtwork==null||parkedVehicleArtwork==null)PrepareVehicleArtwork();
            vehicleArtwork.SetVehicle(Run.VehicleIndex);
            vehicleText.text=Run.Car.Name;durabilityText.text=Run.IsGhost?"No limit":Mathf.CeilToInt(Run.Durability)+" turns";loadText.text=Run.ActiveOrder==null?"No active order":"Order "+Run.OrderTurns+"/"+Run.TargetTurns(Run.ActiveOrder);
            loadText.color=Run.Overloaded?new Color(1,.66f,.34f):new Color(.92f,.95f,.93f);
            durabilityFill.fillAmount=Run.IsGhost?1:Run.Durability/Run.Car.MaxDurability;
            int v=Run.Stops[Run.Node].Vehicle;bool parked=docked&&v>=0&&v!=Run.VehicleIndex;
            possessButton.gameObject.SetActive(parked);
            parkedVehicleArtwork.gameObject.SetActive(parked&&Run.UnlockedVehicles[v]);
            possessIcon.gameObject.SetActive(parked&&!Run.UnlockedVehicles[v]);
            if(parked)parkedVehicleArtwork.SetVehicle(v);
            if(parked){possessButton.interactable=Run.CanPossess(v);Caption(possessButton,Run.Vehicles[v].Name);possessIcon.SetKind(Run.UnlockedVehicles[v]?CourierIconKind.Ghost:CourierIconKind.Lock);possessHint.message=!Run.UnlockedVehicles[v]?"Old pickup locked\nDeliver the Cooling Unit from Settlement Bar to the vampire DJ.":Run.CanPossess(v)?"Possess "+Run.Vehicles[v].Name:"Vehicle unavailable at this stop.";}
            bool repair=docked&&!Run.IsGhost&&Run.Stops[Run.Node].Repair&&Run.Durability<Run.Car.MaxDurability;repairButton.gameObject.SetActive(repair);repairButton.interactable=Run.Durability<Run.Car.MaxDurability;
            int waiting=0,loadedCount=0;
            for(int i=0;i<rows.Count;i++)
            {
                var p=Run.Parcels[i];bool show=(p.State==ParcelState.Loaded||p.State==ParcelState.Waiting)&&Run.Batch(p).First()==p;
                rows[i].gameObject.SetActive(show);
                if(show){var parent=p.State==ParcelState.Loaded?loadedContent:waitingContent;if(rows[i].transform.parent!=parent)rows[i].transform.SetParent(parent,false);rows[i].Refresh();if(p.State==ParcelState.Waiting)waiting++;else loadedCount++;}
            }
            waitingSection.gameObject.SetActive(true);
            if(waitingEmptyText!=null)waitingEmptyText.gameObject.SetActive(waiting==0);
            if(loadedEmptyText!=null)loadedEmptyText.gameObject.SetActive(loadedCount==0);
            if(cargoToggleText!=null)cargoToggleText.text="Orders "+Run.CompletedBatches+"/"+Run.BatchCount+"  [Tab]";
            if(cargoToggleButton!=null)cargoToggleButton.interactable=owner.Active&&!Run.Finished;
            if(!owner.Tutorial.Finished&&owner.Tutorial.CargoLoaded&&!tutorialCargoLoaded)SetCargoOpen(false);
            tutorialCargoLoaded=owner.Tutorial.CargoLoaded;
            RefreshNodePrompt();
            bool controls=parked||repair;
            if(objectiveText!=null)Place(objectiveText.rectTransform,controls?568:32,84,pageSize.x-(controls?600:64),48);
            departButton.gameObject.SetActive(docked);
            if(!docked&&Run.ActiveRoad!=null)
            {
                if(objectiveText!=null)objectiveText.text=(Run.State==ShiftState.Encounter?"Road event":Run.IsReturning?"Returning to previous stop":Run.State==ShiftState.Pushing?"Pushing to next stop":"In transit")+" - "+Mathf.RoundToInt(Run.Progress*100)+"%";
                destinationText.text="Next: "+Run.Stops[Run.TravelDestination].Name;
                costGroup.SetActive(true);timeCostText.text=Run.RemainingTravelTurns+" turns";
                wearCostText.text="-"+Mathf.Min(Mathf.CeilToInt(Run.Durability),Run.RemainingTravelTurns);riskText.text=(Run.IsGhost?"Ghost transport - ":"")+"F: skip animation to the next event or stop";return;
            }
            costGroup.SetActive(route.Count>0);
            if(selected<0||selected==Run.Node){destinationText.text="Choose a destination";riskText.text=Run.Results.Count>0?"Last order: "+Run.Results.Last().Turns+"/"+Run.Results.Last().TargetTurns+" turns - "+Run.Results.Last().Evaluation:"";departButton.interactable=false;departHint.message="Depart\nSelect a stop on the map first.";return;}
            destinationText.text=Run.Stops[selected].Name;
            timeCostText.text=Mathf.CeilToInt(route.Sum(r=>Run.TimeCost(r)))+" turns";wearCostText.text="-"+Mathf.CeilToInt(route.Sum(r=>Run.WearCost(r)));
            string blocked=Run.DestinationBlockReason(selected);
            var warnings=new List<string>();
            if(route.Any(r=>r.Kind==RoadKind.Storm))warnings.Add("Storm: severe weather");
            if(route.Any(r=>r.Gravel)&&Run.VehicleIndex!=2)warnings.Add("Gravel damages fragile cargo");
            if(route.Any(r=>r.Kind==RoadKind.River))warnings.Add("Keep cargo dry at the crossing");
            riskText.text=blocked!=""?blocked:string.Join(" · ",warnings);
            int at=Run.Node;var stops=new List<string>();foreach(var road in route){at=road.Other(at);stops.Add(Run.Stops[at].Name);}
            departButton.interactable=blocked==""&&!owner.ConversationActive;departHint.message=owner.ConversationActive?"Finish the conversation before departing.":blocked!=""?blocked:"Depart > "+string.Join(" > ",stops)+"\nFastest estimated route; events may add time. Stop at waypoints, then press E to continue.";
        }
    }
}
