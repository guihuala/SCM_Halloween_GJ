using UnityEngine;
using UnityEngine.UI;
namespace NightCourier
{
    public sealed class CourierHudPanel : CourierPanel
    {
        public Text subtitleText,timeText;
        public Image progressFill;
        public GameObject roadStatus;
        public Button advanceButton;
        public CourierStageGraphic stageGraphic;
        protected override void Awake(){base.Awake();advanceButton.onClick.AddListener(()=>owner.AdvanceDialogue());}
        public override void Bind(NightCourierPrototype prototype){stageGraphic.Owner=prototype;base.Bind(prototype);}
        protected override void Refresh()
        {
            bool driving=Run.State==ShiftState.Driving||Run.State==ShiftState.Pushing;
            roadStatus.SetActive(driving);
            timeText.text=Run.ActiveRoad!=null&&Run.ActiveRoad.Kind==RoadKind.Storm ? Mathf.CeilToInt(Mathf.Max(0,Run.FinalDeadline-Run.Elapsed))+" min" : Run.Late?"Dawn":Mathf.CeilToInt(Mathf.Max(0,CourierRun.ShiftMinutes-Run.Elapsed))+" min";
            progressFill.fillAmount=Run.Progress;
            subtitleText.text=owner.HasPlanner?"":owner.Subtitle;
            advanceButton.GetComponent<CourierIconHint>().message=owner.ConversationActive?"Continue dialogue - E":"Continue to "+(Run.HasJourney?Run.Stops[Run.PlannedDestination].Name:"")+" · E";
            advanceButton.gameObject.SetActive((owner.ConversationActive||Run.HasJourney)&&!owner.HasPlanner&&Run.State==ShiftState.Docked);
        }
    }
}
