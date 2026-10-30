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
            possessButton.onClick.AddListener(() => feedbackText.text = owner.PossessParkedVehicle() ? "Possessed" : "Cargo load exceeds capacity");
            repairButton.onClick.AddListener(() => { if (Run.Repair()) feedbackText.text = "Repaired"; });
        }
        protected override void Refresh()
        {
            currentText.text = Run.Car.Name;
            currentStatsText.text = Stats(Run.VehicleIndex);
            int v = Run.Stops[Run.Node].Vehicle;
            bool available = v >= 0 && v != Run.VehicleIndex;
            parkedText.text = available ? Run.Vehicles[v].Name : "No vehicle available";
            parkedStatsText.text = available ? Stats(v) : "";
            possessButton.gameObject.SetActive(available);
            possessButton.interactable = available && Run.Load <= Run.Vehicles[v].Capacity + 2;
            Caption(possessButton, "Possess ");
            bool repair = Run.Stops[Run.Node].Repair;
            repairButton.gameObject.SetActive(repair);
            repairButton.interactable = repair && Run.Durability < Run.Car.MaxDurability;
            repairText.gameObject.SetActive(!repair);
            repairText.text = "Repairs available at the junkyard and ferry";
            feedbackText.gameObject.SetActive(!string.IsNullOrEmpty(feedbackText.text));
        }
        private string Stats(int index)
        {
            var car = Run.Vehicles[index];
            return "Capacity " + car.Capacity + " slots - Durability " + Mathf.CeilToInt(Run.Durabilities[index]) + "/" + car.MaxDurability +
                "\n\n" + new[] { "Cannot carry heavy cargo", "Carries heavy cargo", "Protects fragile cargo on gravel" }[index];
        }
    }
}
