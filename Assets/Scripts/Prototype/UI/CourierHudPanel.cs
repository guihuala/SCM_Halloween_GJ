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
        [Header("Ending Dialogue")]
        public Color endingDialogueColor = CourierColors.Hex(0xDCCBED);
        private GameObject endingArtwork;
        private GameObject endingCaptions;
        private Text endingText, endingHint;
        private CourierTextJuice endingJuice;
        [Header("HUD Motion")]
        public bool reducedMotion;
        public UIInfiniteScroll backgroundScroll;
        private readonly CourierHudMotion motion = new CourierHudMotion();
        private bool progressReady;
        private int progressLeg = -1;
        private Road progressRoad;
        private CourierTravelStatus travelStatus;
        private void Show(GameObject target, bool visible, Vector2 offset) { motion.Present(target, visible, offset, reducedMotion); }
        public void ApplyReadableLayout()
        {
            float timerWidth=Mathf.Clamp(timeText.preferredWidth+8,64,136);
            var status=(RectTransform)roadStatus.transform;status.sizeDelta=new Vector2(60+timerWidth,72);
            var clock=status.Find("ClockIcon") as RectTransform;
            if(clock!=null){clock.anchorMin=clock.anchorMax=clock.pivot=new Vector2(0,1);clock.anchoredPosition=new Vector2(0,-14);clock.sizeDelta=Vector2.one*44;}
            timeText.fontSize=30;timeText.alignment=TextAnchor.MiddleLeft;
            timeText.rectTransform.anchorMin=timeText.rectTransform.anchorMax=timeText.rectTransform.pivot=new Vector2(0,1);
            timeText.rectTransform.anchoredPosition=new Vector2(60,0);timeText.rectTransform.sizeDelta=new Vector2(timerWidth,72);
            if(travelStatus!=null)travelStatus.Layout();
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
            bool ending = owner != null && Run.Finished;
            var bar=subtitleText.transform.parent.Find("SubtitleBar");
            var image=bar!=null?bar.GetComponent<Image>():null;
            if(image!=null)image.color=CourierColors.Alpha(ending?endingDialogueColor:CourierColors.Paper,1);
            subtitleText.color=CourierColors.Night;
            foreach(var outline in subtitleText.GetComponents<Outline>())outline.enabled=false;
            if(speakerName!=null)speakerName.color=CourierColors.Night;
            var buttonImage=advanceButton.targetGraphic as Image;if(buttonImage!=null)buttonImage.color=ending?CourierColors.Gold:CourierColors.Mint;
            var icon=advanceButton.GetComponentInChildren<CourierIconGraphic>(true);if(icon!=null)icon.color=CourierColors.Night;
        }
        private void RefreshSpeaker()
        {
            ApplyReadingColors();
            if(characterCatalog==null)characterCatalog=Resources.Load<CourierCharacterCatalog>("NightVoyage/Characters");
            bool talking=owner.ConversationActive&&Run.State==ShiftState.Docked;
            var character=talking&&characterCatalog!=null?characterCatalog.ForStop(CourierStory.SpeakerStop(owner.DialogueSpeaker)):null;
            bool goolie=talking&&owner.DialogueSpeaker=="Goolie";
            bool show=(character!=null||goolie)&&speakerPortrait!=null&&speakerName!=null;
            if(speakerPortrait!=null)
            {
                if(show){speakerPortrait.sprite=goolie?Resources.Load<Sprite>("NightVoyage/GooliePortrait"):characterCatalog.PortraitFor(character);speakerPortrait.enabled=speakerPortrait.sprite!=null;}
            }
            if(portraitFrame!=null){if(show)portraitFrame.color=goolie?CourierColors.Mint:character.accent;}
            if(speakerName!=null){if(talking){NightCourier.CourierLocalization.Set(speakerName, owner.DialogueSpeaker);speakerName.color=character!=null?Color.Lerp(CourierColors.Night,character.accent,.23f):CourierColors.Night;}}
            var bar=subtitleText.transform.parent.Find("SubtitleBar") as RectTransform;
            subtitleText.fontSize=CourierLocalization.IsChinese?32:36;subtitleText.lineSpacing=CourierLocalization.IsChinese?1.25f:1.08f;
            subtitleText.rectTransform.sizeDelta=new Vector2(show?1224:1580,1000);
            NightCourier.CourierLocalization.Set(subtitleText, owner.Subtitle);
            float bodyHeight=Mathf.Max(152,subtitleText.preferredHeight+16);
            float extra=Mathf.Max(0,bodyHeight-152);
            if(bar!=null){bar.anchoredPosition=new Vector2(-20,-740+extra);bar.sizeDelta=new Vector2(1840,260+extra);}
            subtitleText.rectTransform.anchoredPosition=new Vector2(show?416:48,-824+extra);
            subtitleText.rectTransform.sizeDelta=new Vector2(show?1224:1580,bodyHeight);
            subtitleText.fontSize=CourierLocalization.IsChinese?32:36;subtitleText.lineSpacing=CourierLocalization.IsChinese?1.25f:1.08f;subtitleText.alignment=TextAnchor.UpperLeft;
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
            if (Run.Finished) { PresentEndingCaptions(); return; }
            if (endingCaptions != null) endingCaptions.SetActive(false);
            PresentText();
        }
        private void PresentEndingCaptions()
        {
            if (endingCaptions == null)
            {
                endingCaptions = new GameObject("EndingCaptions", typeof(RectTransform), typeof(CourierEndingGradient));
                var rect = (RectTransform)endingCaptions.transform;
                rect.SetParent(transform, false);
                rect.anchorMin = Vector2.zero; rect.anchorMax = new Vector2(1, 0);
                rect.pivot = new Vector2(.5f, 0); rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(0, 320);
                endingCaptions.GetComponent<CourierEndingGradient>().raycastTarget = false;
                endingText = EndingLabel("Epilogue", rect, 34);
                var body = endingText.rectTransform;
                body.anchorMin = new Vector2(.12f, 0); body.anchorMax = new Vector2(.88f, 0);
                body.pivot = new Vector2(.5f, 0); body.anchoredPosition = new Vector2(0, 88);
                body.sizeDelta = new Vector2(0, 140);
                endingText.lineSpacing = 1.3f;
                var shadow = endingText.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, .8f); shadow.effectDistance = new Vector2(1, -2);
                endingJuice = endingText.gameObject.AddComponent<CourierTextJuice>();
                endingJuice.rise = 0; endingJuice.pop = 0;
                endingHint = EndingLabel("Continue", rect, 20);
                var hint = endingHint.rectTransform;
                hint.anchorMin = hint.anchorMax = new Vector2(.5f, 0);
                hint.pivot = new Vector2(.5f, 0); hint.anchoredPosition = new Vector2(0, 25);
                hint.sizeDelta = new Vector2(360, 52);
                endingHint.raycastTarget = true;
                var button = endingHint.gameObject.AddComponent<Button>();
                button.targetGraphic = endingHint;
                var colors = button.colors;
                colors.normalColor = new Color(.8f, .87f, .81f);
                colors.highlightedColor = Color.white;
                colors.pressedColor = CourierColors.Gold;
                colors.selectedColor = Color.white; button.colors = colors;
                button.onClick.AddListener(() => owner.AdvanceDialogue());
            }
            endingCaptions.SetActive(owner.ConversationActive);
            if (!owner.ConversationActive) return;
            // Translate before recoloring rich-text emphasis for the dark backdrop.
            endingJuice.Present(CourierLocalization.T(owner.Subtitle).Replace("#30614F", "#BFE3C6"), !reducedMotion);
            if (reducedMotion) endingJuice.Complete();
            endingHint.text = CourierLocalization.IsChinese ? "E / 点击继续  ›" : "E / Click to continue  ›";
        }
        private Text EndingLabel(string name, Transform parent, int size)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(parent, false);
            label.font = subtitleText.font; label.fontSize = size;
            label.color = new Color(.97f, .96f, .91f);
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            return label;
        }
        private void PresentEndingArtwork()
        {
            if (Run.Finished && endingArtwork == null)
            {
                var texture = Resources.Load<Texture2D>("NightVoyage/ending_01");
                if (texture == null) return;
                endingArtwork = new GameObject("EndingCG", typeof(RectTransform), typeof(Image));
                var backdrop = (RectTransform)endingArtwork.transform;
                backdrop.SetParent(transform, false);
                backdrop.SetAsFirstSibling();
                backdrop.anchorMin = Vector2.zero;
                backdrop.anchorMax = Vector2.one;
                backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
                var matte = endingArtwork.GetComponent<Image>();
                matte.color = Color.black;
                matte.raycastTarget = false;
                var artwork = new GameObject("Artwork", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
                artwork.transform.SetParent(backdrop, false);
                var image = artwork.GetComponent<RawImage>();
                image.texture = texture;
                image.raycastTarget = false;
                var aspect = artwork.GetComponent<AspectRatioFitter>();
                aspect.aspectRatio = (float)texture.width / texture.height;
                aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            }
            if (endingArtwork != null) endingArtwork.SetActive(Run.Finished);
            if (backgroundScroll != null) backgroundScroll.gameObject.SetActive(!Run.Finished);
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
            if (Run.Finished)
            {
                PresentEndingCaptions();
                return endingJuice != null && endingJuice.Complete();
            }
            // Refresh owns layout and motion; completing text must not apply another offset.
            PresentText();
            return subtitleJuice.Complete();
        }
        protected override void Awake()
        {
            base.Awake();
            var track=roadStatus.transform.Find("ProgressFillBackground").GetComponent<Image>();
            travelStatus=CourierTravelStatus.Create(transform,progressFill,track);
            advanceButton.onClick.AddListener(()=>owner.AdvanceDialogue());
            if(skipRideButton!=null)skipRideButton.onClick.AddListener(()=>owner.SkipRide());
        }
        protected override void Update(){if(owner!=null&&!hasRemoved)Refresh();}
        public override void Bind(NightCourierPrototype prototype){if(stageGraphic!=null){stageGraphic.gameObject.SetActive(false);stageGraphic.Owner=prototype;}base.Bind(prototype);}
        protected override void Refresh()
        {
            PresentEndingArtwork();
            if(backgroundScroll!=null)
            {
                backgroundScroll.reducedMotion=reducedMotion;
                var pattern=backgroundScroll.GetComponent<Image>();
                if(pattern!=null)pattern.color=CourierColors.Alpha(Color.white,owner.HasPlanner?.2f:1);
            }
            NightCourier.CourierLocalization.Set(timeText, Run.ActiveOrder==null?"—":Run.OrderTurns+"/"+Run.TargetTurns(Run.ActiveOrder));
            motion.RestoreLayout();ApplyReadableLayout();
            bool driving=Run.State==ShiftState.Driving||Run.State==ShiftState.Pushing;
            var statusRect=(RectTransform)roadStatus.transform;var root=(RectTransform)transform;
            // A compact right-aligned row: order clock, then fast-forward below Pause.
            statusRect.pivot=new Vector2(1,1);statusRect.position=root.TransformPoint(new Vector3(root.rect.xMax-128,root.rect.yMax-112,0));
            bool travelling=driving||Run.State==ShiftState.Encounter;
            Show(roadStatus,travelling&&Run.ActiveOrder!=null&&!Run.Finished&&!owner.GuideOpen,new Vector2(0,32));
            // At a waypoint the map owns the single Continue action. This ribbon
            // is reserved for movement and road events, so it does not repeat it.
            Show(travelStatus.gameObject,travelling&&!Run.Finished&&!owner.GuideOpen,new Vector2(0,-16));
            if(skipRideButton!=null)
            {
                var skipRect=(RectTransform)skipRideButton.transform;
                skipRect.pivot=new Vector2(1,1);
                skipRect.position=root.TransformPoint(new Vector3(root.rect.xMax-32,root.rect.yMax-112,0));
                Show(skipRideButton.gameObject,driving&&owner.Active,new Vector2(0,32));
            }
            if(pauseButton!=null)Show(pauseButton.gameObject,owner.Active&&!Run.Finished&&!owner.HasPlanner&&Run.State!=ShiftState.Encounter,new Vector2(0,32));
            // A new journey starts at its own progress rather than draining the previous bar.
            if(!progressReady||!travelling||progressLeg!=Run.PlannedDestination||progressRoad!=Run.ActiveRoad||reducedMotion)
                progressFill.fillAmount=Run.DisplayProgress;
            else if(owner.Active)progressFill.fillAmount=Mathf.Lerp(progressFill.fillAmount,Run.DisplayProgress,1-Mathf.Exp(-12*Time.unscaledDeltaTime));
            progressReady=travelling;progressLeg=Run.PlannedDestination;
            progressRoad=Run.ActiveRoad;
            travelStatus.Refresh(Run,owner.Active,reducedMotion);
            PresentSubtitle();
            advanceButton.GetComponent<CourierIconHint>().message=owner.ConversationActive?"Continue dialogue - E":"Continue to "+(Run.HasJourney?Run.Stops[Run.PlannedDestination].Name:"")+" · E";
        }
    }
    // A soft scrim leaves the illustration visible without a rectangular dialogue panel.
    public sealed class CourierEndingGradient : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = rectTransform.rect;
            const int bands = 12;
            for (int i = 0; i <= bands; i++)
            {
                float t = (float)i / bands;
                var tint = new Color(.025f, .04f, .035f, .92f * (1 - t * t));
                float y = Mathf.Lerp(rect.yMin, rect.yMax, t);
                mesh.AddVert(new Vector3(rect.xMin, y), tint, Vector2.zero);
                mesh.AddVert(new Vector3(rect.xMax, y), tint, Vector2.zero);
                if (i == 0) continue;
                int v = i * 2;
                mesh.AddTriangle(v - 2, v, v - 1); mesh.AddTriangle(v, v + 1, v - 1);
            }
        }
    }
}
