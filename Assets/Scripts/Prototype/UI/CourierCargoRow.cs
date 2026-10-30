using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    public sealed class CourierCargoRow : MonoBehaviour
    {
        public Text nameText, destinationText, requirementsText, actionLabel;
        public Button actionButton;
        private Parcel parcel;
        private NightCourierPrototype owner;
        public void Bind(NightCourierPrototype prototype, Parcel item)
        {
            owner = prototype; parcel = item;
            foreach (var text in GetComponentsInChildren<Text>(true)) text.font = owner.UIFont;
            actionButton.onClick.AddListener(() => { if (parcel.State == ParcelState.Loaded) owner.Run.Unload(parcel); else owner.Run.LoadParcel(parcel); });
        }
        public void Refresh()
        {
            var run = owner.Run;
            nameText.text = parcel.Name;
            destinationText.text = "→ " + run.Stops[parcel.Destination].Name + " · " + parcel.Weight + "格";
            string tags = parcel.Damaged ? "已破损" : (parcel.Fragile ? "易碎  " : "") + (parcel.Heavy ? "重货  " : "");
            if (parcel.Deadline > 0 && !parcel.Damaged) tags += "限时 " + NightCourierPrototype.Minutes(parcel.Deadline - run.Elapsed);
            requirementsText.text = tags.Trim();
            requirementsText.gameObject.SetActive(tags.Length > 0);
            bool loaded = parcel.State == ParcelState.Loaded;
            actionButton.interactable = loaded ? run.State == ShiftState.Docked && parcel.Origin == run.Node : run.CanLoad(parcel);
            actionButton.gameObject.SetActive(!loaded || parcel.Origin == run.Node);
            actionLabel.text = loaded ? (parcel.Origin == run.Node ? "卸下" : "运送中") : parcel.Heavy && run.VehicleIndex == 0 ? "需皮卡" : !run.CanLoad(parcel) ? "放不下" : "装车";
        }
    }
}
