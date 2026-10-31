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
        private CourierPanel host;
        private readonly Vector3[] corners = new Vector3[4];
        public void Bind(NightCourierPrototype prototype, CourierPanel panel)
        {
            owner = prototype; host = panel;
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
            if (host is CourierHudPanel) visible &= !owner.HasPlanner && run.State != ShiftState.Encounter;
            if (host is CourierEventPanel) visible &= run.State == ShiftState.Encounter;
            if (host is CourierRoutePanel) visible &= run.State != ShiftState.Encounter;
            strip.gameObject.SetActive(visible);
            foreach (var frame in focusFrames) frame.gameObject.SetActive(false);
            if (!visible) return;
            strip.anchoredPosition = new Vector2(0,-64);
            skipButton.GetComponentInChildren<Text>().text = step == 6 ? "Done" : "Skip";
            string title, body; RectTransform first = null, second = null;
            var hud = host as CourierHudPanel; var planner = host as CourierRoutePanel; var encounter = host as CourierEventPanel;
            switch (step)
            {
                case 0:
                    title = "Meet the drow";
                    body = "Press E or the dialogue arrow to continue. The drow has a party theme tape to send.";
                    if (hud != null) first = (RectTransform)hud.advanceButton.transform;
                    break;
                case 1:
                    title = "Open your map";
                    body = "Your map and cargo are already open. Choose a stop or load a parcel. Planning does not use time.";
                    break;
                case 2:
                    title = "Load your first delivery";
                    body = "Load Party Theme Tape from Pick Up: click its box arrow, or drag the card into On Board. Watch your load / capacity.";
                    if (planner != null)
                    {
                        var row = planner.GetComponentsInChildren<CourierCargoRow>().FirstOrDefault(r => r.Item == run.Parcels[0]);
                        if (row != null) first = (RectTransform)row.actionButton.transform;
                    }
                    break;
                case 3:
                    title = "Choose a destination";
                    body = "Take the tape to Abandoned Radio Tower. You can choose any open stop; orange lines show the full route.";
                    if (planner != null) first = planner.stopLabels[3].rectTransform;
                    break;
                case 4:
                    title = guide.Departed ? "On the road" : "Ready to depart";
                    body = guide.Departed ? "Driving uses time and durability. Press F to skip to the next event or stop; choices remain yours."
                        : "Check the time and wrench costs, then press the green arrow. A full load means slower travel and more wear.";
                    if (!guide.Departed && planner != null) first = (RectTransform)planner.departButton.transform;
                    break;
                case 5:
                    title = run.State == ShiftState.Encounter ? "Make a road choice" : "Almost there";
                    body = run.State == ShiftState.Encounter ? "Clock = extra time. Wrench = durability loss. Glass and umbrella icons warn of cargo damage. Choose either option."
                        : "Keep going. Parcels for the next stop will be delivered automatically when you arrive.";
                    if (encounter != null) { first = (RectTransform)encounter.safeButton.transform; second = (RectTransform)encounter.riskButton.transform; }
                    break;
                default:
                    title = "First stop reached";
                    body = "Deliveries happen automatically. Click your current stop to talk, load more cargo, or choose a new route. You're ready!";
                    if (hud != null && owner.ConversationActive) first = (RectTransform)hud.advanceButton.transform;
                    break;
            }
            if (run.State == ShiftState.Pushing) body = "Broken down? No Game Over: your ghost pushes to the next stop. Timed side parcels expire, but the story continues.";
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
            frame.anchoredPosition = new Vector2(min.x - 6, max.y + 6);
            frame.sizeDelta = new Vector2(max.x - min.x + 12, max.y - min.y + 12);
            frame.gameObject.SetActive(true);
        }
    }
}
