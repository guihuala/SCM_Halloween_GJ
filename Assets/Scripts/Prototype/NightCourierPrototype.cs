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
        private ShiftState lastState;
        private float visualTime;
        private string[] dialogue = new string[0];
        private int dialogueIndex;
        public bool ConversationActive { get { return dialogueIndex < dialogue.Length; } }
        public string Subtitle
        {
            get
            {
                if (Run.State == ShiftState.Docked) return ConversationActive ? dialogue[dialogueIndex] : Run.Stops[Run.Node].Name + " | " + (Run.HasJourney?"Continue to "+Run.Stops[Run.PlannedDestination].Name+"  [E: Continue]":"Click this stop or press E to talk.");
                if (Run.State == ShiftState.Pushing) return "Broken down. You are pushing to the next stop. The journey continues.";
                if (Run.State == ShiftState.Driving) return "Heading to " + Run.Stops[Run.HasJourney?Run.PlannedDestination:Run.ActiveRoad.Other(Run.Node)].Name+(Run.HasJourney&&Run.ActiveRoad.Other(Run.Node)!=Run.PlannedDestination?" - Next stop: "+Run.Stops[Run.ActiveRoad.Other(Run.Node)].Name:"");
                return "";
            }
        }
        public void BeginConversation()
        {
            if (Run.State != ShiftState.Docked || !Active) return;
            dialogue = Run.TalkToNpc(); dialogueIndex = 0;
        }
        public void AdvanceDialogue()
        {
            if (ConversationActive) { dialogueIndex++; if(!ConversationActive)Tutorial.OpenMap(); }
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
            var previousHud = FindObjectOfType<HudController>();
            if (previousHud != null && previousHud.pauseButton != null) previousHud.pauseButton.gameObject.SetActive(false);
            planner = Open<CourierPanel>("CourierRoutePanel", UIPanelLayer.Bottom);
            hud = Open<CourierHudPanel>("CourierHudPanel", UIPanelLayer.Normal);
            lastState = Run.State;
            BeginConversation();
            gameObject.AddComponent<CourierSoundscape>().Initialize(this, radioMusic);
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
            Tutorial.Observe(Run);if(!ConversationActive)Tutorial.OpenMap();
            if(planner==null)planner=Open<CourierPanel>("CourierRoutePanel",UIPanelLayer.Bottom);
        }
        // The map is the main play surface, so Esc now goes straight to pause.
        public bool ClosePlanner(){return false;}
        public void ToggleCar()
        {
            if (Run.State != ShiftState.Docked || !Active) return;
            InCar = !InCar;
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
            if(!Run.StartJourney(destination))return false;
            Tutorial.Observe(Run); Tutorial.Leave();
            InCar=true;ClosePlanner();return true;
        }
        public bool Depart(Road road)
        {
            if (!InCar) ToggleCar();
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
            BeginConversation();
        }
        private void Update()
        {
            if (!Active) return;
            visualTime += Time.deltaTime;
            bool moving=Run.State==ShiftState.Driving||Run.State==ShiftState.Pushing;
            DriveBlend=Mathf.MoveTowards(DriveBlend,moving?1:0,Time.deltaTime*3);
            if(moving)RoadMotionTime+=Time.deltaTime*DriveBlend*(Run.State==ShiftState.Pushing?.4f:1);
            PossessionGlow=Mathf.MoveTowards(PossessionGlow,0,Time.deltaTime*2);
            if(moving&&Input.GetKeyDown(KeyCode.F)){SkipRide();return;}
            Run.Tick(Time.deltaTime*cruisePlaybackSpeed);
            SyncState();
            if (Run.State != ShiftState.Docked) return;
            if(Input.GetKeyDown(KeyCode.Tab)||Input.GetKeyDown(KeyCode.M))ShowPlanner("CourierRoutePanel");
            if(Input.GetKeyDown(KeyCode.E))
            {
                if(ConversationActive||Run.HasJourney)AdvanceDialogue();else BeginConversation();
            }
        }
        private void SyncState()
        {
            if (Run.State != lastState)
            {
                if (Run.State == ShiftState.Docked) { InCar = true; GhostPosition = new Vector2(395, 330); BeginConversation(); }
                if (Run.State == ShiftState.Encounter)
                { ClosePlanner(); forcedPanel = Open<CourierEventPanel>("CourierEventPanel", UIPanelLayer.Popup); }
                if (Run.Finished)
                { ClosePlanner(); CloseForced(); forcedPanel = Open<CourierResultPanel>("CourierResultPanel", UIPanelLayer.Popup); }
                lastState = Run.State;
            }
        }
        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }
        public static string Minutes(float value) { return Mathf.CeilToInt(Mathf.Max(0, value)) + " min"; }
    }
}
