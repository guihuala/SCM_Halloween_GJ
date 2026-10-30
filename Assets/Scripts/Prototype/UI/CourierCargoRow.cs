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
            destinationText.text = "送往 " + run.Stops[parcel.Destination].Name + "   ·   重量 " + parcel.Weight + " 格   ·   酬金 ¥" + parcel.Reward;
            string tags = parcel.Damaged ? "已破损，到站后将被拒收" : parcel.Tags.Length > 0 ? parcel.Tags : "普通快递 · 无特殊要求";
            if (parcel.Deadline > 0 && !parcel.Damaged) tags += "  剩余 " + NightCourierPrototype.Minutes(parcel.Deadline - run.Elapsed);
            requirementsText.text = tags;
            bool loaded = parcel.State == ParcelState.Loaded;
            actionButton.interactable = loaded ? run.State == ShiftState.Docked && parcel.Origin == run.Node : run.CanLoad(parcel);
            actionLabel.text = loaded ? (parcel.Origin == run.Node ? "卸回站点" : "运送中") : parcel.Heavy && run.VehicleIndex == 0 ? "需要皮卡" : !run.CanLoad(parcel) ? "容量不足" : "装上车";
        }
    }
}
