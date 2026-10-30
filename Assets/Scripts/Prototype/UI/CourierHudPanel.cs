using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    public sealed class CourierHudPanel : CourierPanel
    {
        public Text locationText, timeText, conditionText, goalText, controlsText;
        public Button cargoButton, routeButton, vehicleButton, pauseButton;
        public CourierStageGraphic stageGraphic;
        protected override void Awake()
        {
            base.Awake();
            cargoButton.onClick.AddListener(() => owner.ShowPlanner("CourierCargoPanel"));
            routeButton.onClick.AddListener(() => owner.ShowPlanner("CourierRoutePanel"));
            vehicleButton.onClick.AddListener(() => owner.ShowPlanner("CourierVehiclePanel"));
            pauseButton.onClick.AddListener(() => GameManager.Instance.PauseGame());
        }
        public override void Bind(NightCourierPrototype prototype) { stageGraphic.Owner = prototype; base.Bind(prototype); }
        protected override void Refresh()
        {
            bool docked = Run.State == ShiftState.Docked;
            locationText.text = docked ? Run.Stops[Run.Node].Name : "行驶中  " + Mathf.RoundToInt(Run.Progress * 100) + "%";
            timeText.text = "剩余 " + NightCourierPrototype.Minutes(CourierRun.ShiftMinutes - Run.Elapsed);
            conditionText.text = "耐久 " + Mathf.CeilToInt(Run.Durability) + "/" + Run.Car.MaxDurability;
            goalText.text = "送达 " + Run.Delivered + "/" + CourierRun.Target;
            controlsText.text = owner.InCar ? "Tab 下车" : "WASD 移动 · E 交互 · Tab 上车";
            controlsText.gameObject.SetActive(docked);
            cargoButton.gameObject.SetActive(docked);
            routeButton.gameObject.SetActive(docked);
            vehicleButton.gameObject.SetActive(docked && (Run.Stops[Run.Node].Repair || Run.Stops[Run.Node].Vehicle >= 0));
        }
    }
}
