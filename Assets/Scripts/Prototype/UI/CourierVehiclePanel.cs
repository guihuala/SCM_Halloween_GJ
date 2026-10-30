using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    public sealed class CourierVehiclePanel : CourierPanel
    {
        public Text currentText, currentStatsText, parkedText, parkedStatsText, repairText, feedbackText;
        public Button possessButton, repairButton;
        protected override void Awake()
        {
            base.Awake();
            possessButton.onClick.AddListener(() => feedbackText.text = owner.PossessParkedVehicle() ? "已附身" : "载重不符");
            repairButton.onClick.AddListener(() => { if (Run.Repair()) feedbackText.text = "已修复"; });
        }
        protected override void Refresh()
        {
            currentText.text = Run.Car.Name;
            currentStatsText.text = Stats(Run.VehicleIndex);
            int v = Run.Stops[Run.Node].Vehicle;
            bool available = v >= 0 && v != Run.VehicleIndex;
            parkedText.text = available ? Run.Vehicles[v].Name : "无可附身车辆";
            parkedStatsText.text = available ? Stats(v) : "";
            possessButton.gameObject.SetActive(available);
            possessButton.interactable = available && Run.Load <= Run.Vehicles[v].Capacity + 2;
            Caption(possessButton, "附身");
            bool repair = Run.Stops[Run.Node].Repair;
            repairButton.gameObject.SetActive(repair);
            repairButton.interactable = repair && Run.Durability < Run.Car.MaxDurability;
            repairText.gameObject.SetActive(!repair);
            repairText.text = "废车场、加油站可维修";
            feedbackText.gameObject.SetActive(!string.IsNullOrEmpty(feedbackText.text));
        }
        private string Stats(int index)
        {
            var car = Run.Vehicles[index];
            return "容量 " + car.Capacity + "格 · 耐久 " + Mathf.CeilToInt(Run.Durabilities[index]) + "/" + car.MaxDurability +
                "\n\n" + new[] { "不能拉重货", "可拉重货", "碎石路保护易碎件" }[index];
        }
    }
}
