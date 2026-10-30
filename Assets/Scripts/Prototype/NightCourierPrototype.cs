using UnityEngine;

namespace NightCourier
{
    // The simulation is scene-local; all UI is now editable uGUI prefabs.
    public sealed class NightCourierPrototype : MonoBehaviour
    {
        public static NightCourierPrototype Current { get; private set; }
        public CourierRun Run { get; private set; }
        public bool InCar { get; private set; } = true;
        public Vector2 GhostPosition { get; private set; } = new Vector2(395, 330);
        [SerializeField] private Font uiFont;
        public Font UIFont { get { return uiFont != null ? uiFont : fallbackFont; } }
        public bool HasPlanner { get { return planner != null; } }
        private Font fallbackFont;
        private CourierPanel planner, forcedPanel;
        private CourierHudPanel hud;
        private ShiftState lastState;
        private float visualTime;
        public float VisualTime { get { return visualTime; } }
        public bool Active { get { return GameManager.Instance == null || GameManager.Instance.CurrentState == GameManager.GameState.Playing; } }
        private void Awake()
        {
            Current = this;
            Run = new CourierRun();
            if (uiFont == null) fallbackFont = Font.CreateDynamicFontFromOSFont(new[] { "PingFang SC", "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 24);
        }
        private void Start()
        {
            var previousHud = FindObjectOfType<HudController>();
            if (previousHud != null && previousHud.pauseButton != null) previousHud.pauseButton.gameObject.SetActive(false);
            hud = Open<CourierHudPanel>("CourierHudPanel", UIPanelLayer.Bottom);
            lastState = Run.State;
        }
        private T Open<T>(string name, UIPanelLayer layer) where T : CourierPanel
        {
            var p = UIManager.Instance.OpenPanel(name, null, layer) as T;
            if (p != null) p.Bind(this);
            return p;
        }
        public void ShowPlanner(string name)
        {
            if (!Active || Run.State != ShiftState.Docked) return;
            ClosePlanner();
            planner = Open<CourierPanel>(name, UIPanelLayer.Popup);
        }
        public bool ClosePlanner()
        {
            if (planner == null) return false;
            UIManager.Instance.ClosePanel(planner.gameObject.name.Replace("(Clone)", ""));
            planner = null;
            return true;
        }
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
            InCar = true;
            return true;
        }
        public bool Depart(Road road)
        {
            if (!InCar) ToggleCar();
            if (!Run.Depart(road)) return false;
            ClosePlanner();
            return true;
        }
        public void ResolveEncounter(bool safe)
        {
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
            InCar = true; GhostPosition = new Vector2(395, 330); lastState = Run.State;
            if (hud != null) hud.Bind(this);
        }
        private void Update()
        {
            if (!Active) return;
            visualTime += Time.deltaTime;
            Run.Tick(Time.deltaTime);
            if (Run.State != lastState)
            {
                if (Run.State == ShiftState.Docked) { InCar = true; GhostPosition = new Vector2(395, 330); }
                if (Run.State == ShiftState.Encounter)
                { ClosePlanner(); forcedPanel = Open<CourierEventPanel>("CourierEventPanel", UIPanelLayer.Popup); }
                if (Run.State == ShiftState.Won || Run.State == ShiftState.Lost)
                { ClosePlanner(); CloseForced(); forcedPanel = Open<CourierResultPanel>("CourierResultPanel", UIPanelLayer.Popup); }
                lastState = Run.State;
            }
            if (Run.State != ShiftState.Docked || HasPlanner) return;
            if (Input.GetKeyDown(KeyCode.M)) { ShowPlanner("CourierRoutePanel"); return; }
            if (Input.GetKeyDown(KeyCode.Tab)) ToggleCar();
            if (InCar) return;
            GhostPosition = new Vector2(Mathf.Clamp(GhostPosition.x + Input.GetAxisRaw("Horizontal") * Time.deltaTime * 320, 180, 1230),
                Mathf.Clamp(GhostPosition.y - Input.GetAxisRaw("Vertical") * Time.deltaTime * 150, 270, 370));
            if (!Input.GetKeyDown(KeyCode.E)) return;
            if (Mathf.Abs(GhostPosition.x - 790) < 115) ShowPlanner("CourierCargoPanel");
            else if (Mathf.Abs(GhostPosition.x - 1080) < 110 && Run.Stops[Run.Node].Vehicle >= 0) ShowPlanner("CourierVehiclePanel");
            else if (Mathf.Abs(GhostPosition.x - 395) < 130) ToggleCar();
        }
        private void OnDestroy()
        {
            if (Current == this) Current = null;
            if (fallbackFont != null) Destroy(fallbackFont);
        }
        public static string Minutes(float value) { return Mathf.CeilToInt(Mathf.Max(0, value)) + " 分钟"; }
    }
}
