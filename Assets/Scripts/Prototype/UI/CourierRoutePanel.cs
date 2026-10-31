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
            vehicleText.rectTransform.anchoredPosition=new Vector2(132,-24);vehicleText.rectTransform.sizeDelta=new Vector2(260,56);
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
        private readonly CourierHudMotion destinationMotion = new CourierHudMotion();
        private CourierMapCanvas mapCanvas;
        private RectTransform mapViewport;
        private Text mapCanvasHelp;
        private CourierMapLegend mapLegend;
        public bool LegendHighlightsNode(int node) => mapLegend!=null&&mapLegend.HighlightsNode(node);
        private bool destinationVisible;
        private CourierMapMotion mapMotion;
        public bool MapReducedMotion => reducedPanelMotion;
        public int HoveredMapNode => hoveredNode;
        public int SelectedMapNode => selected;
        public bool CanNavigateMap => owner!=null&&owner.Active&&!owner.ConversationActive&&!CargoOpen&&!Run.Finished;
        private CourierCargoFeedback cargoFeedback;
        public bool CargoOpen { get; private set; }
        private bool tutorialCargoLoaded;
        private bool presentationReady;
        private Button informationButton;
        private Text activeRecipientLabel;
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
            if(hasRemoved)return;
            if(cargoDrawer!=null){drawerMotion.RestoreLayout();drawerMotion.Present(cargoDrawer.gameObject,CargoOpen,new Vector2(0,-48),reducedPanelMotion);}
            if(destinationTooltip!=null)
            {
                destinationMotion.RestoreLayout();
                if(destinationVisible)RefreshDestinationTooltip();
                destinationMotion.Present(destinationTooltip.gameObject,destinationVisible,new Vector2(0,-12),reducedPanelMotion);
            }
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
            destinationMotion.RestoreLayout();
            if(page==null)page=transform.Find("Window") as RectTransform;
            if(page==null)return;
            page.anchorMin=Vector2.zero;page.anchorMax=Vector2.one;page.offsetMin=page.offsetMax=Vector2.zero;
            if(map==null)map=mapGraphic.transform.parent as RectTransform;
            if(mapViewport==null)
            {
                mapViewport=new GameObject("MapViewport",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(RectMask2D),typeof(CourierMapCanvas)).GetComponent<RectTransform>();
                mapViewport.SetParent(page,false);mapViewport.SetAsFirstSibling();
                var surface=mapViewport.GetComponent<Image>();surface.color=Color.clear;surface.raycastTarget=true;
                map.SetParent(mapViewport,false);mapCanvas=mapViewport.GetComponent<CourierMapCanvas>();mapCanvas.content=map;mapCanvas.panel=this;
                var help=new GameObject("MapCanvasHelp",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));help.transform.SetParent(page,false);
                mapCanvasHelp=help.GetComponent<Text>();mapCanvasHelp.font=vehicleText.font;mapCanvasHelp.fontSize=20;mapCanvasHelp.color=CourierColors.Muted;
                mapCanvasHelp.text="";mapCanvasHelp.enabled=false;mapCanvasHelp.raycastTarget=false;
            }
            if(nodePromptButton!=null&&nodePromptButton.transform.parent!=page)nodePromptButton.transform.SetParent(page,false);
            if(objectiveText==null)
            {
                var go=new GameObject("ObjectiveText",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));
                go.transform.SetParent(page,false);objectiveText=go.GetComponent<Text>();
                objectiveText.font=vehicleText.font;objectiveText.fontSize=30;
                objectiveText.color=CourierColors.Paper;objectiveText.alignment=TextAnchor.MiddleLeft;objectiveText.raycastTarget=false;
            }
            if(cargoHeading!=null){cargoHeading.enabled=false;CourierSymbolUI.Icon(cargoDrawer,"OrdersSymbol",CourierIconKind.Box,28,20,44,CourierColors.Night,"Orders · Tab");}
            objectiveText.enabled=false;vehicleText.enabled=false;
            if(waitingEmptyText!=null)waitingEmptyText.enabled=false;
            if(loadedEmptyText!=null)loadedEmptyText.enabled=false;
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
            float mapX=0,mapWidth=size.x-32,mapHeight=Mathf.Max(260,size.y-144);
            Place(mapViewport,0,0,size.x,size.y);
            Place(mapCanvasHelp.rectTransform,24,size.y-40,440,32);
            PrepareDestinationTooltip();
            if(mapLegend==null)mapLegend=CourierMapLegend.Create(page,this,vehicleText.font);
            mapLegend.ApplyLayout(size);
            if(mapHeading!=null)mapHeading.gameObject.SetActive(false);
            foreach(var name in new[]{"PageHeaderRule","SummaryRule"})
            {var decoration=page.Find(name);if(decoration!=null)decoration.gameObject.SetActive(false);}
            if(pageSize!=size||Mathf.Abs(layoutBottom-contentBottom)>.5f)
            {
                pageSize=size;layoutBottom=contentBottom;
                if(pageTitle!=null)pageTitle.gameObject.SetActive(false);
                Place(map,mapX,0,mapWidth,mapHeight);mapCanvas.ApplyLayout();
                var edges=mapGraphic.rectTransform;edges.anchorMin=Vector2.zero;edges.anchorMax=Vector2.one;edges.offsetMin=edges.offsetMax=Vector2.zero;
                Place(vehicleText.rectTransform,132,24,260,56);vehicleText.fontSize=34;
                SizeElement("DurabilityIcon",160,28,48,48);
                SizeElement("DurabilityFillBackground",224,48,140,12);SizeElement("DurabilityFill",224,48,140,12);
                Place(durabilityText.rectTransform,384,24,96,56);durabilityText.fontSize=32;
                SizeElement("LoadIcon",512,28,48,48);Place(loadText.rectTransform,576,24,180,56);loadText.fontSize=32;
                SizeElement("PossessButton",32,104,80,64);SizeElement("RepairButton",128,104,80,64);
                foreach(var button in new[]{possessButton,repairButton}){var label=button.GetComponentInChildren<Text>(true);if(label!=null)label.fontSize=28;}
                if(cargoToggleButton!=null){Place((RectTransform)cargoToggleButton.transform,size.x-332,24,172,76);cargoToggleText.fontSize=32;}
                if(pauseButton!=null)ResizeIconButton(pauseButton,84);
                if(objectiveText!=null)objectiveText.fontSize=30;
                if(cargoDrawer!=null)
                {
                    float height=Mathf.Max(360,contentBottom-184),drawerWidth=Mathf.Min(820,size.x-64),column=drawerWidth-48;
                    float waitingHeight=(height-144)*.64f,loadedHeight=height-144-waitingHeight;
                    Place(cargoDrawer,size.x-drawerWidth-32,184,drawerWidth,height);
                    if(cargoHeading!=null)Place(cargoHeading.rectTransform,28,20,drawerWidth-280,52);
                    if(cargoCloseButton!=null)Place((RectTransform)cargoCloseButton.transform,drawerWidth-84,16,56,56);
                    Place(waitingSection,24,92,column,waitingHeight);
                    Place(loadedSection,24,116+waitingHeight,column,loadedHeight);
                    foreach(var content in new[]{waitingContent,loadedContent})
                    {
                        var layout=content.GetComponent<VerticalLayoutGroup>();
                        if(layout!=null){layout.spacing=16;layout.padding=new RectOffset(4,12,8,16);}
                    }
                    var waitingScroll=waitingContent.parent.parent as RectTransform;
                    Place(waitingScroll,0,56,column,waitingHeight-56);
                    Place(loadedScroll,0,56,column,loadedHeight-56);
                    foreach(var section in new[]{waitingSection,loadedSection})
                    {
                        var title=section.GetComponentInChildren<Text>(true);if(title!=null){title.enabled=false;title.rectTransform.sizeDelta=new Vector2(column,44);}
                        CourierSymbolUI.Icon(section,"SectionSymbol",section==waitingSection?CourierIconKind.Store:CourierIconKind.Box,0,0,40,CourierColors.Night,section==waitingSection?"Available orders · visit the sender to collect":"On board · deliver this batch first");
                    }
                    if(waitingEmptyText!=null)Place(waitingEmptyText.rectTransform,0,64,column,56);
                    if(loadedEmptyText!=null)Place(loadedEmptyText.rectTransform,0,64,column,56);
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
            if(mapMotion==null)mapMotion=map.GetComponent<CourierMapMotion>()??map.gameObject.AddComponent<CourierMapMotion>();
            mapMotion.panel=this;
            if(owner!=null){RefreshNodePrompt();RefreshDialogueFocus();RefreshDestinationTooltip();}
        }
        private void PrepareDestinationTooltip()
        {
            if(destinationText==null||departButton==null||costGroup==null||riskText==null)return;
            if(destinationTooltip==null)
            {
                var old=map.Find("DestinationTooltip");
                destinationTooltip=old!=null?(RectTransform)old:new GameObject("DestinationTooltip",typeof(RectTransform)).GetComponent<RectTransform>();
                destinationTooltip.SetParent(page,false);
                var image=destinationTooltip.GetComponent<Image>();
                if(image==null)image=destinationTooltip.gameObject.AddComponent<Image>();
                image.sprite=Resources.Load<Sprite>("NightVoyage/HUD/Panel");image.type=Image.Type.Sliced;
                image.color=CourierColors.Paper;image.raycastTarget=true;
                foreach(var element in new Transform[]{destinationText.transform,costGroup.transform,riskText.transform,departButton.transform})element.SetParent(destinationTooltip,false);
                destinationTooltip.gameObject.SetActive(false);
            }
            if(destinationTooltip.parent!=page)destinationTooltip.SetParent(page,false);
            if(routeModeButton==null)
            {
                var go=new GameObject("RouteModeButton",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button));
                go.transform.SetParent(destinationTooltip,false);routeModeButton=go.GetComponent<Button>();
                var image=go.GetComponent<Image>();image.sprite=Resources.Load<Sprite>("NightVoyage/HUD/Panel");image.type=Image.Type.Sliced;image.color=CourierColors.Mint;
                routeModeButton.targetGraphic=image;
                var labelObject=new GameObject("Label",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));labelObject.transform.SetParent(go.transform,false);
                var label=labelObject.GetComponent<Text>();label.font=CourierTypography.Display;label.fontSize=24;label.alignment=TextAnchor.MiddleCenter;label.color=CourierColors.Night;label.raycastTarget=false;
                label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=new Vector2(8,0);label.rectTransform.offsetMax=new Vector2(-8,0);
                routeModeButton.onClick.AddListener(()=>{if(owner==null||!owner.Active||owner.ConversationActive||Run.State!=ShiftState.Docked)return;Run.PreferSafeRoutes=!Run.PreferSafeRoutes;Refresh();});
            }
        }
        private void RefreshDestinationTooltip()
        {
            if(destinationTooltip==null)return;
            destinationVisible=selected>=0&&selected!=Run.Node&&Run.State==ShiftState.Docked&&!Run.Finished&&!owner.ConversationActive&&!CargoOpen;
            if(!destinationVisible)return;
            float width=Mathf.Min(600,page.rect.width-24);
            riskText.fontSize=24;riskText.resizeTextForBestFit=false;
            riskText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width-32);
            float height=Mathf.Min(352,page.rect.height-136);
            var target=selected>=6?mapArtwork.NodeTarget(selected):mapArtwork!=null&&selected<mapArtwork.landmarkImages.Length?mapArtwork.landmarkImages[selected].rectTransform:(RectTransform)stopButtons[selected].transform;
            target.GetWorldCorners(dialogueCorners);
            var lo=page.InverseTransformPoint(dialogueCorners[0]);var hi=page.InverseTransformPoint(dialogueCorners[2]);
            float left=hi.x-page.rect.xMin+16;
            if(left+width>page.rect.width-12)left=lo.x-page.rect.xMin-width-16;
            float top=page.rect.yMax-hi.y;
            Place(destinationTooltip,Mathf.Clamp(left,12,page.rect.width-width-12),Mathf.Clamp(top,120,Mathf.Max(120,page.rect.height-height-12)),width,height);
            Place(destinationText.rectTransform,24,20,width-144,76);destinationText.fontSize=34;destinationText.fontStyle=FontStyle.Normal;destinationText.color=Color.Lerp(CourierColors.Night,CourierColors.Mint,.28f);
            Place((RectTransform)departButton.transform,width-100,20,80,80);ResizeIconButton(departButton,80);
            Place((RectTransform)costGroup.transform,20,112,width-40,56);
            Place((RectTransform)routeModeButton.transform,20,268,72,64);
            Caption(routeModeButton,Run.PreferSafeRoutes?"Safe route  |  Switch to fastest":"Fastest route  |  Switch to safe");
            CourierSymbolUI.Button(routeModeButton,Run.PreferSafeRoutes?CourierIconKind.Check:CourierIconKind.FastForward,Run.PreferSafeRoutes?"Safe route · click for fastest route":"Fastest route · click for safe route");
            routeModeButton.interactable=owner.Active&&!owner.ConversationActive;
            timeCostText.fontSize=wearCostText.fontSize=32;
            wearCostText.color=Color.Lerp(CourierColors.Night,CourierColors.Gold,.38f);
            Place(timeCostText.rectTransform,56,0,130,48);Place(wearCostText.rectTransform,256,0,120,48);
            var timeIcon=costGroup.transform.Find("ClockIcon") as RectTransform;var wearIcon=costGroup.transform.Find("WearIcon") as RectTransform;
            if(timeIcon!=null)Place(timeIcon,0,4,40,40);if(wearIcon!=null)Place(wearIcon,200,4,40,40);
            riskText.enabled=false;
            string summary=riskText.text;
            int sentence=summary.IndexOf(". ",System.StringComparison.Ordinal);if(sentence>=0)summary=summary.Substring(0,sentence+1);
            if(summary.Length>96)summary=summary.Substring(0,93)+"…";
            Color summaryColor=summary.StartsWith("Protect",System.StringComparison.OrdinalIgnoreCase)?Color.Lerp(CourierColors.Night,CourierColors.Mint,.38f):Color.Lerp(CourierColors.Night,CourierColors.Gold,.3f);
            var summaryLabel=MetricLabel("RouteSummary",summary,24,194,summaryColor);
            summaryLabel.fontSize=23;summaryLabel.fontStyle=FontStyle.Normal;Place(summaryLabel.rectTransform,24,194,width-48,64);
            summaryLabel.horizontalOverflow=HorizontalWrapMode.Wrap;summaryLabel.verticalOverflow=VerticalWrapMode.Truncate;
            MetricLabel("TravelLabel","TURNS",80,164,Color.Lerp(CourierColors.Night,CourierColors.Mint,.32f));
            MetricLabel("WearLabel","WEAR",280,164,wearCostText.color);
            MetricLabel("RouteModeLabel",Run.PreferSafeRoutes?"Safer route":"Fastest route",112,280,CourierColors.Night);
            var warning=CourierSymbolUI.Icon(destinationTooltip,"RouteWarningSymbol",CourierIconKind.Warning,width-68,280,36,CourierColors.Gold,riskText.text);
            warning.gameObject.SetActive(!string.IsNullOrWhiteSpace(riskText.text));
            destinationTooltip.SetAsLastSibling();
        }
        private Text MetricLabel(string name,string text,float x,float y,Color color)
        {
            var child=destinationTooltip.Find(name);
            if(child==null){var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));go.transform.SetParent(destinationTooltip,false);child=go.transform;}
            var label=child.GetComponent<Text>();label.font=CourierTypography.Body;label.fontSize=20;label.fontStyle=FontStyle.Normal;label.text=text;label.color=color;label.raycastTarget=false;
            Place(label.rectTransform,x,y,220,36);
            return label;
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
                destinationVisible=false;
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
            Place((RectTransform)nodePromptButton.transform,pageSize.x-112,104,80,64);
            CourierSymbolUI.Button(nodePromptButton,Run.HasJourney?CourierIconKind.Arrow:CourierIconKind.Talk,Run.HasJourney?"Continue to "+Run.Stops[Run.PlannedDestination].Name+" · E":"Talk · E");

        }
        private void RefreshKeyInformation()
        {
            if(informationButton==null)
            {
                var go=new GameObject("InformationButton",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button));
                go.transform.SetParent(page,false);informationButton=go.GetComponent<Button>();
                var image=go.GetComponent<Image>();image.sprite=Resources.Load<Sprite>("NightVoyage/HUD/Button");image.type=Image.Type.Sliced;image.color=CourierColors.Raised;informationButton.targetGraphic=image;
            }
            Place((RectTransform)informationButton.transform,pageSize.x-436,24,80,76);
            CourierSymbolUI.Button(informationButton,CourierIconKind.Info,CourierInformation.Rules(Run));
            informationButton.GetComponent<CourierIconHint>().showOnClick=true;
            if(activeRecipientLabel==null)
            {
                var go=new GameObject("ActiveRecipientLabel",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));go.transform.SetParent(page,false);
                activeRecipientLabel=go.GetComponent<Text>();activeRecipientLabel.font=owner.UIFont;activeRecipientLabel.fontSize=28;activeRecipientLabel.color=CourierColors.Paper;activeRecipientLabel.alignment=TextAnchor.MiddleLeft;
            }
            var order=Run.ActiveOrder;bool active=order!=null;
            activeRecipientLabel.gameObject.SetActive(active);
            var pin=CourierSymbolUI.Icon(page,"RecipientPin",CourierIconKind.Pin,824,28,44,CourierColors.Mint,active?"Deliver to "+Run.Stops[order.Destination].Name:"");pin.gameObject.SetActive(active);
            bool damaged=active&&Run.Batch(order).Any(p=>p.Damaged);
            var state=CourierSymbolUI.Icon(page,"ActiveOrderStateSymbol",damaged||Run.Late?CourierIconKind.Warning:CourierIconKind.Check,760,28,44,damaged||Run.Late?CourierColors.Gold:CourierColors.Mint,active?CourierInformation.Order(Run,order):"");state.gameObject.SetActive(active);
            if(active)
            {
                activeRecipientLabel.text=Run.Stops[order.Destination].Name;Place(activeRecipientLabel.rectTransform,884,24,480,56);
                CourierSymbolUI.Hint(activeRecipientLabel.transform,CourierInformation.Order(Run,order));activeRecipientLabel.raycastTarget=true;
            }
            var loadIcon=loadText.transform.parent.Find("LoadIcon");
            if(loadIcon!=null)
            {
                var symbol=loadIcon.GetComponent<CourierIconGraphic>();if(symbol!=null)symbol.SetKind(active?CourierIconKind.Clock:CourierIconKind.Box);
                CourierSymbolUI.Hint(loadIcon,active?CourierInformation.Order(Run,order):"No active order · visit a sender and accept a batch in Orders (Tab)");
            }
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
        public void Select(int node){if(!owner.Active||owner.ConversationActive||Run.State!=ShiftState.Docked)return;if(node==Run.Node){owner.BeginConversation();return;}selected=node;if(Run.DestinationBlockReason(node)=="")owner.Tutorial.ChooseRoute();Refresh();if(mapMotion!=null)mapMotion.PlaySelection(node);}
        public void ClearDestination()
        {
            if(!CanNavigateMap)return;
            selected=hoveredNode=-1;
            var events=UnityEngine.EventSystems.EventSystem.current;if(events!=null)events.SetSelectedGameObject(null);
            Refresh();
        }
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
                stopLabels[i].text=Run.Stops[i].Sign;stopCounts[i].text="";stopCounts[i].enabled=false;
                stopIcons[i].gameObject.SetActive(!(current&&docked&&mapArtwork!=null&&mapArtwork.travelerIcon!=null));
                stopIcons[i].SetKind(locked?CourierIconKind.Lock:parcels.Length>0?CourierIconKind.Box:current&&docked?CourierIconKind.Pin:CourierIconKind.Dot);
                Color c=current?CourierColors.Gold:parcels.Length>0?CourierColors.Mint:CourierColors.Muted;
                stopIcons[i].color=stopLabels[i].color=stopCounts[i].color=c;
                var badge=stopButtons[i].transform.Find("OrderBadgeSymbol");if(badge!=null)badge.gameObject.SetActive(pending.Length>0&&parcels.Length==0);
                stopHints[i].message=Run.Stops[i].Name+(current?" - You are here":"")+(locked?" - Story locked":"")+
                    (parcels.Length>0?"\nDeliveries: "+string.Join(", ",parcels.Select(p=>p.Name)):"")+string.Concat(pending.Select(g=>"\nPick up: "+string.Join(" + ",g.Select(p=>p.Name))+" > "+Run.Stops[g.Key].Name));
            }
            if(vehicleArtwork==null||parkedVehicleArtwork==null)PrepareVehicleArtwork();
            vehicleArtwork.SetVehicle(Run.VehicleIndex);
            vehicleText.text=Run.Car.Name;durabilityText.text=Run.IsGhost?"∞":Mathf.CeilToInt(Run.Durability).ToString();loadText.text=Run.ActiveOrder==null?"—":Run.OrderTurns+"/"+Run.TargetTurns(Run.ActiveOrder);
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
            if(cargoToggleText!=null)cargoToggleText.text=Run.CompletedBatches+"/"+Run.BatchCount;
            if(cargoToggleButton!=null)cargoToggleButton.interactable=owner.Active&&!owner.ConversationActive&&!Run.Finished;
            if(!owner.Tutorial.Finished&&owner.Tutorial.CargoLoaded&&!tutorialCargoLoaded)SetCargoOpen(false);
            tutorialCargoLoaded=owner.Tutorial.CargoLoaded;
            RefreshKeyInformation();
            RefreshNodePrompt();
            CourierSymbolUI.Button(cargoToggleButton,CourierIconKind.Box,"Orders · Tab",true);
            cargoToggleText.alignment=TextAnchor.MiddleRight;cargoToggleText.rectTransform.offsetMin=new Vector2(64,8);cargoToggleText.rectTransform.offsetMax=new Vector2(-16,-8);
            CourierSymbolUI.Button(cargoCloseButton,CourierIconKind.Close,"Close orders · Tab");
            foreach(var topButton in new[]{informationButton,cargoToggleButton,pauseButton})
            {
                if(topButton==null)continue;
                var backing=topButton.targetGraphic as Image;if(backing!=null)backing.color=Color.clear;
                foreach(var border in topButton.GetComponents<Outline>())border.enabled=false;
            }
            CourierSymbolUI.Hint(vehicleArtwork.transform,CourierInformation.Vehicle(Run));
            vehicleArtwork.artwork.raycastTarget=true;vehicleArtwork.ghost.raycastTarget=true;
            CourierSymbolUI.Hint(durabilityText.transform,Run.IsGhost?"Ghost · unlimited movement":"Vehicle turns remaining: "+Mathf.CeilToInt(Run.Durability));durabilityText.raycastTarget=true;
            CourierSymbolUI.Hint(loadText.transform,Run.ActiveOrder==null?"No active order":CourierInformation.Order(Run,Run.ActiveOrder));loadText.raycastTarget=true;
            var emptyLoaded=CourierSymbolUI.Icon(loadedSection,"EmptySymbol",CourierIconKind.Box,12,72,48,CourierColors.Muted,"No active order");emptyLoaded.gameObject.SetActive(loadedCount==0);
            var emptyWaiting=CourierSymbolUI.Icon(waitingSection,"EmptySymbol",CourierIconKind.Check,12,72,48,CourierColors.Muted,"All orders collected");emptyWaiting.gameObject.SetActive(waiting==0);
            if(parked){parkedVehicleArtwork.gameObject.SetActive(false);CourierSymbolUI.Button(possessButton,v==0?CourierIconKind.Heat:v==1?CourierIconKind.Dry:CourierIconKind.Fragile,CourierInformation.Station(Run,Run.Node));}
            CourierSymbolUI.Button(repairButton,CourierIconKind.Wrench,"Repair vehicle");
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
                costGroup.SetActive(true);timeCostText.text=Run.RemainingTravelTurns.ToString();
                wearCostText.text="-"+Mathf.Min(Mathf.CeilToInt(Run.Durability),Run.RemainingTravelTurns);riskText.text=(Run.IsGhost?"Ghost transport - ":"")+"F: skip animation to the next event or stop";return;
            }
            costGroup.SetActive(route.Count>0);
            if(selected<0||selected==Run.Node){destinationText.text="Choose a destination";riskText.text=Run.Results.Count>0?"Last order: "+Run.Results.Last().Turns+"/"+Run.Results.Last().TargetTurns+" turns - "+Run.Results.Last().Evaluation:"";departButton.interactable=false;departHint.message="Depart\nSelect a stop on the map first.";return;}
            destinationText.text=Run.Stops[selected].Name;
            int travelTurns=Mathf.CeilToInt(route.Sum(r=>Run.TimeCost(r)));
            timeCostText.text=Run.ActiveOrder==null?travelTurns.ToString():(Run.OrderTurns+travelTurns)+"/"+Run.TargetTurns(Run.ActiveOrder);
            timeCostText.color=Run.ActiveOrder!=null&&Run.OrderTurns+travelTurns>Run.TargetTurns(Run.ActiveOrder)?CourierColors.Gold:CourierColors.Night;
            var forecastClock=costGroup.transform.Find("ClockIcon");
            if(forecastClock!=null)CourierSymbolUI.Hint(forecastClock,Run.ActiveOrder==null?"Travel turns · no order clock while empty":"Projected order turns / target (current + travel). Excludes random events.");wearCostText.text="-"+Mathf.CeilToInt(route.Sum(r=>Run.WearCost(r)));
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
