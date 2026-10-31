using System.Linq;
using UnityEngine.UI;
namespace NightCourier
{
    public sealed class CourierResultPanel : CourierPanel
    {
        public Text titleText,summaryText;
        public Button restartButton,menuButton;
        protected override void Awake(){base.Awake();restartButton.onClick.AddListener(()=>owner.RestartShift());menuButton.onClick.AddListener(()=>GameManager.Instance.ReturnToMainMenu());}
        protected override void Refresh()
        {
            titleText.text=Run.State==ShiftState.EndingA?"Party Ready!":"Together at Dawn";
            summaryText.text=(Run.State==ShiftState.EndingA?"The theme tune plays. Mead, flowers and warm food await.\nEvery delivery arrived safely for the Halloween party.":"Some deliveries did not make it through the night.\nThe neighbours gather anyway. There is a place for you at the party.")+
                "\n\nGhost Courier is always with you.\nDeliveries "+Run.Parcels.Count(p=>p.State==ParcelState.Delivered)+"/"+Run.Parcels.Count+"  ·  Elapsed "+NightCourierPrototype.Minutes(Run.Elapsed);
        }
    }
}
