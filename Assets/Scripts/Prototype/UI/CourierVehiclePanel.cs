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
            possessButton.onClick.AddListener(() => feedbackText.text = owner.PossessParkedVehicle() ? "附身成功！货物已随你转移。" : "无法附身：请检查载重与重货要求。");
            repairButton.onClick.AddListener(() => { if (Run.Repair()) feedbackText.text = "维修完成，消耗12分钟。"; });
        }
        protected override void Refresh()
        {
            currentText.text = "当前载具 · " + Run.Car.Name;
            currentStatsText.text = "载重容量  " + Run.Car.Capacity + " 格\n当前耐久  " + Mathf.CeilToInt(Run.Durability) + " / " + Run.Car.MaxDurability + "\n\n" + Run.Car.Ability;
            int v = Run.Stops[Run.Node].Vehicle;
            bool available = v >= 0 && v != Run.VehicleIndex;
            parkedText.text = available ? "路边发现 · " + Run.Vehicles[v].Name : "本站没有其他可附身载具";
            parkedStatsText.text = available ? "载重容量  " + Run.Vehicles[v].Capacity + " 格\n当前耐久  " + Mathf.CeilToInt(Run.Durabilities[v]) + " / " + Run.Vehicles[v].MaxDurability + "\n\n" + Run.Vehicles[v].Ability : "废车场有旧皮卡。\n南山墓园有重型货车。\n\n附身会转移货物，保留各车的耐久。";
            possessButton.interactable = available && Run.Load <= Run.Vehicles[v].Capacity + 2;
            Caption(possessButton, available ? "附身" + Run.Vehicles[v].Name : "暂无可附身车辆");
            repairButton.interactable = Run.Stops[Run.Node].Repair && Run.Durability < Run.Car.MaxDurability;
            repairText.text = Run.Stops[Run.Node].Repair ? "本站提供免费维修，耗时12分钟。" : "此站没有维修服务。废车场与加油站可修车。";
        }
    }
}
