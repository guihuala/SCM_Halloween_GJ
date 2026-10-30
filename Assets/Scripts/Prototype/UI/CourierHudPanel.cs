using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    public sealed class CourierHudPanel : CourierPanel
    {
        [Header("顶部状态")]
        public Text locationText, timeText, conditionText, goalText;
        [Header("当前任务与提示")]
        public Text instructionText, feedbackText, controlsText, stageSignText, npcText, parkedText;
        public Button cargoButton, routeButton, vehicleButton, ghostButton, pauseButton;
        public CourierStageGraphic stageGraphic;
        protected override void Awake()
        {
            base.Awake();
            cargoButton.onClick.AddListener(() => owner.ShowPlanner("CourierCargoPanel"));
            routeButton.onClick.AddListener(() => owner.ShowPlanner("CourierRoutePanel"));
            vehicleButton.onClick.AddListener(() => owner.ShowPlanner("CourierVehiclePanel"));
            ghostButton.onClick.AddListener(() => owner.ToggleCar());
            pauseButton.onClick.AddListener(() => GameManager.Instance.PauseGame());
        }
        public override void Bind(NightCourierPrototype prototype) { stageGraphic.Owner = prototype; base.Bind(prototype); }
        protected override void Refresh()
        {
            bool docked = Run.State == ShiftState.Docked;
            locationText.text = docked ? "停靠站 · " + Run.Stops[Run.Node].Name : "公路巡航中";
            timeText.text = "天亮还剩\n" + NightCourierPrototype.Minutes(CourierRun.ShiftMinutes - Run.Elapsed);
            conditionText.text = "车辆耐久\n" + Mathf.CeilToInt(Run.Durability) + " / " + Run.Car.MaxDurability;
            goalText.text = "送达目标\n" + Run.Delivered + " / " + CourierRun.Target + " 件";
            bool carrying = Run.Parcels.Any(p => p.State == ParcelState.Loaded);
            instructionText.text = docked ? (carrying ? "下一步：打开「路线与出发」，选择收件站。" : "下一步：打开「接货与货舱」，选择要送的快递。") :
                Run.State == ShiftState.Encounter ? "遇到公路事件，选择一种处理方式。" : "正在赶往下一站 · 行驶进度 " + Mathf.RoundToInt(Run.Progress * 100) + "%";
            feedbackText.text = Run.Journal.Count > 0 ? Run.Journal[0] : "";
            controlsText.text = docked ? (owner.InCar ? "下车后可操控幽灵，与委托人或废弃车辆互动。" : "WASD 飘移 · E 交互 · Tab 返回车辆") : "车辆自动巡航，时间与耐久持续消耗。";
            stageSignText.text = docked ? Run.Stops[Run.Node].Name : (Run.ActiveRoad != null ? (Run.ActiveRoad.Gravel ? "碎石路 · 易碎件注意颠簸" : "高速公路 · 平稳路面") : "");
            npcText.gameObject.SetActive(docked); parkedText.gameObject.SetActive(docked && Run.Stops[Run.Node].Vehicle >= 0);
            parkedText.text = docked && Run.Stops[Run.Node].Vehicle >= 0 ? "可附身：" + Run.Vehicles[Run.Stops[Run.Node].Vehicle].Name : "";
            cargoButton.interactable = routeButton.interactable = vehicleButton.interactable = ghostButton.interactable = docked;
            Caption(ghostButton, owner.InCar ? "下车探索 · Tab" : "返回车辆 · Tab");
        }
    }
}
