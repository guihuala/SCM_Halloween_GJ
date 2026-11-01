using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    // A quiet, readable travel ribbon. Animation stays inside the progress bar.
    public sealed class CourierTravelStatus : MonoBehaviour
    {
        private RectTransform root;
        private Image fill, track, head;
        private readonly Image[] dashes = new Image[3];
        private Text destination, state, percent;
        private float phase;

        public static CourierTravelStatus Create(Transform parent, Image fill, Image track)
        {
            var go = new GameObject("TravelStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            go.layer = 5; go.transform.SetParent(parent, false);
            var view = go.AddComponent<CourierTravelStatus>();
            view.root = (RectTransform)go.transform;
            var paper = go.GetComponent<Image>();
            paper.sprite = Resources.Load<Sprite>("NightVoyage/HUD/Panel");
            paper.type = Image.Type.Sliced; paper.color = CourierColors.Paper; paper.raycastTarget = false;
            view.fill = fill; view.track = track;
            track.transform.SetParent(go.transform, false); fill.transform.SetParent(go.transform, false);
            track.color = CourierColors.Night; fill.color = CourierColors.Mint;
            track.raycastTarget = fill.raycastTarget = false;
            view.destination = view.Label("Destination", 26, TextAnchor.MiddleLeft);
            view.state = view.Label("TravelState", 20, TextAnchor.MiddleLeft);
            view.state.color = Color.Lerp(CourierColors.Night, CourierColors.Paper, .2f);
            view.percent = view.Label("TravelPercent", 26, TextAnchor.MiddleRight);
            for (int i = 0; i < view.dashes.Length; i++)
                view.dashes[i] = view.Mark("TravelDash" + i, CourierColors.Alpha(CourierColors.Night, .24f));
            view.head = view.Mark("ProgressHead", CourierColors.Paper);
            view.Layout();
            return view;
        }

        private Text Label(string name, int size, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.layer = 5; go.transform.SetParent(transform, false);
            var label = go.GetComponent<Text>();
            label.font = CourierTypography.Body; label.fontSize = size; label.color = CourierColors.Night;
            label.alignment = alignment; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private Image Mark(string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = 5; go.transform.SetParent(transform, false);
            var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            return image;
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
        }

        public void Layout()
        {
            var parent = (RectTransform)transform.parent;
            float width = Mathf.Min(704, Mathf.Max(320, parent.rect.width - 64));
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, 0);
            root.anchoredPosition = new Vector2(0, 32); root.sizeDelta = new Vector2(width, 120);
            Place(destination.rectTransform, 28, 10, width - 152, 36);
            Place(percent.rectTransform, width - 116, 10, 88, 36);
            Place(state.rectTransform, 28, 46, width - 56, 30);
            Place(track.rectTransform, 28, 88, width - 56, 14);
            Place(fill.rectTransform, 28, 88, width - 56, 14);
        }

        public void Refresh(CourierRun run, bool active, bool reducedMotion)
        {
            bool waiting = run.State == ShiftState.Docked && run.HasJourney;
            bool moving = (run.State == ShiftState.Driving || run.State == ShiftState.Pushing) && active;
            bool animate = moving && !reducedMotion;
            if (animate) phase = Mathf.Repeat(phase + Time.unscaledDeltaTime * .45f, 1f);
            int target = run.IsReturning ? run.TravelDestination : run.PlannedDestination;
            if (target < 0 || target >= run.Stops.Length) target = run.TravelDestination;
            CourierLocalization.Set(destination, "Destination · " + run.Stops[Mathf.Clamp(target, 0, run.Stops.Length - 1)].Name);
            string caption = waiting ? "Travel paused · continue with E" :
                run.State == ShiftState.Encounter ? "Road event · travel paused" : !active ? "Travel paused" :
                run.IsReturning ? "Returning to previous stop" : run.State == ShiftState.Pushing ? "Pushing to next stop" : "In transit";
            if (moving) caption += " · Current leg";
            CourierLocalization.Set(state, caption);
            CourierLocalization.Set(percent, waiting ? "—" : Mathf.FloorToInt(run.DisplayProgress * 100) + "%");
            // A retreat can leave the player waiting at a junction. Do not call it
            // 100% complete while the intended destination still needs another leg.
            track.gameObject.SetActive(!waiting); fill.gameObject.SetActive(!waiting);
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, waiting ? 92 : 120);
            fill.color = run.State == ShiftState.Encounter ? CourierColors.Gold : CourierColors.Mint;
            float width = fill.rectTransform.rect.width;
            head.gameObject.SetActive(!waiting && fill.fillAmount > .012f);
            Place(head.rectTransform, 28 + Mathf.Clamp(fill.fillAmount * width, 2, width - 2) - 2, 86, 4, 18);
            head.color = CourierColors.Alpha(CourierColors.Paper, animate ? .6f + .16f * Mathf.Sin(phase * Mathf.PI * 2) : .7f);
            for (int i = 0; i < dashes.Length; i++)
            {
                // Short directional marks never travel beyond the revealed fill.
                float x = Mathf.Repeat(phase + i / 3f, 1f) * Mathf.Max(0, fill.fillAmount * width - 20);
                dashes[i].gameObject.SetActive(animate && fill.fillAmount * width > 32);
                Place(dashes[i].rectTransform, 32 + x, 94, 8, 2);
            }
            var group = GetComponent<CanvasGroup>();
            group.interactable = group.blocksRaycasts = false;
        }
    }
}
