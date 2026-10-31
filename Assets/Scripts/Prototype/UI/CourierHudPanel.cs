using UnityEngine;
using UnityEngine.UI;
namespace NightCourier
{
    public sealed class CourierHudPanel : CourierPanel
    {
        public Text subtitleText,timeText;
        public Image progressFill;
        public GameObject roadStatus;
        public Button advanceButton,skipRideButton;
        public CourierStageGraphic stageGraphic;
        protected override void Awake(){base.Awake();advanceButton.onClick.AddListener(()=>owner.AdvanceDialogue());if(skipRideButton!=null)skipRideButton.onClick.AddListener(()=>owner.SkipRide());}
        public override void Bind(NightCourierPrototype prototype){if(stageGraphic!=null){stageGraphic.gameObject.SetActive(false);stageGraphic.Owner=prototype;}base.Bind(prototype);}
        protected override void Refresh()
        {
            bool driving=Run.State==ShiftState.Driving||Run.State==ShiftState.Pushing;
            roadStatus.SetActive(!Run.Finished);
            bool travelling=driving||Run.State==ShiftState.Encounter;
            progressFill.gameObject.SetActive(travelling);
            var track=roadStatus.transform.Find("ProgressFillBackground");if(track!=null)track.gameObject.SetActive(travelling);
            if(skipRideButton!=null)skipRideButton.gameObject.SetActive(driving&&owner.Active);
            timeText.text=Run.Late?"Dawn":Mathf.CeilToInt(Mathf.Max(0,Run.NightMinutes-Run.Elapsed))+" min";
            progressFill.fillAmount=Run.Progress;
            subtitleText.text=owner.Subtitle;
            advanceButton.GetComponent<CourierIconHint>().message=owner.ConversationActive?"Continue dialogue - E":"Continue to "+(Run.HasJourney?Run.Stops[Run.PlannedDestination].Name:"")+" · E";
            advanceButton.gameObject.SetActive((owner.ConversationActive||Run.HasJourney)&&Run.State==ShiftState.Docked);
        }
    }
}
