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
        private readonly Vector3[] corners = new Vector3[4];
        public void Bind(NightCourierPrototype prototype)
        {
            owner = prototype;
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(() => { owner.Tutorial.Dismiss(); Refresh(); });
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
            ApplyReadingColors();
            var run = owner.Run; var guide = owner.Tutorial;
            int step = guide.Step(run, owner.ConversationActive);
            bool visible = owner.Active && !guide.Finished && !run.Finished;
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
            if(skipLabel!=null)skipLabel.text = step == 6 ? "Done" : "Skip";
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
                    title = "Start at the Parcel Depot";
                    body = "Open Orders [Tab], choose any sender, then click their map stop and the green departure arrow. Empty travel has no order timer.";
                    break;
                case 2:
                    title = "Choose your first order";
                    body = "Visit any sender shown in Orders. At their stop, accept a batch with the box arrow or drag it into On Board. Choose any order; carry one batch at a time.";
                    if (planner != null)
                    {
                        var row = planner.GetComponentsInChildren<CourierCargoRow>().FirstOrDefault(r => r.Item.Origin == run.Node && r.Item.State == ParcelState.Waiting);
                        if(!planner.CargoOpen){first=planner.cargoToggleButton.transform as RectTransform;body="Open Orders [Tab] to compare senders and destinations. Travel to any sender to accept their order.";}
                        else if (row != null) first = (RectTransform)row.actionButton.transform;
                    }
                    break;
                case 3:
                    title = "Choose a destination";
                    body = "Deliver to "+(run.ActiveOrder!=null?run.Stops[run.ActiveOrder.Destination].Name:"your recipient")+". Orange lines show the route. Compare Safe and Fastest before departing.";
                    if (planner != null) first = planner.stopLabels[run.ActiveOrder!=null?run.ActiveOrder.Destination:0].rectTransform;
                    break;
                case 4:
                    title = guide.Departed ? "On the road" : "Ready to depart";
                    body = guide.Departed ? (run.State==ShiftState.Docked ? "This is a waypoint, not your delivery stop. Press E or Continue to follow the route to your chosen recipient. You can also change direction here." : "Moving uses whole order turns and vehicle turns. Press F to skip to the next event or stop; choices remain yours.")
                        : "Check the turn costs, then press the green arrow. Exhausted vehicles switch to ghost transport.";
                    if (!guide.Departed && planner != null) first = (RectTransform)planner.departButton.transform;
                    else if (run.State==ShiftState.Docked&&planner!=null) first=planner.nodePromptButton.transform as RectTransform;
                    break;
                case 5:
                    title = run.State == ShiftState.Encounter ? "A road event" : "Almost there";
                    body = run.State == ShiftState.Encounter ? "Marked junctions roll a "+Mathf.RoundToInt(run.EventProbability*100)+"% chance while carrying cargo. At most one event per order: wait +1 turn or fall back one node. Confirm to continue."
                        : "Press E at each waypoint to continue toward your recipient. Delivery happens automatically.";
                    if (encounter != null) { first = (RectTransform)encounter.safeButton.transform; }
                    break;
                default:
                    title = "First stop reached";
                    body = "Deliveries happen automatically. Click your current stop to talk, load more cargo, or choose a new route. You're ready!";
                    if (hud != null && owner.ConversationActive) first = (RectTransform)hud.advanceButton.transform;
                    break;
            }
            if (run.State == ShiftState.Pushing) body = "Your ghost carries the full order at a slower pace. Late or damaged orders are still accepted.";
            if (!owner.HasPlanner && run.State == ShiftState.Docked && step >= 2 && step <= 4)
            { body = "Choose a stop on the map and manage your cargo on the right. You can explore freely or skip this guide."; first = null; }
            heading.text = "FIRST SHIFT  " + Mathf.Max(1,step) + "/6  -  " + title;
            instruction.text = body;
            CompactGuide(step,title,body);
            Focus(0, first); Focus(1, second);
        }
        private void CompactGuide(int step,string title,string body)
        {
            strip.anchorMin=strip.anchorMax=strip.pivot=Vector2.zero;
            strip.anchoredPosition=new Vector2(24,32);strip.sizeDelta=new Vector2(248,80);
            instruction.enabled=false;heading.enabled=true;heading.text=Mathf.Max(1,step)+"/6";
            heading.rectTransform.anchoredPosition=new Vector2(80,-16);heading.rectTransform.sizeDelta=new Vector2(72,48);heading.alignment=TextAnchor.MiddleCenter;
            var kind=step==0?CourierIconKind.Talk:step<=2?CourierIconKind.Box:step==3?CourierIconKind.Route:step==5?CourierIconKind.Warning:step==6?CourierIconKind.Check:CourierIconKind.Arrow;
            CourierSymbolUI.Icon(strip,"GuideSymbol",kind,16,16,48,CourierColors.Night,title+"\n"+body);
            var close=(RectTransform)skipButton.transform;close.anchorMin=close.anchorMax=close.pivot=new Vector2(0,1);close.anchoredPosition=new Vector2(176,-12);close.sizeDelta=Vector2.one*56;
            CourierSymbolUI.Button(skipButton,CourierIconKind.Close,step==6?"Finish guide":"Dismiss guide");
            var closeImage=skipButton.targetGraphic as Image;if(closeImage!=null)closeImage.color=Color.clear;
            foreach(var outline in skipButton.GetComponents<Outline>())outline.enabled=false;
            var closeSymbol=skipButton.transform.Find("ActionSymbol") as RectTransform;if(closeSymbol!=null)closeSymbol.sizeDelta=Vector2.one*32;
        }
        private void Focus(int index, RectTransform target)
        {
            if (target == null || !target.gameObject.activeInHierarchy) return;
            target.GetWorldCorners(corners);
            var parent = (RectTransform)transform;
            var min = parent.InverseTransformPoint(corners[0]); var max = parent.InverseTransformPoint(corners[2]);
            var frame = focusFrames[index];
            frame.anchorMin=frame.anchorMax=new Vector2(0,1);
            frame.pivot=new Vector2(0,1);
            min-=new Vector3(parent.rect.xMin,parent.rect.yMax,0);max-=new Vector3(parent.rect.xMin,parent.rect.yMax,0);
            frame.anchoredPosition = new Vector2(min.x - 6, max.y + 6);
            frame.sizeDelta = new Vector2(max.x - min.x + 12, max.y - min.y + 12);
            frame.gameObject.SetActive(true);
        }
    }
}
