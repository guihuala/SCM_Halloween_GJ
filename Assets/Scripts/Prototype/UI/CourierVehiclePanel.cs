using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    public sealed class CourierVehiclePanel : CourierPanel
    {
        public Text currentText, currentStatsText, parkedText, parkedStatsText, repairText, feedbackText;
        public Button possessButton, repairButton;
        public CourierVehicleIconView currentArtwork, parkedArtwork;
        public void PrepareVehicleArtwork()
        {
            currentArtwork=CourierVehicleIconView.Ensure(currentText.transform.parent,"CurrentVehicleArtwork",new Vector2(40,-132),new Vector2(72,72));
            parkedArtwork=CourierVehicleIconView.Ensure(parkedText.transform.parent,"ParkedVehicleArtwork",new Vector2(600,-132),new Vector2(72,72));
            currentText.rectTransform.anchoredPosition=new Vector2(128,-138);currentText.rectTransform.sizeDelta=new Vector2(412,55);
            parkedText.rectTransform.anchoredPosition=new Vector2(688,-138);parkedText.rectTransform.sizeDelta=new Vector2(412,55);
        }
        protected override void Awake()
        {
            base.Awake();PrepareVehicleArtwork();
            possessButton.onClick.AddListener(() => feedbackText.text = owner.PossessParkedVehicle() ? "Possessed" : "Vehicle unavailable at this stop");
            repairButton.onClick.AddListener(() => { if (Run.Repair()) feedbackText.text = "Repaired"; });
        }
        protected override void Refresh()
        {
            if(currentArtwork==null||parkedArtwork==null)PrepareVehicleArtwork();
            currentArtwork.SetVehicle(Run.VehicleIndex);
            currentText.text = Run.Car.Name;
            currentStatsText.text = Run.IsGhost ? Run.Car.Ability : Stats(Run.VehicleIndex);
            int v = Run.Stops[Run.Node].Vehicle;
            bool available = v >= 0;
            parkedArtwork.gameObject.SetActive(available);if(available)parkedArtwork.SetVehicle(v);
            parkedText.text = available ? Run.Vehicles[v].Name : "No vehicle available";
            parkedStatsText.text = available ? Stats(v) : "";
            possessButton.gameObject.SetActive(available);
            possessButton.interactable = available && Run.CanPossess(v);
            Caption(possessButton, "Possess ");
            bool repair = !Run.IsGhost && Run.Stops[Run.Node].Repair;
            repairButton.gameObject.SetActive(repair);
            repairButton.interactable = repair && Run.Durability < Run.Car.MaxDurability;
            repairText.gameObject.SetActive(!repair);
            repairText.text = "Exchange at H, W or Q: +1 order turn; fresh 5 vehicle turns";
            feedbackText.gameObject.SetActive(!string.IsNullOrEmpty(feedbackText.text));
        }
        private string Stats(int index)
        {
            var car = Run.Vehicles[index];
            return "Vehicle turns " + Mathf.CeilToInt(Run.Durabilities[index]) + "/" + car.MaxDurability +
                "\n\nAll order batches can be carried. " + car.Ability;
        }
    }
}
