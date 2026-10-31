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
        public float DriveBlend {get;private set;}
        public float RoadMotionTime {get;private set;}
        public float PossessionGlow {get;private set;}
        public Font UIFont { get { return uiFont; } }
        public bool HasPlanner { get { return planner != null; } }
        private CourierPanel planner, forcedPanel;
        private CourierHudPanel hud;
        private CourierTutorialPanel tutorialPanel;
        private ShiftState lastState;
        private float visualTime;
        private string[] dialogue = new string[0];
        private int dialogueIndex;
        private int dialogueNode=-1;
        public bool ConversationActive { get { return dialogueIndex < dialogue.Length; } }
        public string DialogueSpeaker { get { if(!ConversationActive)return "";var line=dialogue[dialogueIndex];int colon=line.IndexOf(':');return colon>0?line.Substring(0,colon):"Narrator"; } }
        public string Subtitle
        {
            get
            {
                if (ConversationActive && (Run.State == ShiftState.Docked || Run.Finished)) return dialogue[dialogueIndex];
                if (Run.State == ShiftState.Docked) return Run.Stops[Run.Node].Name + " | " + (Run.HasJourney?"Continue to "+Run.Stops[Run.PlannedDestination].Name+"  [E: Continue]":"Click this stop or press E to talk.");
                if (Run.State == ShiftState.Pushing) return "Broken down. You are pushing to the next stop. The journey continues.";
                if (Run.State == ShiftState.Driving && Run.IsReturning) return "Returning to " + Run.Stops[Run.TravelDestination].Name;
                if (Run.State == ShiftState.Driving) return "Heading to " + Run.Stops[Run.HasJourney?Run.PlannedDestination:Run.TravelDestination].Name+(Run.HasJourney&&Run.TravelDestination!=Run.PlannedDestination?" - Next stop: "+Run.Stops[Run.TravelDestination].Name:"");
                return "";
            }
        }
        public void BeginConversation()
        {
            if ((Run.State != ShiftState.Docked && !Run.Finished) || !Active) return;
            if(ConversationActive&&dialogueNode==Run.Node)return;
            dialogueNode=Run.Node;
            dialogue = Run.TalkToNpc(); dialogueIndex = 0;
        }
        public void AdvanceDialogue()
        {
            if(!Active)return;
            if(ConversationActive&&hud!=null&&hud.CompleteSubtitle())return;
            if (ConversationActive) { dialogueIndex++; CourierSoundscape.PlayInterface("dialogue"); if(!ConversationActive){if(Run.Story!=null&&Run.Story.HasPending(Run))BeginConversation();else Tutorial.OpenMap();} }
            else if(Run.State==ShiftState.Docked&&Run.HasJourney&&Active)DepartTo(Run.PlannedDestination);
        }
        public float VisualTime { get { return visualTime; } }
        public bool Active { get { return GameManager.Instance == null || GameManager.Instance.CurrentState == GameManager.GameState.Playing; } }
        private void Awake()
        {
            Current = this;
            Run = new CourierRun();
            CourierContentOverrides.Apply(Run);
            if (uiFont == null) Debug.LogError("Assign the project font to NightCourierPrototype.");
        }
        private void Start()
        {
            planner = Open<CourierPanel>("CourierRoutePanel", UIPanelLayer.Bottom);
            hud = Open<CourierHudPanel>("CourierHudPanel", UIPanelLayer.Normal);
            lastState = Run.State;
            BeginConversation();
            SyncTutorialPanel();
            gameObject.AddComponent<CourierSoundscape>().Initialize(this, radioMusic);
        }
        private void SyncTutorialPanel()
        {
            if(Tutorial.Finished||Run.Finished)
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
            int vehicle = Run.Stops[Run.Node].Vehicle;
            if (vehicle < 0 || !Run.Possess(vehicle)) return false;
            InCar = true; PossessionGlow=1;
            return true;
        }
        public bool DepartTo(int destination)
        {
            if(ConversationActive||!Active||!Run.StartJourney(destination))return false;
            Tutorial.Observe(Run); Tutorial.Leave();
            InCar=!Run.IsGhost;ClosePlanner();return true;
        }
        public bool Depart(Road road)
        {
            // Transport mode is owned by the run, including ghost fallback.
            if (!Run.Depart(road)) return false;
            Tutorial.Observe(Run); Tutorial.Leave();
            ClosePlanner();
            return true;
        }
        public bool SkipRide()
        {
            if(!Active||!Run.FastForwardTravel())return false;
            SyncState();return true;
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
            Run = new CourierRun(System.Environment.TickCount);
            CourierContentOverrides.Apply(Run);
            InCar = true; GhostPosition = new Vector2(395, 330); lastState = Run.State;
            DriveBlend=RoadMotionTime=PossessionGlow=0;
            if (hud != null) hud.Bind(this);
            if (planner != null) planner.Bind(this);
            dialogue=new string[0];dialogueIndex=0;
            BeginConversation();
        }
        private void Update()
        {
            SyncTutorialPanel();
            if (!Active) return;
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
            if(Input.GetKeyDown(KeyCode.M))ShowPlanner("CourierRoutePanel");
            if(Input.GetKeyDown(KeyCode.E))
            {
                if(ConversationActive||Run.HasJourney)AdvanceDialogue();else if(!Run.Finished)BeginConversation();
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
