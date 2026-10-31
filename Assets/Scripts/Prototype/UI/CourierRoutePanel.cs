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
        public Text objectiveText,destinationText,timeCostText,wearCostText,riskText,vehicleText,durabilityText,loadText;
        public Image durabilityFill;
        public Button departButton,possessButton,repairButton;
        public CourierIconGraphic possessIcon;
        public CourierIconHint departHint,possessHint;
        public GameObject costGroup;
        public RectTransform waitingSection,loadedSection,loadedScroll;
        public Transform waitingContent,loadedContent;
        public CourierCargoRow rowPrefab;
        public CourierTrunkDropZone dropZone;
        private readonly List<CourierCargoRow> rows=new List<CourierCargoRow>();
        private int selected=-1;
        private List<Road> route=new List<Road>();
        protected override void Awake()
        {
            base.Awake();
            for(int i=0;i<stopButtons.Length;i++){int node=i;stopButtons[i].onClick.AddListener(()=>Select(node));}
            departButton.onClick.AddListener(()=>{if(selected>=0)owner.DepartTo(selected);});
            possessButton.onClick.AddListener(()=>owner.PossessParkedVehicle());repairButton.onClick.AddListener(()=>Run.Repair());
        }
        public override void Bind(NightCourierPrototype prototype)
        {
            mapGraphic.Owner=prototype;dropZone.Owner=prototype;
            foreach(var row in rows)Destroy(row.gameObject);rows.Clear();
            foreach(var p in prototype.Run.Parcels){var row=Instantiate(rowPrefab,waitingContent);row.Bind(prototype,p);rows.Add(row);}
            selected=prototype.Run.HasJourney?prototype.Run.PlannedDestination:-1;
            base.Bind(prototype);
        }
        private void Select(int node){if(Run.State!=ShiftState.Docked)return;if(node==Run.Node){owner.BeginConversation();return;}selected=node;if(Run.DestinationBlockReason(node)=="")owner.Tutorial.ChooseRoute();Refresh();}
        protected override void Refresh()
        {
            bool docked=Run.State==ShiftState.Docked;
            objectiveText.text=Run.HasJourney?"Waypoint stop - Destination: "+Run.Stops[Run.PlannedDestination].Name:Run.ObjectiveText;
            route=Run.FindRoute(selected);mapGraphic.SelectedRoute=route;mapGraphic.SetVerticesDirty();
            for(int i=0;i<stopButtons.Length;i++)
            {
                var parcels=Run.Parcels.Where(p=>p.State==ParcelState.Loaded&&p.Destination==i).ToArray();
                bool current=i==Run.Node,locked=false;
                stopButtons[i].interactable=docked;
                stopLabels[i].text=Run.Stops[i].Name;stopCounts[i].text=parcels.Length>0?parcels.Length.ToString():"";
                stopIcons[i].SetKind(locked?CourierIconKind.Lock:parcels.Length>0?CourierIconKind.Box:current&&docked?CourierIconKind.Pin:CourierIconKind.Dot);
                Color c=current?new Color(1,.66f,.34f):parcels.Length>0?new Color(.45f,.94f,.77f):new Color(.68f,.74f,.76f);
                stopIcons[i].color=stopLabels[i].color=stopCounts[i].color=c;
                stopHints[i].message=Run.Stops[i].Name+(current?" - You are here":"")+(locked?" - Story locked":"")+
                    (parcels.Length>0?"\nDeliveries: "+string.Join(", ",parcels.Select(p=>p.Name)):"");
            }
            vehicleText.text=Run.Car.Name;durabilityText.text=Mathf.CeilToInt(Run.Durability)+"/"+Run.Car.MaxDurability;loadText.text=Run.Load+"/"+Run.Car.Capacity+(Run.Overloaded?" - Full":"");
            loadText.color=Run.Overloaded?new Color(1,.66f,.34f):new Color(.92f,.95f,.93f);
            durabilityFill.fillAmount=Run.Durability/Run.Car.MaxDurability;
            int v=Run.Stops[Run.Node].Vehicle;bool parked=docked&&v>=0&&v!=Run.VehicleIndex;
            possessButton.gameObject.SetActive(parked);
            if(parked){possessButton.interactable=Run.CanPossess(v);Caption(possessButton,Run.Vehicles[v].Name);possessIcon.SetKind(Run.UnlockedVehicles[v]?CourierIconKind.Ghost:CourierIconKind.Lock);possessHint.message=!Run.UnlockedVehicles[v]?"Old pickup locked\nDeliver Repair Equipment from Settlement Bar to the vampire DJ.":Run.CanPossess(v)?"Possess "+Run.Vehicles[v].Name:"Cannot switch vehicles\nStore cargo until the load is at most "+(Run.Vehicles[v].Capacity+2)+".";}
            bool repair=docked&&Run.Stops[Run.Node].Repair&&Run.Durability<Run.Car.MaxDurability;repairButton.gameObject.SetActive(repair);repairButton.interactable=Run.Durability<Run.Car.MaxDurability;
            int waiting=0;
            for(int i=0;i<rows.Count;i++)
            {
                var p=Run.Parcels[i];bool show=p.State==ParcelState.Loaded||(p.State==ParcelState.Waiting&&p.Origin==Run.Node&&(!p.Critical||Run.Talked[Run.Node]));
                rows[i].gameObject.SetActive(show);
                if(show){var parent=p.State==ParcelState.Loaded?loadedContent:waitingContent;if(rows[i].transform.parent!=parent)rows[i].transform.SetParent(parent,false);rows[i].Refresh();if(p.State==ParcelState.Waiting)waiting++;}
            }
            float top=parked||repair?182:126;waitingSection.gameObject.SetActive(waiting>0);waitingSection.anchoredPosition=new Vector2(756,-top);
            float loadedTop=top+(waiting>0?180:0);loadedSection.anchoredPosition=new Vector2(756,-loadedTop);loadedScroll.sizeDelta=new Vector2(520,Mathf.Max(120,620-loadedTop-38));
            departButton.gameObject.SetActive(docked);
            if(!docked&&Run.ActiveRoad!=null)
            {
                objectiveText.text=(Run.State==ShiftState.Encounter?"Road event":Run.State==ShiftState.Pushing?"Pushing to next stop":"In transit")+" - "+Mathf.RoundToInt(Run.Progress*100)+"%";
                destinationText.text="Next: "+Run.Stops[Run.ActiveRoad.Other(Run.Node)].Name;
                costGroup.SetActive(true);timeCostText.text=Mathf.CeilToInt(Run.TimeCost(Run.ActiveRoad)*(1-Run.Progress))+" min";
                wearCostText.text="-"+Mathf.CeilToInt(Run.WearCost(Run.ActiveRoad)*(1-Run.Progress));riskText.text="F: skip to the next event or stop";return;
            }
            costGroup.SetActive(route.Count>0);
            if(selected<0){destinationText.text="Choose a destination";riskText.text="";departButton.interactable=false;departHint.message="Depart\nSelect a stop on the map first.";return;}
            destinationText.text=Run.Stops[selected].Name;
            timeCostText.text=Mathf.CeilToInt(route.Sum(r=>Run.TimeCost(r)))+" min";wearCostText.text="-"+Mathf.CeilToInt(route.Sum(r=>Run.WearCost(r)));
            string blocked=Run.DestinationBlockReason(selected);
            var warnings=new List<string>();
            if(route.Any(r=>r.Kind==RoadKind.Storm))warnings.Add("Storm: severe weather");
            if(route.Any(r=>r.Gravel)&&Run.VehicleIndex!=2)warnings.Add("Gravel damages fragile cargo");
            if(route.Any(r=>r.Kind==RoadKind.River))warnings.Add("Keep cargo dry at the crossing");
            riskText.text=blocked!=""?blocked:string.Join(" · ",warnings);
            int at=Run.Node;var stops=new List<string>();foreach(var road in route){at=road.Other(at);stops.Add(Run.Stops[at].Name);}
            departButton.interactable=blocked=="";departHint.message=blocked!=""?blocked:"Depart > "+string.Join(" > ",stops)+"\nFastest estimated route; events may add time. Stop at waypoints, then press E to continue.";
        }
    }
}
