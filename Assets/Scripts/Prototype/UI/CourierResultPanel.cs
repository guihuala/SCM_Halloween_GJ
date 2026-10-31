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
            titleText.text="All Orders Delivered!";
            summaryText.resizeTextForBestFit=false;summaryText.fontSize=24;summaryText.lineSpacing=1;summaryText.alignment=UnityEngine.TextAnchor.UpperLeft;
            summaryText.text="Completed "+Run.CompletedBatches+"/"+Run.BatchCount+" orders - "+Run.Delivered+" cargo items\n\n"+
                string.Join("\n",Run.Results.Select((r,i)=>(i+1)+". "+Run.Stops[r.Origin].Sign+" > "+Run.Stops[r.Destination].Sign+" - "+r.Turns+"/"+r.TargetTurns+" turns - "+r.Evaluation));
        }
    }
}
