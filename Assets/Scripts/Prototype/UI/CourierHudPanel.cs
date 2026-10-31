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
        [Header("Dialogue Speaker")]
        public CourierCharacterCatalog characterCatalog;
        public Image speakerPortrait, portraitFrame;
        public Text speakerName;
        private CourierTextJuice subtitleJuice;
        private void RefreshSpeaker()
        {
            if(characterCatalog==null)characterCatalog=Resources.Load<CourierCharacterCatalog>("NightVoyage/Characters");
            bool talking=owner.ConversationActive&&(Run.State==ShiftState.Docked||Run.Finished);
            var character=talking&&characterCatalog!=null?characterCatalog.ForStop(CourierStory.SpeakerStop(owner.DialogueSpeaker)):null;
            bool show=character!=null&&speakerPortrait!=null&&speakerName!=null;
            if(speakerPortrait!=null)
            {
                speakerPortrait.gameObject.SetActive(show);
                if(show){speakerPortrait.sprite=characterCatalog.PortraitFor(character);speakerPortrait.enabled=speakerPortrait.sprite!=null;}
            }
            if(portraitFrame!=null){portraitFrame.gameObject.SetActive(show);if(show)portraitFrame.color=character.accent;}
            if(speakerName!=null){speakerName.gameObject.SetActive(talking);if(talking){speakerName.text=owner.DialogueSpeaker;speakerName.color=character!=null?character.accent:Color.white;}}
            var bar=subtitleText.transform.parent.Find("SubtitleBar") as RectTransform;
            if(bar!=null){bar.gameObject.SetActive(talking);bar.anchoredPosition=new Vector2(0,-810);bar.sizeDelta=new Vector2(1800,190);}
            subtitleText.gameObject.SetActive(talking);
            subtitleText.rectTransform.anchoredPosition=talking?new Vector2(416,-867):new Vector2(40,-944);
            subtitleText.rectTransform.sizeDelta=talking?new Vector2(1224,110):new Vector2(1600,40);
            subtitleText.fontSize=talking?30:26;
            var advanceRect=(RectTransform)advanceButton.transform;
            advanceRect.anchoredPosition=new Vector2(advanceRect.anchoredPosition.x,talking?-873:-936);
            if(skipRideButton!=null)
            {
                var skipRect=(RectTransform)skipRideButton.transform;var root=(RectTransform)transform;
                skipRect.position=root.TransformPoint(new Vector3(root.rect.center.x-84,root.rect.yMax-24,0));
            }
        }

        private void PresentSubtitle()
        {
            RefreshSpeaker();
            if(subtitleJuice==null)subtitleJuice=subtitleText.GetComponent<CourierTextJuice>()??subtitleText.gameObject.AddComponent<CourierTextJuice>();
            subtitleJuice.Present(owner.Subtitle,owner.ConversationActive&&(Run.State==ShiftState.Docked||Run.Finished));
        }
        public bool CompleteSubtitle()
        {
            PresentSubtitle();
            return subtitleJuice.Complete();
        }
        protected override void Awake(){base.Awake();advanceButton.onClick.AddListener(()=>owner.AdvanceDialogue());if(skipRideButton!=null)skipRideButton.onClick.AddListener(()=>owner.SkipRide());}
        public override void Bind(NightCourierPrototype prototype){if(stageGraphic!=null){stageGraphic.gameObject.SetActive(false);stageGraphic.Owner=prototype;}base.Bind(prototype);}
        protected override void Refresh()
        {
            bool driving=Run.State==ShiftState.Driving||Run.State==ShiftState.Pushing;
            roadStatus.SetActive(!Run.Finished);
            var statusRect=(RectTransform)roadStatus.transform;var root=(RectTransform)transform;
            statusRect.pivot=new Vector2(1,1);statusRect.position=root.TransformPoint(new Vector3(root.rect.xMax-160,root.rect.yMax-24,0));
            bool travelling=driving||Run.State==ShiftState.Encounter;
            progressFill.gameObject.SetActive(travelling);
            var track=roadStatus.transform.Find("ProgressFillBackground");if(track!=null)track.gameObject.SetActive(travelling);
            if(skipRideButton!=null)skipRideButton.gameObject.SetActive(driving&&owner.Active);
            timeText.text=Run.ActiveOrder==null?"No active order":"Order "+Run.OrderTurns+"/"+Run.TargetTurns(Run.ActiveOrder)+" turns";
            progressFill.fillAmount=Run.Progress;
            PresentSubtitle();
            advanceButton.GetComponent<CourierIconHint>().message=owner.ConversationActive?"Continue dialogue - E":"Continue to "+(Run.HasJourney?Run.Stops[Run.PlannedDestination].Name:"")+" · E";
            advanceButton.gameObject.SetActive(owner.ConversationActive&&(Run.State==ShiftState.Docked||Run.Finished));
        }
    }
}
