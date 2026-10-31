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
        public void Refresh()
        {
            if (owner == null) return;
            var run = owner.Run; var guide = owner.Tutorial;
            int step = guide.Step(run, owner.ConversationActive);
            bool visible = owner.Active && !guide.Finished && !run.Finished;
            strip.gameObject.SetActive(visible);
            foreach (var frame in focusFrames) frame.gameObject.SetActive(false);
            if (!visible) return;
            strip.anchoredPosition = new Vector2(0,-120);
            float width=Mathf.Min(1300,((RectTransform)transform).rect.width-64);
            strip.sizeDelta=new Vector2(width,104);
            heading.rectTransform.sizeDelta=new Vector2(width-152,30);
            instruction.rectTransform.sizeDelta=new Vector2(width-152,60);
            ((RectTransform)skipButton.transform).anchoredPosition=new Vector2(width-96,-28);
            skipButton.GetComponentInChildren<Text>().text = step == 6 ? "Done" : "Skip";
            string title, body; RectTransform first = null, second = null;
            var ui=UIManager.Instance;
            var hud=ui.GetPanel("CourierHudPanel") as CourierHudPanel;
            var planner=ui.GetPanel("CourierRoutePanel") as CourierRoutePanel;
            var encounter=ui.GetPanel("CourierEventPanel") as CourierEventPanel;
            switch (step)
            {
                case 0:
                    title = "Meet the drow";
                    body = "Press E or the dialogue arrow to continue. The drow has a party theme tape to send.";
                    if (hud != null) first = (RectTransform)hud.advanceButton.transform;
                    break;
                case 1:
                    title = "Open your map";
                    body = "Your map is always open. Press Tab or click Cargo to manage parcels. Planning does not use time.";
                    break;
                case 2:
                    title = "Load your first delivery";
                    body = "Load Party Theme Tape from Pick Up: click its box arrow, or drag the card into On Board. One batch at a time; all items with the same sender and recipient travel together.";
                    if (planner != null)
                    {
                        var row = planner.GetComponentsInChildren<CourierCargoRow>().FirstOrDefault(r => r.Item == run.Parcels[0]);
                        if(!planner.CargoOpen){first=planner.cargoToggleButton.transform as RectTransform;body="Open Cargo with Tab or the top button. Then load the Party Theme Tape from Pick Up.";}
                        else if (row != null) first = (RectTransform)row.actionButton.transform;
                    }
                    break;
                case 3:
                    title = "Choose a destination";
                    body = "Take the tape to Abandoned Radio Tower. You can choose any open stop; orange lines show the full route.";
                    if (planner != null) first = planner.stopLabels[3].rectTransform;
                    break;
                case 4:
                    title = guide.Departed ? "On the road" : "Ready to depart";
                    body = guide.Departed ? "Moving uses whole order turns and vehicle turns. Press F to skip to the next event or stop; choices remain yours."
                        : "Check the turn costs, then press the green arrow. Exhausted vehicles switch to ghost transport.";
                    if (!guide.Departed && planner != null) first = (RectTransform)planner.departButton.transform;
                    break;
                case 5:
                    title = run.State == ShiftState.Encounter ? "A road event" : "Almost there";
                    body = run.State == ShiftState.Encounter ? "Events make you wait one turn, take a detour, or return to the previous stop. Read the outcome, then confirm to continue."
                        : "Keep going. Parcels for the next stop will be delivered automatically when you arrive.";
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
            Focus(0, first); Focus(1, second);
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
