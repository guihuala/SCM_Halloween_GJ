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
            vehicleArtwork=CourierVehicleIconView.Ensure(vehicleText.transform.parent,"CurrentVehicleArtwork",new Vector2(32,-12),new Vector2(92,92));
            parkedVehicleArtwork=CourierVehicleIconView.Ensure(possessButton.transform,"ParkedVehicleArtwork",new Vector2(10,-6),new Vector2(40,40));
            parkedVehicleArtwork.SetVehicle(1);possessIcon.gameObject.SetActive(false);
            vehicleText.rectTransform.anchoredPosition=new Vector2(132,-24);vehicleText.rectTransform.sizeDelta=new Vector2(168,56);
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
        [Header("Destination Tooltip")]
        public RectTransform destinationTooltip;
        public Button routeModeButton;
        [Range(0,1)] public float dialogueMapAlpha=.28f;
        private CanvasGroup mapInteraction;
        private readonly CourierHudMotion drawerMotion = new CourierHudMotion();
        private CourierCargoFeedback cargoFeedback;
        public bool CargoOpen { get; private set; }
        private bool tutorialCargoLoaded;
        private bool presentationReady;
        private long presentationStamp;
        private Vector2 presentationSize;
        private long PresentationStamp()
        {
            unchecked
            {
                long value=Run.Node;
                value=value*31+(int)Run.State;value=value*31+Run.VehicleIndex;value=value*31+(int)Run.Durability;
                value=value*31+Run.OrderTurns;value=value*31+selected;value=value*31+Run.PlannedDestination;
                value=value*31+(owner.Active?1:0);value=value*31+(owner.ConversationActive?1:0);
                value=value*31+(CargoOpen?1:0);value=value*31+(Run.PreferSafeRoutes?1:0);
                value=value*31+(owner.Tutorial.CargoLoaded?1:0);value=value*31+(owner.Tutorial.Finished?1:0);
                value=value*31+(Run.OrderEventTriggered?1:0);value=value*31+Run.Results.Count;
                value=value*31+(mapArtwork!=null&&mapArtwork.isActiveAndEnabled?1:0);
                value=value*31+Mathf.RoundToInt(((RectTransform)waitingContent).rect.width);
                value=value*31+Mathf.RoundToInt(((RectTransform)loadedContent).rect.width);
                foreach(var p in Run.Parcels){value=value*31+(int)p.State;value=value*31+(p.Damaged?1:0);}
                return value;
            }
        }
        public void SetCargoOpen(bool open)
        {
            if(open&&owner!=null&&owner.ConversationActive)return;
            CargoOpen=open;
            if(cargoDrawer!=null)
            {
                var group=cargoDrawer.GetComponent<CanvasGroup>();
                if(group==null)group=cargoDrawer.gameObject.AddComponent<CanvasGroup>();
                group.interactable=open;group.blocksRaycasts=open;
                if(open){cargoDrawer.gameObject.SetActive(true);cargoDrawer.SetAsLastSibling();}
            }
        }
        private void LateUpdate()
        {
            if(hasRemoved||cargoDrawer==null)return;
            drawerMotion.RestoreLayout();
            drawerMotion.Present(cargoDrawer.gameObject,CargoOpen,new Vector2(0,-48),reducedPanelMotion);
        }
        public void ToggleCargoDrawer(){if(owner!=null&&owner.Active&&!owner.ConversationActive)SetCargoOpen(!CargoOpen);}
        private readonly List<CourierCargoRow> rows=new List<CourierCargoRow>();
        private int selected=-1;
        private int hoveredNode=-1;
        public void PreviewRoute(int node)
        {
            if(owner==null)return;
            if(!owner.Active||owner.ConversationActive||CargoOpen||Run.State!=ShiftState.Docked||node==Run.Node){hoveredNode=-1;RefreshRoutePreview();return;}
            hoveredNode=node;RefreshRoutePreview();
        }
        public void ClearRoutePreview(int node)
        {
            if(hoveredNode!=node)return;
            hoveredNode=-1;RefreshRoutePreview();
        }
        private void RefreshRoutePreview()
        {
            if(owner==null)return;
            if(!owner.Active||owner.ConversationActive||CargoOpen||Run.State!=ShiftState.Docked||hoveredNode==Run.Node)hoveredNode=-1;
            mapGraphic.PreviewRoute=Run.FindRoute(hoveredNode);
            mapGraphic.SetVerticesDirty();
        }
        private List<Road> route=new List<Road>();
        private RectTransform page,map;
        private Vector2 pageSize;
        private float layoutBottom;
        private readonly Vector3[] dialogueCorners=new Vector3[4];
        private float sidebarX=756,contentBottom=620;
        public void ApplyFullscreenLayout()
        {
            drawerMotion.RestoreLayout();
            if(page==null)page=transform.Find("Window") as RectTransform;
            if(page==null)return;
            page.anchorMin=Vector2.zero;page.anchorMax=Vector2.one;page.offsetMin=page.offsetMax=Vector2.zero;
            if(map==null)map=mapGraphic.transform.parent as RectTransform;
            if(nodePromptButton!=null&&nodePromptButton.transform.parent!=page)nodePromptButton.transform.SetParent(page,false);
            if(objectiveText==null)
            {
                var go=new GameObject("ObjectiveText",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));
                go.transform.SetParent(page,false);objectiveText=go.GetComponent<Text>();
                objectiveText.font=vehicleText.font;objectiveText.fontSize=30;
                objectiveText.color=CourierColors.Paper;objectiveText.alignment=TextAnchor.MiddleLeft;objectiveText.raycastTarget=false;
            }
            if(cargoHeading!=null)cargoHeading.text="ORDERS";
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
            float mapX=32,mapWidth=size.x-64,mapHeight=Mathf.Max(260,size.y-208);
            PrepareDestinationTooltip();
            if(mapHeading!=null)mapHeading.gameObject.SetActive(false);
            foreach(var name in new[]{"PageHeaderRule","SummaryRule"})
            {var decoration=page.Find(name);if(decoration!=null)decoration.gameObject.SetActive(false);}
            if(pageSize!=size||Mathf.Abs(layoutBottom-contentBottom)>.5f)
            {
                pageSize=size;layoutBottom=contentBottom;
                if(pageTitle!=null)pageTitle.gameObject.SetActive(false);
                Place(map,mapX,184,mapWidth,mapHeight);
                var edges=mapGraphic.rectTransform;edges.anchorMin=Vector2.zero;edges.anchorMax=Vector2.one;edges.offsetMin=edges.offsetMax=Vector2.zero;
                Place(vehicleText.rectTransform,132,24,168,56);vehicleText.fontSize=34;
                SizeElement("DurabilityIcon",316,28,48,48);
                SizeElement("DurabilityFillBackground",376,48,160,12);SizeElement("DurabilityFill",376,48,160,12);
                Place(durabilityText.rectTransform,552,24,156,56);durabilityText.fontSize=32;
                SizeElement("LoadIcon",724,28,48,48);Place(loadText.rectTransform,788,24,360,56);loadText.fontSize=32;
                SizeElement("PossessButton",32,104,264,64);SizeElement("RepairButton",328,104,240,64);
                foreach(var button in new[]{possessButton,repairButton}){var label=button.GetComponentInChildren<Text>(true);if(label!=null)label.fontSize=28;}
                if(cargoToggleButton!=null){Place((RectTransform)cargoToggleButton.transform,size.x-440,24,280,76);cargoToggleText.fontSize=32;}
                if(pauseButton!=null)ResizeIconButton(pauseButton,84);
                if(objectiveText!=null)objectiveText.fontSize=30;
                if(cargoDrawer!=null)
                {
                    float height=Mathf.Max(360,contentBottom-184),drawerWidth=Mathf.Min(820,size.x-64),column=drawerWidth-48;
                    float waitingHeight=(height-104)*.62f,loadedHeight=height-104-waitingHeight;
                    Place(cargoDrawer,size.x-drawerWidth-32,184,drawerWidth,height);
                    if(cargoHeading!=null)Place(cargoHeading.rectTransform,24,12,drawerWidth-256,40);
                    if(cargoCloseButton!=null)Place((RectTransform)cargoCloseButton.transform,drawerWidth-216,12,192,48);
                    Place(waitingSection,24,72,column,waitingHeight);
                    Place(loadedSection,24,88+waitingHeight,column,loadedHeight);
                    var waitingScroll=waitingContent.parent.parent as RectTransform;
                    Place(waitingScroll,0,44,column,waitingHeight-44);
                    Place(loadedScroll,0,44,column,loadedHeight-44);
                    foreach(var section in new[]{waitingSection,loadedSection})
                    {
                        var title=section.GetComponentInChildren<Text>(true);if(title!=null){title.rectTransform.sizeDelta=new Vector2(column,40);title.fontSize=28;}
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
            if(owner!=null){RefreshNodePrompt();RefreshDialogueFocus();RefreshDestinationTooltip();}
        }
        private void PrepareDestinationTooltip()
        {
            if(destinationText==null||departButton==null||costGroup==null||riskText==null)return;
            if(destinationTooltip==null)
            {
                var old=map.Find("DestinationTooltip");
                destinationTooltip=old!=null?(RectTransform)old:new GameObject("DestinationTooltip",typeof(RectTransform)).GetComponent<RectTransform>();
                destinationTooltip.SetParent(map,false);
                var image=destinationTooltip.GetComponent<Image>();
                if(image==null)image=destinationTooltip.gameObject.AddComponent<Image>();
                image.sprite=Resources.Load<Sprite>("NightVoyage/HUD/Panel");image.type=Image.Type.Sliced;
                image.color=CourierColors.Paper;image.raycastTarget=true;
                foreach(var element in new Transform[]{destinationText.transform,costGroup.transform,riskText.transform,departButton.transform})element.SetParent(destinationTooltip,false);
                destinationTooltip.gameObject.SetActive(false);
            }
            if(routeModeButton==null)
            {
                var go=new GameObject("RouteModeButton",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button));
                go.transform.SetParent(destinationTooltip,false);routeModeButton=go.GetComponent<Button>();
                var image=go.GetComponent<Image>();image.sprite=Resources.Load<Sprite>("NightVoyage/HUD/Panel");image.type=Image.Type.Sliced;image.color=CourierColors.Mint;
                routeModeButton.targetGraphic=image;
                var labelObject=new GameObject("Label",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));labelObject.transform.SetParent(go.transform,false);
                var label=labelObject.GetComponent<Text>();label.font=destinationText.font;label.fontSize=24;label.alignment=TextAnchor.MiddleCenter;label.color=CourierColors.Night;label.raycastTarget=false;
                label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=new Vector2(8,0);label.rectTransform.offsetMax=new Vector2(-8,0);
                routeModeButton.onClick.AddListener(()=>{if(owner==null||!owner.Active||owner.ConversationActive||Run.State!=ShiftState.Docked)return;Run.PreferSafeRoutes=!Run.PreferSafeRoutes;Refresh();});
            }
        }
        private void RefreshDestinationTooltip()
        {
            if(destinationTooltip==null)return;
            bool show=selected>=0&&selected!=Run.Node&&Run.State==ShiftState.Docked&&!Run.Finished&&!owner.ConversationActive&&!CargoOpen;
            destinationTooltip.gameObject.SetActive(show);
            if(!show)return;
            float width=Mathf.Min(600,map.rect.width-24);
            riskText.fontSize=24;riskText.resizeTextForBestFit=false;
            riskText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width-32);
            float height=Mathf.Min(Mathf.Max(320,232+riskText.preferredHeight),map.rect.height-24);
            var target=selected>=6?mapArtwork.NodeTarget(selected):mapArtwork!=null&&selected<mapArtwork.landmarkImages.Length?mapArtwork.landmarkImages[selected].rectTransform:(RectTransform)stopButtons[selected].transform;
            target.GetWorldCorners(dialogueCorners);
            var lo=map.InverseTransformPoint(dialogueCorners[0]);var hi=map.InverseTransformPoint(dialogueCorners[2]);
            float left=hi.x-map.rect.xMin+16;
            if(left+width>map.rect.width-12)left=lo.x-map.rect.xMin-width-16;
            float top=map.rect.yMax-hi.y;
            Place(destinationTooltip,Mathf.Clamp(left,12,map.rect.width-width-12),Mathf.Clamp(top,12,Mathf.Max(12,map.rect.height-height-12)),width,height);
            Place(destinationText.rectTransform,16,12,width-128,72);destinationText.fontSize=32;
            Place((RectTransform)departButton.transform,width-96,16,80,80);ResizeIconButton(departButton,80);
            Place((RectTransform)costGroup.transform,16,100,width-32,48);
            Place((RectTransform)routeModeButton.transform,16,156,width-32,48);
            Caption(routeModeButton,Run.PreferSafeRoutes?"Safe route  |  Switch to fastest":"Fastest route  |  Switch to safe");
            routeModeButton.interactable=owner.Active&&!owner.ConversationActive;
            timeCostText.fontSize=wearCostText.fontSize=28;
            Place(timeCostText.rectTransform,56,0,130,48);Place(wearCostText.rectTransform,256,0,120,48);
            var timeIcon=costGroup.transform.Find("ClockIcon") as RectTransform;var wearIcon=costGroup.transform.Find("WearIcon") as RectTransform;
            if(timeIcon!=null)Place(timeIcon,0,4,40,40);if(wearIcon!=null)Place(wearIcon,200,4,40,40);
            Place(riskText.rectTransform,16,216,width-32,Mathf.Max(64,height-232));riskText.fontSize=24;
            riskText.alignment=TextAnchor.UpperLeft;
            riskText.resizeTextForBestFit=true;riskText.resizeTextMinSize=18;riskText.resizeTextMaxSize=24;
            destinationTooltip.SetAsLastSibling();
        }
        private void RefreshDialogueFocus()
        {
            bool talking=owner.ConversationActive;
            if(mapInteraction==null)mapInteraction=map.GetComponent<CanvasGroup>();
            if(mapInteraction==null)mapInteraction=map.gameObject.AddComponent<CanvasGroup>();
            mapInteraction.alpha=talking?dialogueMapAlpha:1;
            mapInteraction.interactable=!talking;mapInteraction.blocksRaycasts=!talking;
            if(talking)
            {
                SetCargoOpen(false);
                if(destinationTooltip!=null)destinationTooltip.gameObject.SetActive(false);
                var events=UnityEngine.EventSystems.EventSystem.current;
                if(events!=null&&events.currentSelectedGameObject!=null&&events.currentSelectedGameObject.transform.IsChildOf(map))events.SetSelectedGameObject(null);
            }
            var hints=GetComponent<CourierTooltip>();if(hints!=null)hints.enabled=!talking;
        }
        private void SizeElement(string name,float x,float y,float width,float height)
        {var rect=page.Find(name) as RectTransform;if(rect!=null)Place(rect,x,y,width,height);}
        private static void ResizeIconButton(Button button,float size)
        {
            ((RectTransform)button.transform).sizeDelta=Vector2.one*size;
            var icon=button.GetComponentInChildren<CourierIconGraphic>(true);
            if(icon!=null){var rt=icon.rectTransform;rt.anchorMin=rt.anchorMax=rt.pivot=Vector2.one*.5f;rt.anchoredPosition=Vector2.zero;rt.sizeDelta=Vector2.one*52;}
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
            nodePromptText.fontSize=30;
            if(node>=6&&(!docked||!Run.HasJourney)){nodePromptButton.gameObject.SetActive(false);return;}
            nodePromptText.text=docked?(Run.HasJourney?"Continue route  [E]":"Talk  [E]"):"Travelling";
            Place((RectTransform)nodePromptButton.transform,pageSize.x-360,104,328,64);

        }
        protected override void Awake()
        {
            base.Awake();PrepareVehicleArtwork();
            if(cargoToggleButton!=null)cargoToggleButton.onClick.AddListener(ToggleCargoDrawer);
            if(cargoCloseButton!=null)cargoCloseButton.onClick.AddListener(()=>SetCargoOpen(false));
            if(nodePromptButton!=null)nodePromptButton.onClick.AddListener(()=>{if(owner==null||!owner.Active||owner.ConversationActive||Run.State!=ShiftState.Docked)return;if(Run.HasJourney)owner.AdvanceDialogue();else owner.BeginConversation();});
            for(int i=0;i<stopButtons.Length;i++)
            {
                int node=i;stopButtons[i].onClick.AddListener(()=>Select(node));
                var hover=stopButtons[i].GetComponent<CourierMapNodeHover>();
                if(hover==null)hover=stopButtons[i].gameObject.AddComponent<CourierMapNodeHover>();
                hover.panel=this;hover.node=node;
            }
            departButton.onClick.AddListener(()=>{if(selected>=0)owner.DepartTo(selected);});
            possessButton.onClick.AddListener(()=>owner.PossessParkedVehicle());repairButton.onClick.AddListener(()=>{if(owner.Active&&!owner.ConversationActive)Run.Repair();});
        }
        public override void Bind(NightCourierPrototype prototype)
        {
            presentationReady=false;tutorialCargoLoaded=false;
            mapGraphic.Owner=prototype;dropZone.Owner=prototype;SetCargoOpen(false);
            foreach(var row in rows)Destroy(row.gameObject);rows.Clear();
            foreach(var p in prototype.Run.Parcels){var row=Instantiate(rowPrefab,waitingContent);row.Bind(prototype,p);rows.Add(row);}
            hoveredNode=-1;
            selected=prototype.Run.HasJourney?prototype.Run.PlannedDestination:-1;
            if(cargoFeedback==null)cargoFeedback=gameObject.AddComponent<CourierCargoFeedback>();
            var trunkIcon=loadText.transform.parent.Find("LoadIcon") as RectTransform;
            cargoFeedback.Initialize(prototype,rows,trunkIcon!=null?trunkIcon:loadText.rectTransform,
                parcel=>mapArtwork!=null&&parcel.Destination<mapArtwork.landmarkImages.Length&&mapArtwork.landmarkImages[parcel.Destination]!=null
                    ?mapArtwork.landmarkImages[parcel.Destination].rectTransform:stopLabels[parcel.Destination].rectTransform,
                loadText,cargoToggleText);
            base.Bind(prototype);
        }
        public void Select(int node){if(!owner.Active||owner.ConversationActive||Run.State!=ShiftState.Docked)return;if(node==Run.Node){owner.BeginConversation();return;}selected=node;if(Run.DestinationBlockReason(node)=="")owner.Tutorial.ChooseRoute();Refresh();}
        protected override void Refresh()
        {
            if(cargoFeedback!=null)cargoFeedback.Observe(reducedPanelMotion);
            var window=transform.Find("Window") as RectTransform;
            Vector2 size=window!=null?window.rect.size:((RectTransform)transform).rect.size;
            long stamp=PresentationStamp();
            if(presentationReady&&presentationStamp==stamp&&presentationSize==size)
            {
                if(Run.State!=ShiftState.Docked&&!Run.Finished&&objectiveText!=null)
                    objectiveText.text=(Run.State==ShiftState.Encounter?"Road event":"In transit")+" - "+Mathf.RoundToInt(Run.DisplayProgress*100)+"%";
                if(mapGraphic.drawTravel&&Run.State==ShiftState.Driving)mapGraphic.SetVerticesDirty();
                return;
            }
            presentationReady=true;presentationStamp=stamp;presentationSize=size;
            ApplyFullscreenLayout();
            bool docked=Run.State==ShiftState.Docked;
            if(objectiveText!=null)objectiveText.text=Run.HasJourney?"Waypoint stop - Destination: "+Run.Stops[Run.PlannedDestination].Name:Run.ObjectiveText;
            route=Run.FindRoute(selected);mapGraphic.SelectedRoute=route;RefreshRoutePreview();
            for(int i=0;i<stopButtons.Length;i++)
            {
                var parcels=Run.Parcels.Where(p=>p.State==ParcelState.Loaded&&p.Destination==i).ToArray();
                var pending=Run.Parcels.Where(p=>p.State==ParcelState.Waiting&&p.Origin==i).GroupBy(p=>p.Destination).ToArray();
                bool current=i==Run.Node,locked=false;
                stopButtons[i].interactable=docked&&owner.Active&&!owner.ConversationActive;
                stopLabels[i].text=CourierStreetMap.Nodes[i].Id+" · "+Run.Stops[i].Sign;stopCounts[i].text=parcels.Length>0?"Deliver":pending.Length>0?pending.Length+" orders":"";
                stopIcons[i].gameObject.SetActive(!(current&&docked&&mapArtwork!=null&&mapArtwork.travelerIcon!=null));
                stopIcons[i].SetKind(locked?CourierIconKind.Lock:parcels.Length>0?CourierIconKind.Box:current&&docked?CourierIconKind.Pin:CourierIconKind.Dot);
                Color c=current?CourierColors.Gold:parcels.Length>0?CourierColors.Mint:CourierColors.Muted;
                stopIcons[i].color=stopLabels[i].color=stopCounts[i].color=c;
                stopHints[i].message=Run.Stops[i].Name+(current?" - You are here":"")+(locked?" - Story locked":"")+
                    (parcels.Length>0?"\nDeliveries: "+string.Join(", ",parcels.Select(p=>p.Name)):"")+string.Concat(pending.Select(g=>"\nPick up: "+string.Join(" + ",g.Select(p=>p.Name))+" > "+Run.Stops[g.Key].Name));
            }
            if(vehicleArtwork==null||parkedVehicleArtwork==null)PrepareVehicleArtwork();
            vehicleArtwork.SetVehicle(Run.VehicleIndex);
            vehicleText.text=Run.Car.Name;durabilityText.text=Run.IsGhost?"No limit":Mathf.CeilToInt(Run.Durability)+" turns";loadText.text=Run.ActiveOrder==null?"No active order":"Order "+Run.OrderTurns+"/"+Run.TargetTurns(Run.ActiveOrder);
            loadText.color=Run.Overloaded?CourierColors.Gold:CourierColors.Paper;
            durabilityFill.fillAmount=Run.IsGhost?1:Run.Durability/Run.Car.MaxDurability;
            int v=Run.Stops[Run.Node].Vehicle;bool parked=docked&&v>=0;
            possessButton.gameObject.SetActive(parked);
            parkedVehicleArtwork.gameObject.SetActive(parked&&Run.UnlockedVehicles[v]);
            possessIcon.gameObject.SetActive(parked&&!Run.UnlockedVehicles[v]);
            if(parked)parkedVehicleArtwork.SetVehicle(v);
            if(parked){possessButton.interactable=owner.Active&&!owner.ConversationActive&&Run.CanPossess(v);Caption(possessButton,Run.Vehicles[v].Name);possessIcon.SetKind(Run.UnlockedVehicles[v]?CourierIconKind.Ghost:CourierIconKind.Lock);possessHint.message=!Run.UnlockedVehicles[v]?"Old pickup locked\nDeliver the Cooling Unit from Settlement Bar to the vampire DJ.":Run.CanPossess(v)?"Possess "+Run.Vehicles[v].Name:"Vehicle unavailable at this stop.";}
            bool repair=docked&&!Run.IsGhost&&Run.Stops[Run.Node].Repair&&Run.Durability<Run.Car.MaxDurability;repairButton.gameObject.SetActive(repair);repairButton.interactable=owner.Active&&!owner.ConversationActive&&Run.Durability<Run.Car.MaxDurability;
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
            if(cargoToggleButton!=null)cargoToggleButton.interactable=owner.Active&&!owner.ConversationActive&&!Run.Finished;
            if(!owner.Tutorial.Finished&&owner.Tutorial.CargoLoaded&&!tutorialCargoLoaded)SetCargoOpen(false);
            tutorialCargoLoaded=owner.Tutorial.CargoLoaded;
            RefreshNodePrompt();
            bool controls=parked||repair;
            if(objectiveText!=null)
            {
                float left=controls?600:32,right=docked?pageSize.x-392:pageSize.x-800;
                Place(objectiveText.rectTransform,left,108,Mathf.Max(300,right-left),56);
            }
            RefreshDestinationTooltip();
            departButton.gameObject.SetActive(docked);
            if(!docked&&Run.ActiveRoad!=null)
            {
                if(objectiveText!=null)objectiveText.text=(Run.State==ShiftState.Encounter?"Road event":Run.IsReturning?"Returning to previous stop":Run.State==ShiftState.Pushing?"Pushing to next stop":"In transit")+" - "+Mathf.RoundToInt(Run.DisplayProgress*100)+"%";
                destinationText.text="Next: "+Run.Stops[Run.TravelDestination].Name;
                costGroup.SetActive(true);timeCostText.text=Run.RemainingTravelTurns+" turns";
                wearCostText.text="-"+Mathf.Min(Mathf.CeilToInt(Run.Durability),Run.RemainingTravelTurns);riskText.text=(Run.IsGhost?"Ghost transport - ":"")+"F: skip animation to the next event or stop";return;
            }
            costGroup.SetActive(route.Count>0);
            if(selected<0||selected==Run.Node){destinationText.text="Choose a destination";riskText.text=Run.Results.Count>0?"Last order: "+Run.Results.Last().Turns+"/"+Run.Results.Last().TargetTurns+" turns - "+Run.Results.Last().Evaluation:"";departButton.interactable=false;departHint.message="Depart\nSelect a stop on the map first.";return;}
            destinationText.text=Run.Stops[selected].Name;
            timeCostText.text=Mathf.CeilToInt(route.Sum(r=>Run.TimeCost(r)))+" turns";wearCostText.text="-"+Mathf.CeilToInt(route.Sum(r=>Run.WearCost(r)));
            string blocked=Run.DestinationBlockReason(selected);
            string warning=Run.RouteWarnings(route);
            if(selected>=6&&selected<=8)warning=Run.StationDescription(selected)+(warning!=""?" · "+warning:"");
            riskText.text=blocked!=""?blocked:warning;
            int at=Run.Node;var stops=new List<string>();foreach(var road in route){at=road.Other(at);stops.Add(Run.Stops[at].Name);}
            departButton.interactable=blocked==""&&!owner.ConversationActive;departHint.message=owner.ConversationActive?"Finish the conversation before departing.":blocked!=""?blocked:"Depart > "+string.Join(" > ",stops)+"\nMove one turn (1 edge as ghost, up to 2 by vehicle). Stops at junctions let you change direction. E continues the route.";
            RefreshDestinationTooltip();
        }
    }
}
