using UnityEngine;

namespace NightCourier
{
    // The simulation is scene-local; all UI is now editable uGUI prefabs.
    public sealed class NightCourierPrototype : MonoBehaviour
    {
        public static NightCourierPrototype Current { get; private set; }
        public CourierRun Run { get; private set; }
        public CourierTutorial Tutorial { get; private set; } = new CourierTutorial();
        public bool InCar { get; private set; } = true;
        public Vector2 GhostPosition { get; private set; } = new Vector2(395, 330);
        [SerializeField] private Font uiFont;
        [SerializeField] private AudioClip radioMusic;
        [SerializeField,Range(1,8)] private float cruisePlaybackSpeed=4f;
        [SerializeField, Min(0)] private float gameplayEntranceDuration = 1.6f;
        private CourierGameplayEntrance entrance;
        public float DriveBlend {get;private set;}
        public float RoadMotionTime {get;private set;}
        public float PossessionGlow {get;private set;}
        public Font UIFont { get { return CourierTypography.Body; } }
        public bool HasPlanner { get { return planner != null; } }
        private CourierPanel planner, forcedPanel;
        private CourierHudPanel hud;
        private CourierTutorialPanel tutorialPanel;
        private ShiftState lastState;
        private float visualTime;
        private string[] dialogue = new string[0];
        private int dialogueIndex;
        private int dialogueNode=-1;
        private bool openingConversationPending = true;
        public bool GuideOpen { get { return Tutorial.GuideOpen; } }
        public void RejectAction(string reason, RectTransform target=null)
        {
            var route=planner as CourierRoutePanel;
            if(route!=null&&route.FriendlyFeedback!=null)
                route.FriendlyFeedback.Reject(target!=null?target:route.loadText.rectTransform,reason);
        }
        public void ShowGuide() { Tutorial.ShowGuide(); SyncTutorialPanel(); }
        public bool AcceptParcel(Parcel parcel)
        {
            if(!Active)return false;
            if(ConversationActive||GuideOpen){RejectAction(ConversationActive?"Finish the conversation first.":"Close the guide first.");return false;}
            if(!Run.LoadParcel(parcel)){RejectAction(parcel!=null&&parcel.Origin!=Run.Node?"Visit the sender to collect this order.":"This order cannot be collected now.");return false;}
            var route=planner as CourierRoutePanel;if(route!=null)route.SetCargoOpen(false);
            dialogueNode=Run.Node;dialogue=Run.Story!=null?Run.Story.ReadPickup(Run,parcel):new string[0];dialogueIndex=0;
            if(dialogue.Length==0)dialogue=new[]{"Narrator: Collected "+string.Join(" + ",System.Linq.Enumerable.Select(Run.Batch(parcel),p=>p.Name))+". Deliver to "+Run.Stops[parcel.Destination].Name+"."};
            Tutorial.Observe(Run);return true;
        }
        public void InteractAtNode()
        {
            if(!Active||ConversationActive||GuideOpen||Run.State!=ShiftState.Docked)return;
            if(Run.Story!=null&&Run.Story.HasPending(Run)){BeginConversation();return;}
            if(Run.Stops[Run.Node].Vehicle>=0){PossessParkedVehicle();return;}
            if(System.Linq.Enumerable.Any(Run.Parcels,p=>Run.CanLoad(p))){ShowPlanner("CourierRoutePanel");return;}
            if(Run.HasJourney)AdvanceDialogue();
        }
        public bool ConversationActive { get { return dialogueIndex < dialogue.Length; } }
        public string DialogueSpeaker { get { if(!ConversationActive)return "";var line=dialogue[dialogueIndex];int colon=line.IndexOf(':');return colon>0?line.Substring(0,colon):"Narrator"; } }
        public string Subtitle
        {
            get
            {
                if (ConversationActive && (Run.State == ShiftState.Docked || Run.Finished))
                {
                    // The speaker is shown separately in the dialogue header.
                    var line = dialogue[dialogueIndex];
                    int colon = line.IndexOf(':');
                    return colon > 0 ? line.Substring(colon + 1).TrimStart() : line;
                }
                if (Run.State == ShiftState.Docked) return Run.Stops[Run.Node].Name + " | " + (Run.Stops[Run.Node].Vehicle>=0?Run.StationDescription(Run.Node)+"  [E: Swap vehicle]":Run.HasJourney?"Continue to "+Run.Stops[Run.PlannedDestination].Name+"  [E: Continue]":Run.Node>=6?Run.StationDescription(Run.Node):"Click this stop or press E to talk.");
                if (Run.State == ShiftState.Pushing) return "Broken down. You are pushing to the next stop. The journey continues.";
                if (Run.State == ShiftState.Driving && Run.IsReturning) return "Returning to " + Run.Stops[Run.TravelDestination].Name;
                if (Run.State == ShiftState.Driving) return "Heading to " + Run.Stops[Run.HasJourney?Run.PlannedDestination:Run.TravelDestination].Name+(Run.HasJourney&&Run.TravelDestination!=Run.PlannedDestination?" - Next stop: "+Run.Stops[Run.TravelDestination].Name:"");
                return "";
            }
        }
        public void BeginConversation()
        {
            if ((Run.State != ShiftState.Docked && !Run.Finished) || !Active || GuideOpen || Run.Story==null || !Run.Story.HasPending(Run)) return;
            if(ConversationActive&&dialogueNode==Run.Node)return;
            dialogueNode=Run.Node;
            dialogue = Run.TalkToNpc(); dialogueIndex = 0;
        }
        public void AdvanceDialogue()
        {
            if(!Active)return;
            if(ConversationActive&&hud!=null&&hud.CompleteSubtitle())return;
            if (ConversationActive) { dialogueIndex++; CourierSoundscape.PlayInterface("dialogue"); if(!ConversationActive){if(Run.Story!=null&&Run.Story.HasPending(Run))BeginConversation();else {Tutorial.OpenMap();if(Run.Story!=null&&Run.Story.WasPlayed("gooy_letter"))Tutorial.AfterLetter();}} }
            else if(Run.State==ShiftState.Docked&&Run.HasJourney&&Active)DepartTo(Run.PlannedDestination);
        }
        private void BeginOpeningConversation()
        {
            if (!openingConversationPending || !Active || GuideOpen) return;
            BeginConversation();
            openingConversationPending = false;
        }
        public float VisualTime { get { return visualTime; } }
        public bool Active { get { return (entrance == null || !entrance.IsRevealing) && (GameManager.Instance == null || GameManager.Instance.CurrentState == GameManager.GameState.Playing); } }
        private void Awake()
        {
            Current = this;
            Run = new CourierRun(System.Environment.TickCount) { Node = CourierStreetMap.Node("S").StopId };
            CourierContentOverrides.Apply(Run);
            // Handwritten body face; headings and actions use the display role.
            uiFont = CourierTypography.Body;
        }
        private void Start()
        {
            planner = Open<CourierPanel>("CourierRoutePanel", UIPanelLayer.Bottom);
            hud = Open<CourierHudPanel>("CourierHudPanel", UIPanelLayer.Normal);
            lastState = Run.State;
            BeginOpeningConversation();
            SyncTutorialPanel();
            gameObject.AddComponent<CourierSoundscape>().Initialize(this, radioMusic);
            var route = planner as CourierRoutePanel;
            entrance = CourierGameplayEntrance.Play(transform, gameplayEntranceDuration,
                (route != null && route.MapReducedMotion) || (hud != null && hud.reducedMotion));
        }
        private void SyncTutorialPanel()
        {
            if((Tutorial.Finished&&!GuideOpen)||Run.Finished)
            {
                if(tutorialPanel!=null){UIManager.Instance.ClosePanel("CourierTutorialPanel");tutorialPanel=null;}
                return;
            }
            if(tutorialPanel==null&&Active)
            {
                tutorialPanel=UIManager.Instance.OpenPanel("CourierTutorialPanel",null,UIPanelLayer.Top) as CourierTutorialPanel;
                if(tutorialPanel!=null)tutorialPanel.Bind(this);
            }
        }
        private T Open<T>(string name, UIPanelLayer layer) where T : CourierPanel
        {
            var p = UIManager.Instance.OpenPanel(name, null, layer) as T;
            if (p != null) p.Bind(this);
            return p;
        }
        public void ShowPlanner(string name)
        {
            if(!Active)return;
            Tutorial.Observe(Run);if(!ConversationActive){if(Run.Story!=null&&Run.Story.HasPending(Run))BeginConversation();else Tutorial.OpenMap();}
            if(planner==null)planner=Open<CourierPanel>("CourierRoutePanel",UIPanelLayer.Bottom);
            var routePanel=planner as CourierRoutePanel;if(routePanel!=null)routePanel.SetCargoOpen(true);
        }
        // The map is the main play surface, so Esc now goes straight to pause.
        public bool ClosePlanner(){return false;}
        public void ToggleCar()
        {
            if (Run.State != ShiftState.Docked || !Active) return;
            if(!Run.UseGhost())return;
            InCar = false;
            if (!InCar) GhostPosition = new Vector2(395, 330);
        }
        public bool PossessParkedVehicle()
        {
            if(!Active||ConversationActive||GuideOpen)return false;
            int vehicle = Run.Stops[Run.Node].Vehicle;
            if (vehicle < 0 || !Run.Possess(vehicle)) return false;
            InCar = true; PossessionGlow=1;
            var route = planner as CourierRoutePanel;
            if(route!=null)route.PresentVehicleExchange();
            return true;
        }
        public bool DepartTo(int destination)
        {
            if(!Active)return false;
            if(ConversationActive||GuideOpen){RejectAction(ConversationActive?"Finish the conversation first.":"Close the guide first.");return false;}
            if(!Run.StartJourney(destination)){RejectAction(Run.State!=ShiftState.Docked?"Wait until you arrive.":Run.DestinationBlockReason(destination));return false;}
            Tutorial.Observe(Run); Tutorial.Leave();
            InCar=!Run.IsGhost;ClosePlanner();return true;
        }
        public bool Depart(Road road)
        {
            // Transport mode is owned by the run, including ghost fallback.
            if (!Active||ConversationActive||GuideOpen||!Run.Depart(road)) return false;
            Tutorial.Observe(Run); Tutorial.Leave();
            ClosePlanner();
            return true;
        }
        public bool SkipRide()
        {
            if(!Active||!Run.FastForwardTravel())return false;
            SyncState();return true;
        }
        public bool CanUndoMovement => Active && !GuideOpen && Run.CanUndoMovement;
        public bool UndoMovement()
        {
            if(!CanUndoMovement||!Run.UndoMovement())return false;
            CloseForced();
            dialogue=new string[0];dialogueIndex=0;dialogueNode=-1;
            openingConversationPending=false;
            InCar=!Run.IsGhost;GhostPosition=new Vector2(395,330);lastState=Run.State;
            DriveBlend=RoadMotionTime=PossessionGlow=0;
            var soundscape=GetComponent<CourierSoundscape>();if(soundscape!=null)soundscape.ClearQueuedCues();
            if(planner!=null)planner.Bind(this);
            if(hud!=null)hud.Bind(this);
            SyncTutorialPanel();
            return true;
        }
        public void ResolveEncounter(bool safe)
        {
            if(Run.State==ShiftState.Encounter) Tutorial.ResolveEvent();
            Run.ResolveEncounter(safe);
            CloseForced();
        }
        private void CloseForced()
        {
            if (forcedPanel == null) return;
            UIManager.Instance.ClosePanel(forcedPanel.gameObject.name.Replace("(Clone)", ""));
            forcedPanel = null;
        }
        public void RestartShift()
        {
            ClosePlanner(); CloseForced();
            Run = new CourierRun(System.Environment.TickCount) { Node = CourierStreetMap.Node("S").StopId };
            CourierContentOverrides.Apply(Run);
            Tutorial = new CourierTutorial();
            if(tutorialPanel!=null)tutorialPanel.Bind(this);
            InCar = true; GhostPosition = new Vector2(395, 330); lastState = Run.State;
            DriveBlend=RoadMotionTime=PossessionGlow=0;
            if (hud != null) hud.Bind(this);
            if (planner != null) planner.Bind(this);
            dialogue=new string[0];dialogueIndex=0;
            openingConversationPending = true;
            BeginOpeningConversation();
        }
        private void Update()
        {
            SyncTutorialPanel();
            if (!Active || GuideOpen) return;
            // Retry once gameplay and its entrance transition are ready.
            BeginOpeningConversation();
            if(Input.GetKeyDown(KeyCode.Z)){UndoMovement();return;}
            visualTime += Time.deltaTime;
            bool moving=Run.State==ShiftState.Driving||Run.State==ShiftState.Pushing;
            DriveBlend=Mathf.MoveTowards(DriveBlend,moving?1:0,Time.deltaTime*3);
            if(moving)RoadMotionTime+=Time.deltaTime*DriveBlend*(Run.State==ShiftState.Pushing?.4f:1);
            PossessionGlow=Mathf.MoveTowards(PossessionGlow,0,Time.deltaTime*2);
            if(moving&&Input.GetKeyDown(KeyCode.F)){SkipRide();return;}
            Run.Tick(Time.deltaTime*cruisePlaybackSpeed);
            SyncState();
            if(Input.GetKeyDown(KeyCode.Tab)){var routePanel=planner as CourierRoutePanel;if(routePanel!=null)routePanel.ToggleCargoDrawer();}
            if (Run.State != ShiftState.Docked && !Run.Finished) return;
            if(!ConversationActive&&!Run.Finished)
            {
                Vector2 direction=Vector2.zero;
                if(Input.GetKeyDown(KeyCode.W)||Input.GetKeyDown(KeyCode.UpArrow))direction=Vector2.up;
                if(Input.GetKeyDown(KeyCode.S)||Input.GetKeyDown(KeyCode.DownArrow))direction=Vector2.down;
                if(Input.GetKeyDown(KeyCode.A)||Input.GetKeyDown(KeyCode.LeftArrow))direction=Vector2.left;
                if(Input.GetKeyDown(KeyCode.D)||Input.GetKeyDown(KeyCode.RightArrow))direction=Vector2.right;
                if(direction!=Vector2.zero)
                {
                    var here=CourierStreetMap.Nodes[System.Array.FindIndex(CourierStreetMap.Nodes,n=>n.StopId==Run.Node)];
                    StreetNode best=null;float score=.2f;
                    foreach(var edge in CourierStreetMap.Edges)
                    {
                        if(edge.A!=here&&edge.B!=here)continue;
                        var next=edge.Other(here);var delta=new Vector2(next.Point.X-here.Point.X,here.Point.Y-next.Point.Y).normalized;
                        float alignment=Vector2.Dot(direction,delta);if(alignment>score){score=alignment;best=next;}
                    }
                    if(best!=null)DepartTo(best.StopId);
                }
            }
            if(Input.GetKeyDown(KeyCode.M))ShowPlanner("CourierRoutePanel");
            if(Input.GetKeyDown(KeyCode.E))
            {
                if(ConversationActive)AdvanceDialogue();else if(!Run.Finished)InteractAtNode();
            }
        }
        private void SyncState()
        {
            InCar=!Run.IsGhost;
            if (Run.State != lastState)
            {
                if (Run.State == ShiftState.Docked) { InCar = !Run.IsGhost; GhostPosition = new Vector2(395, 330); BeginConversation(); }
                if (Run.State == ShiftState.Encounter)
                { ClosePlanner(); forcedPanel = Open<CourierEventPanel>("CourierEventPanel", UIPanelLayer.Popup); }
                if (Run.Finished)
                { ClosePlanner(); CloseForced(); if(Run.Story!=null&&Run.Story.HasPending(Run))BeginConversation();else {dialogue=new string[0];dialogueIndex=0;} }
                lastState = Run.State;
            }
            if(Run.Finished&&!ConversationActive&&forcedPanel==null)forcedPanel=Open<CourierResultPanel>("CourierResultPanel",UIPanelLayer.Popup);
        }
        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }
        public static string Minutes(float value) { return Mathf.CeilToInt(Mathf.Max(0, value)) + " turns"; }
    }
}
