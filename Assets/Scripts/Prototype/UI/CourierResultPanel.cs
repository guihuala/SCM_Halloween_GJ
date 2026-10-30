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
            titleText.text=Run.State==ShiftState.EndingA?"Ending A - Perfect Delivery":"Ending B - Road Keeper";
            summaryText.text=(Run.State==ShiftState.EndingA?"Your parcel scatters into light, bound for the world of the living.\nAt last, you can let this night go.":"Dawn breaks, and you choose to stay on Route Zero.\nThe next lost courier will find you in the glow.")+
                "\n\nSide deliveries "+Run.Parcels.Count(p=>!p.Critical&&p.State==ParcelState.Delivered)+"/4  ·  Elapsed "+NightCourierPrototype.Minutes(Run.Elapsed);
        }
    }
}
