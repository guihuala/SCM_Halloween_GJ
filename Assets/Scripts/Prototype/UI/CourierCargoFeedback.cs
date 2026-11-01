using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    // Observe committed state changes, so clicks, drops and automatic deliveries share feedback.
    // All flying artwork lives outside the scroll masks and never receives input.
    public sealed class CourierCargoFeedback : MonoBehaviour
    {
        private sealed class Flight
        {
            public Image[] images;
            public Vector2 from;
            public RectTransform destination;
            public float age;
            public Color tint;
        }
        private readonly Dictionary<Parcel, ParcelState> states = new Dictionary<Parcel, ParcelState>();
        private readonly List<Flight> flights = new List<Flight>();
        private NightCourierPrototype owner;
        private IList<CourierCargoRow> rows;
        private RectTransform trunk, overlay;
        private Func<Parcel, RectTransform> recipient;
        private CourierCargoIconCatalog catalog;
        private Text notice;
        private Vector2 noticeHome;
        private float noticeAge = 2;
        private const float FlightDuration = .55f;
        public int ActiveFlights { get { return flights.Count; } }

        public void Initialize(NightCourierPrototype prototype, IList<CourierCargoRow> cargoRows,
            RectTransform trunkTarget, Func<Parcel, RectTransform> deliveryTarget, params Text[] counters)
        {
            Clear(); states.Clear(); owner = prototype; rows = cargoRows;
            trunk = trunkTarget; recipient = deliveryTarget;
            catalog = Resources.Load<CourierCargoIconCatalog>("NightVoyage/CargoIcons");
            foreach (var parcel in owner.Run.Parcels) states[parcel] = parcel.State;

        }

        public void Observe(bool reducedMotion)
        {
            if (owner == null) return;
            if (reducedMotion) Clear();
            int loaded = 0, delivered = 0;
            foreach (var parcel in owner.Run.Parcels)
            {
                if (!states.TryGetValue(parcel, out var previous)) { states[parcel] = parcel.State; continue; }
                if (previous == parcel.State) continue;
                states[parcel] = parcel.State;
                bool loading = parcel.State == ParcelState.Loaded;
                bool delivering = previous == ParcelState.Loaded && parcel.State == ParcelState.Delivered;
                if (!loading && !delivering) continue;
                if (loading) loaded++; else delivered++;
                if (reducedMotion) continue;
                var first = owner.Run.Batch(parcel).First();
                var row = rows.FirstOrDefault(r => r != null && r.Item == first);
                Vector3 from = Center(trunk);
                if (loading && row != null && row.cargoArtwork != null && row.cargoArtwork.gameObject.activeInHierarchy)
                    from = row.DropFeedbackOrigin ?? Center(row.cargoArtwork.rectTransform);
                var destination = loading ? trunk : recipient?.Invoke(parcel);
                if (destination == null) destination = trunk;
                Fly(catalog != null ? catalog.ForParcel(owner.Run.Parcels.IndexOf(parcel)) : null,
                    from, destination, loading, (loaded + delivered - 1) * .06f);
            }
            if (reducedMotion || loaded + delivered == 0) return;
            ShowNotice(delivered > 0 ? "Delivered ×" + delivered : "+" + loaded + " on board", delivered > 0);
        }

        private static Vector3 Center(RectTransform rect)
        { return rect != null ? rect.TransformPoint(rect.rect.center) : Vector3.zero; }
        private void EnsureOverlay()
        {
            if (overlay != null) return;
            var go = new GameObject("CargoTransferFeedback", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(transform, false); overlay = (RectTransform)go.transform;
            overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            var group = go.GetComponent<CanvasGroup>(); group.interactable = group.blocksRaycasts = false;
        }
        private void Fly(Sprite sprite, Vector3 origin, RectTransform destination, bool loading, float delay)
        {
            if (destination == null) return;
            EnsureOverlay(); overlay.SetAsLastSibling();
            var flight = new Flight { images = new Image[1], from = overlay.InverseTransformPoint(origin),
                destination = destination, age = -delay,
                tint = loading ? CourierColors.Mint : CourierColors.Gold };
            for (int i = 0; i < flight.images.Length; i++)
            {
                var go = new GameObject("FlyingCargo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(overlay, false);
                var image = go.GetComponent<Image>(); image.sprite = sprite; image.preserveAspect = true;
                image.raycastTarget = false; image.color = Color.clear;
                image.rectTransform.sizeDelta = new Vector2(76,76); flight.images[i] = image;
            }
            flights.Add(flight);
        }
        private void ShowNotice(string message, bool delivered)
        {
            EnsureOverlay();
            if (notice == null)
            {
                var go = new GameObject("CargoChange", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                go.transform.SetParent(overlay, false); notice = go.GetComponent<Text>();
                notice.font = owner.UIFont; notice.fontSize = 28; notice.alignment = TextAnchor.MiddleLeft;
                notice.raycastTarget = false; notice.rectTransform.sizeDelta = new Vector2(300,48);
            }
            notice.transform.SetAsLastSibling(); NightCourier.CourierLocalization.Set(notice, message);
            notice.color = delivered ? CourierColors.Gold : CourierColors.Mint;
            noticeHome = (Vector2)overlay.InverseTransformPoint(Center(trunk)) + new Vector2(64,-48);
            // Keep the entire label inside the panel even on smaller viewports.
            noticeHome.x = Mathf.Clamp(noticeHome.x, overlay.rect.xMin + 160, overlay.rect.xMax - 160);
            noticeHome.y = Mathf.Clamp(noticeHome.y, overlay.rect.yMin + 40, overlay.rect.yMax - 40);
            noticeAge = 0; notice.gameObject.SetActive(true);
        }
        private void Update()
        {
            if (overlay == null) return;
            for (int i = flights.Count - 1; i >= 0; i--)
            {
                var flight = flights[i]; flight.age += Time.unscaledDeltaTime;
                if (flight.destination == null || flight.age >= FlightDuration)
                {
                    foreach (var image in flight.images) if (image != null) Destroy(image.gameObject);
                    flights.RemoveAt(i); continue;
                }
                if (flight.age < 0) continue;
                Vector2 to = overlay.InverseTransformPoint(Center(flight.destination));
                Vector2 control = (flight.from + to) * .5f + Vector2.up * Mathf.Clamp(Vector2.Distance(flight.from,to)*.22f,36,140);
                for (int trail = 0; trail < flight.images.Length; trail++)
                {
                    float t = Mathf.Clamp01(flight.age / FlightDuration - trail * .055f);
                    float u = t * t * (3 - 2 * t);
                    var image = flight.images[trail];
                    image.rectTransform.anchoredPosition = (1-u)*(1-u)*flight.from + 2*(1-u)*u*control + u*u*to;
                    image.rectTransform.localScale = Vector3.one;
                    image.rectTransform.localRotation = Quaternion.identity;
                    var tint = trail == 0 && image.sprite != null ? Color.white : flight.tint;
                    tint.a = Mathf.Clamp01(t*12) * Mathf.Clamp01((1-t)*8) * (trail == 0 ? 1 : .16f / trail);
                    image.color = tint;
                }
            }
            if (notice != null && noticeAge < 1.1f)
            {
                noticeAge += Time.unscaledDeltaTime;
                notice.rectTransform.anchoredPosition = noticeHome + Vector2.up * (noticeAge * 28);
                var tint = notice.color; tint.a = Mathf.Clamp01((1.1f-noticeAge)*3); notice.color = tint;
                if (noticeAge >= 1.1f) notice.gameObject.SetActive(false);
            }
        }
        public void Clear()
        {
            flights.Clear();
            if (overlay != null) { overlay.gameObject.SetActive(false); Destroy(overlay.gameObject); overlay = null; notice = null; }
            noticeAge = 2;
        }
        private void OnDisable() { Clear(); }
    }
}
