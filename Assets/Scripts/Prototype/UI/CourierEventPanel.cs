using UnityEngine;
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
            Caption(safeButton,e.Safe.Split('·')[0].Trim());
            riskButton.gameObject.SetActive(false);
            var buttonRect=(RectTransform)safeButton.transform;
            buttonRect.anchoredPosition=new Vector2(buttonRect.anchoredPosition.x,-284);
            descriptionText.rectTransform.sizeDelta=new Vector2(descriptionText.rectTransform.sizeDelta.x,190);
            var label=safeButton.GetComponentInChildren<Text>();
            label.rectTransform.sizeDelta=new Vector2(776,label.rectTransform.sizeDelta.y);
            var safeHint=safeButton.GetComponent<CourierIconHint>();
            if(safeHint!=null)safeHint.message=Run.SafeChoice;
            // Use the full description width for the outcome; the old cost badge is too narrow.
            descriptionText.text=e.Description+"\n\n"+Run.EventCost+".";
            safeCostText.text="";riskCostText.text="";
            var clock=safeButton.transform.Find("ClockIcon");if(clock!=null)clock.gameObject.SetActive(false);
            fragileRisk.gameObject.SetActive(false);dryRisk.gameObject.SetActive(false);
        }
    }
}
