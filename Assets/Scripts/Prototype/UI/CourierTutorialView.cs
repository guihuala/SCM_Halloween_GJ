using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    public sealed class CourierTutorialView : MonoBehaviour
    {
        public RectTransform strip;
        public Text heading, instruction;
        public Button skipButton;
        public RectTransform[] focusFrames;
        private NightCourierPrototype owner;
        private Image guideBackdrop;
        public bool IsCollapsed { get; private set; }
        private CourierMapMotion focusMotion;
        public void Bind(NightCourierPrototype prototype)
        {
            owner = prototype;
            IsCollapsed = false;
            foreach (var frame in focusFrames)
                foreach (var graphic in frame.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(() => { if (owner.GuideOpen) owner.Tutorial.CloseGuide(); else IsCollapsed = !IsCollapsed; Refresh(); });
            Refresh();
        }
        public void ApplyReadingColors()
        {
            float rootWidth=((RectTransform)transform).rect.width;
            float width=Mathf.Min(1440,(rootWidth<100?1920:rootWidth)-64);
            strip.anchoredPosition=new Vector2(0,-192);strip.sizeDelta=new Vector2(width,204);
            heading.rectTransform.anchoredPosition=new Vector2(24,-12);heading.rectTransform.sizeDelta=new Vector2(width-216,52);heading.fontSize=30;
            instruction.rectTransform.anchoredPosition=new Vector2(24,-76);instruction.rectTransform.sizeDelta=new Vector2(width-216,112);instruction.fontSize=28;
            LayoutSkip(width);
            var background=strip.GetComponent<Image>();
            if(background!=null)background.color=CourierColors.Alpha(Color.Lerp(CourierColors.Paper,CourierColors.Mint,.10f),1);
            heading.color=Color.Lerp(CourierColors.Night,CourierColors.Mint,.18f);
            instruction.color=CourierColors.Night;
            foreach(var label in new[]{heading,instruction})foreach(var outline in label.GetComponents<Outline>())outline.enabled=false;
            var buttonImage=skipButton.targetGraphic as Image;if(buttonImage!=null)buttonImage.color=Color.Lerp(CourierColors.Paper,CourierColors.Mint,.55f);
            var buttonText=skipButton.GetComponentInChildren<Text>(true);if(buttonText!=null)buttonText.color=CourierColors.Night;
        }
        private void LayoutSkip(float width)
        {
            var label=skipButton.GetComponentInChildren<Text>(true);
            var rect=(RectTransform)skipButton.transform;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(width-(label!=null?144:88),-64);
            rect.sizeDelta=label!=null?new Vector2(120,68):new Vector2(64,64);
            if(label!=null)label.fontSize=28;
        }
        public void Refresh()
        {
            if (owner == null) return;
            ClearFocus();
            ApplyReadingColors();
            var run = owner.Run; var guide = owner.Tutorial;
            int step = guide.Step(run, owner.ConversationActive);
            bool visible = owner.Active && (!guide.Finished||guide.GuideOpen) && !run.Finished && !owner.ConversationActive;
            if(guideBackdrop==null)
            {
                var go=new GameObject("GuideBackdrop",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(transform,false);go.transform.SetAsFirstSibling();
                guideBackdrop=go.GetComponent<Image>();guideBackdrop.color=new Color(.03f,.05f,.08f,.8f);guideBackdrop.raycastTarget=true;
                var rect=guideBackdrop.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            }
            guideBackdrop.gameObject.SetActive(visible&&guide.GuideOpen);
            strip.gameObject.SetActive(visible);
            foreach (var frame in focusFrames) frame.gameObject.SetActive(false);
            if (!visible) return;
            strip.anchoredPosition = new Vector2(0,-192);
            float width=Mathf.Min(1440,((RectTransform)transform).rect.width-64);
            strip.sizeDelta=new Vector2(width,204);
            heading.rectTransform.anchoredPosition=new Vector2(24,-12);heading.rectTransform.sizeDelta=new Vector2(width-216,52);heading.fontSize=30;
            instruction.rectTransform.anchoredPosition=new Vector2(24,-76);instruction.rectTransform.sizeDelta=new Vector2(width-216,112);instruction.fontSize=28;
            LayoutSkip(width);
            var mapPage=UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
            if(step>=3&&step<=5&&(mapPage==null||!mapPage.CargoOpen))strip.anchoredPosition=new Vector2(0,-Mathf.Max(192,((RectTransform)transform).rect.height-220));
            var skipLabel=skipButton.GetComponentInChildren<Text>(true);
            if(skipLabel!=null)NightCourier.CourierLocalization.Set(skipLabel, step == 6 ? "Done" : "Skip");
            string title, body; RectTransform first = null, second = null;
            var ui=UIManager.Instance;
            var hud=ui.GetPanel("CourierHudPanel") as CourierHudPanel;
            var planner=ui.GetPanel("CourierRoutePanel") as CourierRoutePanel;
            var encounter=ui.GetPanel("CourierEventPanel") as CourierEventPanel;
            switch (step)
            {
                case 0:
                    title = "Welcome, Goolie";
                    body = "Continue with E. Choose your own delivery order from the map.";
                    if (hud != null) first = (RectTransform)hud.advanceButton.transform;
                    break;
                case 1:
                    title = "Click the glowing tunnel";
                    body = "A letter is waiting for you there. Click the glowing tunnel to see your route.";
                    if (planner != null && planner.mapArtwork != null) first = planner.mapArtwork.landmarkImages[0].rectTransform;
                    break;
                case 2:
                    title = "Choose your first order";
                    body = "Parcels are ready here. Click the box arrow in Orders to collect them. Pickup starts the delivery clock.";
                    if (planner != null)
                    {
                        var row = planner.GetComponentsInChildren<CourierCargoRow>().FirstOrDefault(r => r.Item.Origin == run.Node && r.Item.State == ParcelState.Waiting);
                        if(!planner.CargoOpen){first=planner.cargoToggleButton.transform as RectTransform;body="Click the glowing box to see the orders. The box arrow collects parcels at your stop.";}
                        else if (row != null) first = (RectTransform)row.actionButton.transform;
                    }
                    break;
                case 3:
                    title = run.ActiveOrder != null ? run.Stops[run.ActiveOrder.Destination].Name : "Choose a destination";
                    body = "Your recipient is marked on the map. Click their stop to see the orange route. The green arrow sets you off.";
                    if (planner != null && planner.mapArtwork != null) first = planner.mapArtwork.landmarkImages[run.ActiveOrder!=null?run.ActiveOrder.Destination:0].rectTransform;
                    break;
                case 4:
                    title = guide.Departed ? "On the road" : "Ready to depart";
                    body = guide.Departed ? (run.State==ShiftState.Docked ? "Just a stop along the way. E or the arrow continues the route. Parcels are delivered on arrival." : "F skips ahead to the next stop or event. E keeps you moving at waypoints.")
                        : "Click the glowing green arrow to depart. The clock shows the estimated turns.";
                    if (!guide.Departed && planner != null) first = (RectTransform)planner.departButton.transform;
                    else if (run.State==ShiftState.Docked&&planner!=null) first=planner.nodePromptButton.transform as RectTransform;
                    break;
                case 5:
                    title = run.State == ShiftState.Encounter ? "A road event" : "Almost there";
                    body = run.State == ShiftState.Encounter ? "Something happened on the road. The event card has the details; the green check confirms."
                        : "Press E at each waypoint to continue toward your recipient. Delivery happens automatically.";
                    if (encounter != null) { first = (RectTransform)encounter.safeButton.transform; }
                    break;
                default:
                    title = "First stop reached";
                    body = "First delivery done. More parcels are waiting in Orders.";
                    if (hud != null && owner.ConversationActive) first = (RectTransform)hud.advanceButton.transform;
                    break;
            }
            if (!guide.CargoLoaded && (run.State == ShiftState.Driving || run.State == ShiftState.Pushing))
            {
                title = "On the way to a sender";
                body = "No parcels on board yet, so the delivery clock is off. F skips travel; E continues at waypoints.";
                first = null;
            }
            else if (!guide.CargoLoaded && run.HasJourney && run.State == ShiftState.Docked)
            {
                title = "Continue to the sender";
                body = "The sender is further along this route. E or the arrow keeps you going.";
                if (planner != null) first = planner.nodePromptButton.transform as RectTransform;
            }
            else if (!guide.CargoLoaded && planner != null && planner.departButton.gameObject.activeInHierarchy && guide.RouteChosen)
            {
                title = "Ready to depart";
                body = "Click the glowing green arrow to depart. The clock shows the estimated turns.";
                first = planner.departButton.transform as RectTransform;
            }
            if (run.State == ShiftState.Pushing) body = "Your ghost carries the full order at a slower pace. Late or damaged orders are still accepted.";
            if (!owner.HasPlanner && run.State == ShiftState.Docked && step >= 2 && step <= 4)
            { body = "Choose a stop on the map and manage your cargo on the right. You can explore freely or skip this guide."; first = null; }
            NightCourier.CourierLocalization.Set(heading, "FIRST SHIFT  " + Mathf.Max(1,step) + "/6  -  " + title);
            NightCourier.CourierLocalization.Set(instruction, body);
            if(guide.GuideOpen)FullGuide();else CompactGuide(step,title,body);
            if (!guide.GuideOpen && !IsCollapsed) { Focus(0, first); Focus(1, second); }
        }
        private void FullGuide()
        {
            var size=((RectTransform)transform).rect.size;
            float width=Mathf.Min(1440,size.x-80),height=Mathf.Min(880,size.y-64);
            strip.anchorMin=strip.anchorMax=strip.pivot=Vector2.one*.5f;strip.anchoredPosition=Vector2.zero;strip.sizeDelta=new Vector2(width,height);
            heading.enabled=instruction.enabled=true;
            NightCourier.CourierLocalization.Set(heading, "YOUR HALLOWEEN DELIVERY GUIDE");heading.alignment=TextAnchor.UpperLeft;heading.resizeTextForBestFit=false;heading.fontSize=34;
            heading.rectTransform.anchoredPosition=new Vector2(40,-32);heading.rectTransform.sizeDelta=new Vector2(width-160,64);
            NightCourier.CourierLocalization.Set(instruction, CourierInformation.Rules(owner.Run).Replace("DELIVERY GUIDE\n","").Replace("\n","\n\n"));
            instruction.fontSize=28;instruction.alignment=TextAnchor.UpperLeft;
            instruction.rectTransform.anchoredPosition=new Vector2(40,-112);instruction.rectTransform.sizeDelta=new Vector2(width-80,height-144);
            instruction.resizeTextForBestFit=true;instruction.resizeTextMinSize=20;instruction.resizeTextMaxSize=28;
            foreach(var name in new[]{"TaskToggleHorizontal","TaskToggleVertical"})
            {var mark=skipButton.transform.Find(name);if(mark!=null)mark.gameObject.SetActive(false);}
            var compact=strip.Find("GuideSymbol");if(compact!=null)compact.gameObject.SetActive(false);
            var close=(RectTransform)skipButton.transform;close.anchorMin=close.anchorMax=close.pivot=new Vector2(0,1);
            close.anchoredPosition=new Vector2(width-96,-24);close.sizeDelta=Vector2.one*64;
            CourierSymbolUI.Button(skipButton,CourierIconKind.Close,"Close guide · reopen with the information button");
            skipButton.transform.Find("ActionSymbol").gameObject.SetActive(true);
            var image=skipButton.targetGraphic as Image;if(image!=null){image.sprite=Resources.Load<Sprite>("NightVoyage/HUD/Button");image.type=Image.Type.Sliced;image.color=CourierColors.Mint;}
        }
        private void CompactGuide(int step,string title,string body)
        {
            // Put the next action in the initial reading path, below the top HUD.
            float width = Mathf.Min(600, ((RectTransform)transform).rect.width - 48);
            strip.anchorMin=strip.anchorMax=strip.pivot=new Vector2(0,1);
            strip.anchoredPosition=new Vector2(24,-112);strip.sizeDelta=new Vector2(width,IsCollapsed ? 76 : 220);
            heading.enabled=true;instruction.enabled=!IsCollapsed;
            NightCourier.CourierLocalization.Set(heading, title);
            heading.rectTransform.anchoredPosition=new Vector2(76,-18);heading.rectTransform.sizeDelta=new Vector2(width-144,40);
            heading.alignment=TextAnchor.MiddleLeft;heading.fontSize=32;
            heading.resizeTextForBestFit=true;heading.resizeTextMinSize=24;heading.resizeTextMaxSize=32;
            NightCourier.CourierLocalization.Set(instruction, body);
            instruction.rectTransform.anchoredPosition=new Vector2(24,-72);instruction.rectTransform.sizeDelta=new Vector2(width-48,132);
            instruction.alignment=TextAnchor.UpperLeft;instruction.fontSize=30;
            instruction.resizeTextForBestFit=true;instruction.resizeTextMinSize=24;instruction.resizeTextMaxSize=30;
            heading.raycastTarget=instruction.raycastTarget=false;
            var background=strip.GetComponent<Image>();if(background!=null)background.raycastTarget=false;
            var kind=step==0?CourierIconKind.Talk:step==1?CourierIconKind.Arrow:step==2?CourierIconKind.Box:step==3?CourierIconKind.Route:step==5?CourierIconKind.Warning:step==6?CourierIconKind.Check:CourierIconKind.Arrow;
            CourierSymbolUI.Icon(strip,"GuideSymbol",kind,20,18,40,CourierColors.Night,title+"\n"+body).gameObject.SetActive(true);
            var close=(RectTransform)skipButton.transform;close.anchorMin=close.anchorMax=close.pivot=new Vector2(0,1);close.anchoredPosition=new Vector2(width-64,-10);close.sizeDelta=Vector2.one*56;
            // A collapsed task stays available and follows progress without reopening itself.
            foreach(var label in skipButton.GetComponentsInChildren<Text>(true))label.enabled=false;
            var oldSymbol=skipButton.transform.Find("ActionSymbol");
            if(oldSymbol!=null)oldSymbol.gameObject.SetActive(false);
            TaskToggleStroke("TaskToggleHorizontal",new Vector2(24,3),true);
            TaskToggleStroke("TaskToggleVertical",new Vector2(3,24),IsCollapsed);
            CourierSymbolUI.Hint(skipButton.transform,IsCollapsed ? "Expand task" : "Collapse task");
            var closeImage=skipButton.targetGraphic as Image;if(closeImage!=null)closeImage.color=Color.clear;
            foreach(var outline in skipButton.GetComponents<Outline>())outline.enabled=false;
            var closeSymbol=skipButton.transform.Find("ActionSymbol") as RectTransform;if(closeSymbol!=null)closeSymbol.sizeDelta=Vector2.one*32;
        }
        private void TaskToggleStroke(string name,Vector2 size,bool visible)
        {
            var child=skipButton.transform.Find(name);
            if(child==null)
            {
                var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
                go.transform.SetParent(skipButton.transform,false);child=go.transform;
            }
            var image=child.GetComponent<Image>();image.color=CourierColors.Night;image.raycastTarget=false;
            var rect=image.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
            rect.anchoredPosition=Vector2.zero;rect.sizeDelta=size;child.gameObject.SetActive(visible);
        }
        private void ClearFocus()
        {
            if(focusMotion!=null)focusMotion.TutorialTarget=null;
        }
        private void OnDisable() { ClearFocus(); }
        private void Focus(int index, RectTransform target)
        {
            if(target==null||!target.gameObject.activeInHierarchy)return;
            var image=target.GetComponent<Image>();
            if(image==null||image.sprite==null)return;
            if(focusMotion==null)
            {
                var planner=UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
                if(planner!=null)focusMotion=planner.GetComponentInChildren<CourierMapMotion>(true);
            }
            if(focusMotion!=null)focusMotion.TutorialTarget=image;
        }
    }
}
