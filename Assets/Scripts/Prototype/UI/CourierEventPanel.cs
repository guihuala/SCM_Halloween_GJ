using UnityEngine.UI;
namespace NightCourier
{
    public sealed class CourierEventPanel : CourierPanel
    {
        public Text titleText, descriptionText;
        public Button safeButton, riskButton;
        protected override void Awake()
        {
            base.Awake();
            safeButton.onClick.AddListener(() => owner.ResolveEncounter(true));
            riskButton.onClick.AddListener(() => owner.ResolveEncounter(false));
        }
        protected override void Refresh()
        {
            titleText.text = Run.EncounterTitle;
            descriptionText.text = Run.EncounterDescription + "\n\n选择期间不消耗时间。";
            Caption(safeButton, Run.SafeChoice); Caption(riskButton, Run.RiskChoice);
        }
    }
}
