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
        [Header("HUD Motion")]
        public bool reducedMotion;
        public UIInfiniteScroll backgroundScroll;
        private readonly CourierHudMotion motion = new CourierHudMotion();
        private bool progressReady;
        private int progressLeg = -1;
        private Road progressRoad;
        private void Show(GameObject target, bool visible, Vector2 offset) { motion.Present(target, visible, offset, reducedMotion); }
        public void ApplyReadableLayout()
        {
            var status=(RectTransform)roadStatus.transform;status.sizeDelta=new Vector2(340,76);
            var clock=status.Find("ClockIcon") as RectTransform;
            if(clock!=null){clock.anchoredPosition=new Vector2(0,-4);clock.sizeDelta=Vector2.one*48;}
            timeText.fontSize=30;timeText.rectTransform.anchoredPosition=new Vector2(64,0);timeText.rectTransform.sizeDelta=new Vector2(276,48);
            var track=status.Find("ProgressFillBackground") as RectTransform;
            if(track!=null){track.anchoredPosition=new Vector2(0,-62);track.sizeDelta=new Vector2(340,10);}
            progressFill.rectTransform.anchoredPosition=new Vector2(0,-62);progressFill.rectTransform.sizeDelta=new Vector2(340,10);
            foreach(var button in new[]{advanceButton,pauseButton})
            {
                if(button==null)continue;
                ((RectTransform)button.transform).sizeDelta=Vector2.one*84;
                var icon=button.GetComponentInChildren<CourierIconGraphic>(true);
                if(icon!=null){var rt=icon.rectTransform;rt.anchorMin=rt.anchorMax=rt.pivot=Vector2.one*.5f;rt.anchoredPosition=Vector2.zero;rt.sizeDelta=Vector2.one*52;}
            }
            if(skipRideButton!=null)
            {
                ((RectTransform)skipRideButton.transform).sizeDelta=new Vector2(80,72);
                var label=skipRideButton.GetComponentInChildren<Text>(true);
                if(label!=null){label.fontSize=30;label.rectTransform.sizeDelta=new Vector2(256,72);}
                CourierSymbolUI.Button(skipRideButton,CourierIconKind.FastForward,"Skip travel animation · F");
            }
        }
        public void ApplyReadingColors()
        {
            var bar=subtitleText.transform.parent.Find("SubtitleBar");
            var image=bar!=null?bar.GetComponent<Image>():null;
            if(image!=null)image.color=CourierColors.Alpha(CourierColors.Paper,1);
            subtitleText.color=CourierColors.Night;
            foreach(var outline in subtitleText.GetComponents<Outline>())outline.enabled=false;
            if(speakerName!=null)speakerName.color=CourierColors.Night;
            var buttonImage=advanceButton.targetGraphic as Image;if(buttonImage!=null)buttonImage.color=CourierColors.Mint;
            var icon=advanceButton.GetComponentInChildren<CourierIconGraphic>(true);if(icon!=null)icon.color=CourierColors.Night;
        }
        private void RefreshSpeaker()
        {
            ApplyReadingColors();
            if(characterCatalog==null)characterCatalog=Resources.Load<CourierCharacterCatalog>("NightVoyage/Characters");
            bool talking=owner.ConversationActive&&(Run.State==ShiftState.Docked||Run.Finished);
            var character=talking&&characterCatalog!=null?characterCatalog.ForStop(CourierStory.SpeakerStop(owner.DialogueSpeaker)):null;
            bool goolie=talking&&owner.DialogueSpeaker=="Goolie";
            bool show=(character!=null||goolie)&&speakerPortrait!=null&&speakerName!=null;
            if(speakerPortrait!=null)
            {
                if(show){speakerPortrait.sprite=goolie?Resources.Load<Sprite>("NightVoyage/GooliePortrait"):characterCatalog.PortraitFor(character);speakerPortrait.enabled=speakerPortrait.sprite!=null;}
            }
            if(portraitFrame!=null){if(show)portraitFrame.color=goolie?CourierColors.Mint:character.accent;}
            if(speakerName!=null){if(talking){speakerName.text=owner.DialogueSpeaker;speakerName.color=character!=null?Color.Lerp(CourierColors.Night,character.accent,.23f):CourierColors.Night;}}
            var bar=subtitleText.transform.parent.Find("SubtitleBar") as RectTransform;
            subtitleText.fontSize=36;subtitleText.lineSpacing=1.08f;
            subtitleText.rectTransform.sizeDelta=new Vector2(show?1224:1580,1000);
            subtitleText.text=owner.Subtitle;
            float bodyHeight=Mathf.Max(152,subtitleText.preferredHeight+16);
            float extra=Mathf.Max(0,bodyHeight-152);
            if(bar!=null){bar.anchoredPosition=new Vector2(-20,-740+extra);bar.sizeDelta=new Vector2(1840,260+extra);}
            subtitleText.rectTransform.anchoredPosition=new Vector2(show?416:48,-824+extra);
            subtitleText.rectTransform.sizeDelta=new Vector2(show?1224:1580,bodyHeight);
            subtitleText.fontSize=36;subtitleText.lineSpacing=1.08f;subtitleText.alignment=TextAnchor.UpperLeft;
            var advanceRect=(RectTransform)advanceButton.transform;
            advanceRect.anchoredPosition=new Vector2(advanceRect.anchoredPosition.x,-840);advanceRect.sizeDelta=Vector2.one*96;
            var advanceIcon=advanceButton.GetComponentInChildren<CourierIconGraphic>(true);if(advanceIcon!=null)advanceIcon.rectTransform.sizeDelta=Vector2.one*60;
            if(speakerName!=null){speakerName.rectTransform.anchoredPosition=new Vector2(show?416:48,-752+extra);speakerName.rectTransform.sizeDelta=new Vector2(show?1224:1580,60);speakerName.fontSize=36;}
            var slide = new Vector2(0, -48);
            if(bar!=null)Show(bar.gameObject,talking,slide);
            Show(subtitleText.gameObject,talking,slide);
            if(speakerPortrait!=null)Show(speakerPortrait.gameObject,show,slide);
            if(portraitFrame!=null)Show(portraitFrame.gameObject,show,slide);
            if(speakerName!=null)Show(speakerName.gameObject,talking,slide);
            Show(advanceButton.gameObject,talking,slide);
        }

        private void PresentSubtitle()
        {
            RefreshSpeaker();
            PresentText();
        }
        private void PresentText()
        {
            if(subtitleJuice==null)subtitleJuice=subtitleText.GetComponent<CourierTextJuice>()??subtitleText.gameObject.AddComponent<CourierTextJuice>();
            if(owner.ConversationActive&&(Run.State==ShiftState.Docked||Run.Finished))
                subtitleJuice.Present(owner.Subtitle,!reducedMotion);
            if(reducedMotion)subtitleJuice.Complete();
        }
        public bool CompleteSubtitle()
        {
            // Refresh owns layout and motion; completing text must not apply another offset.
            PresentText();
            return subtitleJuice.Complete();
        }
        protected override void Awake(){base.Awake();advanceButton.onClick.AddListener(()=>owner.AdvanceDialogue());if(skipRideButton!=null)skipRideButton.onClick.AddListener(()=>owner.SkipRide());}
        protected override void Update(){if(owner!=null&&!hasRemoved)Refresh();}
        public override void Bind(NightCourierPrototype prototype){if(stageGraphic!=null){stageGraphic.gameObject.SetActive(false);stageGraphic.Owner=prototype;}base.Bind(prototype);}
        protected override void Refresh()
        {
            if(backgroundScroll!=null)backgroundScroll.reducedMotion=reducedMotion;
            motion.RestoreLayout();ApplyReadableLayout();
            bool driving=Run.State==ShiftState.Driving||Run.State==ShiftState.Pushing;
            var statusRect=(RectTransform)roadStatus.transform;var root=(RectTransform)transform;
            statusRect.pivot=new Vector2(1,1);statusRect.position=root.TransformPoint(new Vector3(root.rect.xMax-32,root.rect.yMax-112,0));
            bool travelling=driving||Run.State==ShiftState.Encounter;
            Show(roadStatus,travelling&&!Run.Finished,new Vector2(0,32));
            Show(progressFill.gameObject,travelling,new Vector2(24,0));
            var track=roadStatus.transform.Find("ProgressFillBackground");if(track!=null)Show(track.gameObject,travelling,new Vector2(24,0));
            if(skipRideButton!=null)
            {
                var skipRect=(RectTransform)skipRideButton.transform;
                skipRect.position=root.TransformPoint(new Vector3(root.rect.xMax-464,root.rect.yMax-112,0));
                Show(skipRideButton.gameObject,driving&&owner.Active,new Vector2(0,32));
            }
            if(pauseButton!=null)Show(pauseButton.gameObject,owner.Active&&!Run.Finished&&!owner.HasPlanner&&Run.State!=ShiftState.Encounter,new Vector2(0,32));
            timeText.text=Run.ActiveOrder==null?"—":Run.OrderTurns+"/"+Run.TargetTurns(Run.ActiveOrder);
            // A new journey starts at its own progress rather than draining the previous bar.
            if(!progressReady||!travelling||progressLeg!=Run.PlannedDestination||progressRoad!=Run.ActiveRoad||reducedMotion)
                progressFill.fillAmount=Run.DisplayProgress;
            else progressFill.fillAmount=Mathf.Lerp(progressFill.fillAmount,Run.DisplayProgress,1-Mathf.Exp(-12*Time.unscaledDeltaTime));
            progressReady=travelling;progressLeg=Run.PlannedDestination;
            progressRoad=Run.ActiveRoad;
            PresentSubtitle();
            advanceButton.GetComponent<CourierIconHint>().message=owner.ConversationActive?"Continue dialogue - E":"Continue to "+(Run.HasJourney?Run.Stops[Run.PlannedDestination].Name:"")+" · E";
        }
    }
}
