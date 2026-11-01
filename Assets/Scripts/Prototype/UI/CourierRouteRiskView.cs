using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    public sealed class CourierRouteRiskView : MonoBehaviour
    {
        private CourierRoutePanel panel;
        private NightCourierPrototype owner;
        private CourierRouteGraphic graphic;
        private RectTransform overlay;
        private int signature;
        private float age = 2;
        private readonly List<CourierIconGraphic> badges = new List<CourierIconGraphic>();
        public int VisibleRiskCount { get; private set; }
        public void Initialize(CourierRoutePanel page, NightCourierPrototype prototype)
        {
            panel = page; owner = prototype; graphic = page.mapGraphic; signature = int.MinValue;
            if (overlay == null)
            {
                overlay = new GameObject("RouteRiskBadges", typeof(RectTransform)).GetComponent<RectTransform>();
                overlay.SetParent(graphic.transform.parent, false); overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
                overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            }
        }
        private void LateUpdate()
        {
            if (owner == null || panel == null) return;
            var run = owner.Run;
            var roads = graphic.PreviewRoute.Count > 0 ? graphic.PreviewRoute : graphic.SelectedRoute;
            bool visible = owner.Active && !owner.ConversationActive && !owner.GuideOpen && !run.Finished && run.State == ShiftState.Docked;
            int key = visible ? run.GetHashCode() : 0;
            unchecked
            {
                if (visible)
                {
                    key = key * 31 + run.VehicleIndex; key = key * 31 + (int)run.Durability;
                    key = key * 31 + (panel.CargoOpen ? 1 : 0);
                    foreach (var road in roads) key = key * 31 + road.GetHashCode();
                    foreach (var p in run.Parcels) { key = key * 31 + (int)p.State; key = key * 31 + (p.Damaged ? 1 : 0); key = key * 31 + (p.HeatSensitive ? 1 : 0) + (p.Dry ? 2 : 0) + (p.Fragile ? 4 : 0); }
                }
            }
            if (key != signature)
            {
                signature = key; age = 0;
                graphic.SurfaceRisks = visible ? run.RouteSurfaceRisks(roads) : null;
                VisibleRiskCount = graphic.SurfaceRisks != null ? graphic.SurfaceRisks.Count : 0;
                int index = 0;
                if (graphic.SurfaceRisks != null)
                    foreach (var entry in graphic.SurfaceRisks)
                    {
                        if (index >= badges.Count) badges.Add(CreateBadge());
                        var badge = badges[index++]; badge.transform.parent.gameObject.SetActive(true);
                        badge.SetKind(entry.Key.Surface == StreetSurface.Heat ? CourierIconKind.Heat : entry.Key.Surface == StreetSurface.Water ? CourierIconKind.Dry : CourierIconKind.Fragile);
                        var hint = badge.GetComponent<CourierIconHint>();
                        string surface = entry.Key.Surface == StreetSurface.Heat ? "Heat" : entry.Key.Surface == StreetSurface.Water ? "Water" : "Bumps";
                        hint.message = surface + "\nWill damage: " + string.Join(", ", entry.Value.Select(p => p.Name));
                    }
                for (int i = index; i < badges.Count; i++) badges[i].transform.parent.gameObject.SetActive(false);
                if (!panel.MapReducedMotion && graphic.SurfaceRisks != null)
                {
                    var affected = new HashSet<Parcel>(graphic.SurfaceRisks.Values.SelectMany(p => p));
                    foreach (var row in panel.GetComponentsInChildren<CourierCargoRow>())
                        if (affected.Contains(row.Item) && row.cargoArtwork != null)
                            (row.cargoArtwork.GetComponent<CourierValuePulse>() ?? row.cargoArtwork.gameObject.AddComponent<CourierValuePulse>()).Play();
                }
                graphic.SetVerticesDirty();
            }
            age += Time.unscaledDeltaTime;
            float emphasis = panel.MapReducedMotion || VisibleRiskCount == 0 ? 0 : Mathf.Sin(Mathf.Clamp01(age / 1.1f) * Mathf.PI);
            if (Mathf.Abs(graphic.riskEmphasis - emphasis) > .001f) { graphic.riskEmphasis = emphasis; graphic.SetVerticesDirty(); }
            if (graphic.SurfaceRisks == null) return;
            int at = 0;
            foreach (var entry in graphic.SurfaceRisks)
            {
                var points = entry.Key.Points; var p = CourierRouteGraphic.MapPoint(points[points.Length / 2], graphic.rectTransform.rect.size);
                var rect = (RectTransform)badges[at++].transform.parent;
                rect.anchoredPosition = new Vector2(p.x, -p.y - 23); rect.localScale = Vector3.one * (1 + emphasis * .12f);
            }
            overlay.SetAsLastSibling();
        }
        private CourierIconGraphic CreateBadge()
        {
            var go = new GameObject("SurfaceRisk", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(overlay, false);
            var paper = go.GetComponent<Image>(); paper.sprite = Resources.Load<Sprite>("NightVoyage/HUD/Panel");
            paper.type = Image.Type.Sliced; paper.color = CourierColors.Paper; paper.raycastTarget = false;
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.sizeDelta = Vector2.one * 38;
            var icon = new GameObject("RiskSymbol", typeof(RectTransform), typeof(CanvasRenderer), typeof(CourierIconGraphic), typeof(CourierIconHint)).GetComponent<CourierIconGraphic>();
            icon.transform.SetParent(rect, false); icon.rectTransform.anchorMin = Vector2.zero; icon.rectTransform.anchorMax = Vector2.one;
            icon.rectTransform.offsetMin = Vector2.one * 5; icon.rectTransform.offsetMax = Vector2.one * -5;
            icon.color = CourierColors.Night;
            return icon;
        }
        private void OnDisable()
        {
            if (graphic != null) { graphic.SurfaceRisks = null; graphic.riskEmphasis = 0; graphic.SetVerticesDirty(); }
            foreach (var badge in badges) if (badge != null) badge.transform.parent.gameObject.SetActive(false);
            signature = int.MinValue;
        }
        private void OnDestroy() { if (overlay != null) Destroy(overlay.gameObject); }
    }
}
