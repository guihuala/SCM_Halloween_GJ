using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    // Transient paper notes live outside scroll masks and never intercept input.
    public sealed class CourierFriendlyFeedback : MonoBehaviour
    {
        private sealed class Note
        {
            public RectTransform rect, target;
            public Text label;
            public CanvasGroup group;
            public float age, lifetime, createdAt;
            public bool drift;
        }
        private readonly List<Note> notes = new List<Note>();
        private readonly HashSet<Parcel> warned = new HashSet<Parcel>();
        private CourierRoutePanel panel;
        private NightCourierPrototype owner;
        private CourierRun observed;
        private Parcel order;
        private int turns, vehicle;
        private float durability, lastFailure = -10;
        private RectTransform overlay;
        private readonly Vector3[] corners = new Vector3[4];
        public int ActiveNotes => notes.Count;
        public void Initialize(CourierRoutePanel page, NightCourierPrototype prototype)
        {
            Clear(); panel = page; owner = prototype; observed = prototype.Run; warned.Clear();
            order = observed.ActiveOrder; turns = order != null ? observed.TurnsFor(order) : 0;
            vehicle = observed.VehicleIndex; durability = observed.Durability;
            if (overlay == null)
            {
                overlay = new GameObject("FriendlyFeedback", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
                overlay.SetParent(transform, false); overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
                overlay.offsetMin = overlay.offsetMax = Vector2.zero;
                var group = overlay.GetComponent<CanvasGroup>(); group.blocksRaycasts = group.interactable = false;
            }
        }
        public void Reject(RectTransform target, string reason)
        {
            if (owner == null || !owner.Active || string.IsNullOrWhiteSpace(reason) || Time.unscaledTime - lastFailure < .5f) return;
            lastFailure = Time.unscaledTime;
            Show(target, reason, CourierIconKind.Warning, 2.6f, false);
            Pulse(target);
        }
        private void Pulse(RectTransform target)
        {
            if (target == null || panel.MapReducedMotion) return;
            var pulse = target.GetComponent<CourierValuePulse>() ?? target.gameObject.AddComponent<CourierValuePulse>();
            pulse.Play();
        }
        public void VehicleExchanged()
        {
            if (owner == null || panel == null) return;
            // Replace earlier notes so repeated exchanges cannot stack over the player.
            Clear();
            string message = "Vehicle exchanged: " + owner.Run.Car.Name;
            var hud = panel.vehicleArtwork != null ? (RectTransform)panel.vehicleArtwork.transform : panel.durabilityText.rectTransform;
            var noteAnchor = panel.possessButton != null && panel.possessButton.gameObject.activeInHierarchy
                ? (RectTransform)panel.possessButton.transform : hud;
            Show(noteAnchor, message + "\nVehicle turns remaining: " + Mathf.CeilToInt(owner.Run.Durability), CourierIconKind.Check, 2.6f, false);
            Pulse(hud); Pulse(panel.durabilityText.rectTransform);
            var marker = panel.mapArtwork != null ? panel.mapArtwork.travelerIcon : null;
            if (marker != null)
            {
                var target = (RectTransform)marker.transform;
                Show(target, message, CourierIconKind.Check, 2.6f, false);
                Pulse(target);
            }
        }
        public void Show(RectTransform target, string text, CourierIconKind icon, float lifetime = 1.2f, bool drift = true)
        {
            if (overlay == null || target == null) return;
            if (notes.Count >= 6) { Destroy(notes[0].rect.gameObject); notes.RemoveAt(0); }
            var rect = new GameObject("FeedbackNote", typeof(RectTransform), typeof(CanvasGroup), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(overlay, false); rect.anchorMin = rect.anchorMax = overlay.pivot; rect.pivot = new Vector2(.5f, 0);
            var image = rect.GetComponent<Image>(); image.sprite = Resources.Load<Sprite>("NightVoyage/HUD/Panel");
            image.type = Image.Type.Sliced; image.color = CourierColors.Paper; image.raycastTarget = false;
            var label = new GameObject("FeedbackText", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(rect, false); label.font = CourierTypography.Body; label.fontSize = 26;
            label.color = CourierColors.Night; label.raycastTarget = false; label.alignment = TextAnchor.MiddleLeft;
            CourierLocalization.Set(label, text); label.horizontalOverflow = HorizontalWrapMode.Wrap;
            float width = drift ? 164 : Mathf.Min(520, overlay.rect.width - 32);
            rect.sizeDelta = new Vector2(width, 68);
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(58, 12); label.rectTransform.offsetMax = new Vector2(-16, -12);
            if (!drift) rect.sizeDelta = new Vector2(width, Mathf.Max(68, label.preferredHeight + 28));
            var symbol = new GameObject("FeedbackSymbol", typeof(RectTransform), typeof(CanvasRenderer), typeof(CourierIconGraphic)).GetComponent<CourierIconGraphic>();
            symbol.transform.SetParent(rect, false); symbol.SetKind(icon); symbol.color = CourierColors.Night; symbol.raycastTarget = false;
            symbol.rectTransform.anchorMin = symbol.rectTransform.anchorMax = new Vector2(0, .5f);
            symbol.rectTransform.anchoredPosition = new Vector2(30, 0); symbol.rectTransform.sizeDelta = Vector2.one * 32;
            notes.Add(new Note { rect = rect, target = target, label = label, group = rect.GetComponent<CanvasGroup>(), lifetime = lifetime, drift = drift, createdAt = Time.unscaledTime });
            overlay.SetAsLastSibling(); Position(notes[notes.Count - 1]);
        }
        private void Position(Note note)
        {
            if (note.target == null) return;
            note.target.GetWorldCorners(corners);
            var low = overlay.InverseTransformPoint(corners[0]); var high = overlay.InverseTransformPoint(corners[2]);
            float y = low.y - note.rect.rect.height - 14;
            if (y < overlay.rect.yMin + 12) y = high.y + 14;
            if (note.drift && !panel.MapReducedMotion) y += Mathf.Min(1, note.age / note.lifetime) * 18;
            note.rect.anchoredPosition = new Vector2(Mathf.Clamp((low.x + high.x) * .5f,
                overlay.rect.xMin + note.rect.rect.width * .5f + 12, overlay.rect.xMax - note.rect.rect.width * .5f - 12),
                Mathf.Clamp(y, overlay.rect.yMin + 12, overlay.rect.yMax - note.rect.rect.height - 12));
        }
        private void LateUpdate()
        {
            if (owner == null || panel == null) return;
            var run = owner.Run;
            if (observed != run)
            { Clear(); warned.Clear(); observed = run; order = run.ActiveOrder; turns = order != null ? run.TurnsFor(order) : 0; vehicle = run.VehicleIndex; durability = run.Durability; }
            else
            {
                if (order != null)
                {
                    int current = run.TurnsFor(order), change = current - turns;
                    if (change > 0)
                    {
                        Show(panel.loadText.rectTransform, "+" + change, CourierIconKind.Clock); Pulse(panel.loadText.rectTransform);
                        int remaining = run.TargetTurns(order) - current;
                        if (order.State == ParcelState.Loaded && remaining <= 2 && remaining >= 0 && warned.Add(order))
                            Show(panel.loadText.rectTransform, "Order turns left: " + remaining, CourierIconKind.Warning, 2.4f, false);
                    }
                }
                if (vehicle >= 0 && vehicle < run.Durabilities.Length && (run.VehicleIndex == vehicle || run.IsGhost))
                {
                    int change = Mathf.CeilToInt(run.Durabilities[vehicle]) - Mathf.CeilToInt(durability);
                    if (change != 0) { Show(panel.durabilityText.rectTransform, (change > 0 ? "+" : "") + change, CourierIconKind.Wrench); Pulse(panel.durabilityText.rectTransform); }
                }
                order = run.ActiveOrder; turns = order != null ? run.TurnsFor(order) : 0; vehicle = run.VehicleIndex; durability = run.Durability;
            }
            for (int i = notes.Count - 1; i >= 0; i--)
            {
                var note = notes[i]; note.age = Time.unscaledTime - note.createdAt;
                if (note.age >= note.lifetime || note.target == null || !note.target.gameObject.activeInHierarchy)
                { Destroy(note.rect.gameObject); notes.RemoveAt(i); continue; }
                Position(note); note.group.alpha = Mathf.Clamp01((note.lifetime - note.age) / .22f);
            }
            // Keep simultaneous clock alerts and deltas readable.
            for (int i = 0; i < notes.Count; i++)
                for (int j = 0; j < i; j++)
                    if (notes[i].target == notes[j].target)
                    { var p = notes[i].rect.anchoredPosition; p.y = Mathf.Max(overlay.rect.yMin + 12, notes[j].rect.anchoredPosition.y - notes[i].rect.rect.height - 8); notes[i].rect.anchoredPosition = p; }
        }
        public void Clear() { foreach (var note in notes) if (note.rect != null) { note.rect.gameObject.SetActive(false); Destroy(note.rect.gameObject); } notes.Clear(); }
        private void OnEnable() { CourierLocalization.Changed += ResizeNotes; }
        private void ResizeNotes()
        {
            foreach (var note in notes)
                if (!note.drift && note.label != null)
                    note.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(68, note.label.preferredHeight + 28));
        }
        private void OnDisable() { CourierLocalization.Changed -= ResizeNotes; Clear(); observed = null; }
    }
}
