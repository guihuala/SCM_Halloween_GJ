using UnityEngine.UI;
namespace NightCourier
{
    public sealed class CourierEventPanel : CourierPanel
    {
        public Text titleText,descriptionText,safeCostText,riskCostText;
        public Button safeButton,riskButton;
        public CourierIconGraphic fragileRisk,dryRisk;
        protected override void Awake(){base.Awake();safeButton.onClick.AddListener(()=>owner.ResolveEncounter(true));riskButton.onClick.AddListener(()=>owner.ResolveEncounter(false));}
        protected override void Refresh()
        {
            var e=Run.Events[Run.EncounterIndex];titleText.text=e.Title;descriptionText.text=e.Description;
            Caption(safeButton,e.Safe.Split('·')[0].Trim());Caption(riskButton,e.Risky.Split('·')[0].Trim());
            var safeHint=safeButton.GetComponent<CourierIconHint>();var riskHint=riskButton.GetComponent<CourierIconHint>();
            if(safeHint!=null)safeHint.message="Take the safe option\n"+Run.SafeChoice+". Protects cargo from this event.";
            if(riskHint!=null)riskHint.message="Take the risky option\n"+Run.RiskChoice;
            safeCostText.text="+"+e.SafeMinutes+" min";riskCostText.text="-"+e.RiskDamage;
            fragileRisk.gameObject.SetActive(e.BreakFragile);dryRisk.gameObject.SetActive(e.WetDry);
        }
    }
}
