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
        private readonly List<StreetEdge> badgeEdges = new List<StreetEdge>();
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
            bool visible = owner.Active && !owner.ConversationActive && !owner.GuideOpen && !run.Finished;
            int key = visible ? run.GetHashCode() : 0;
            unchecked
            {
                if (visible)
                {
                    key = key * 31 + run.Node; key = key * 31 + (int)run.State; key = key * 31 + (int)CourierLocalization.Language;
                    key = key * 31 + run.VehicleIndex; key = key * 31 + (int)run.Durability;
                    key = key * 31 + (panel.CargoOpen ? 1 : 0);
                    foreach (var road in roads) key = key * 31 + road.GetHashCode();
                    foreach (var p in run.Parcels) { key = key * 31 + (int)p.State; key = key * 31 + (p.Damaged ? 1 : 0); key = key * 31 + (p.HeatSensitive ? 1 : 0) + (p.Dry ? 2 : 0) + (p.Fragile ? 4 : 0); }
                }
            }
            if (key != signature)
            {
                signature = key; age = 0;
                graphic.SurfaceRisks = visible && run.State == ShiftState.Docked ? run.RouteSurfaceRisks(roads) : null;
                VisibleRiskCount = graphic.SurfaceRisks != null ? graphic.SurfaceRisks.Count : 0;
                int index = 0;
                badgeEdges.Clear();
                var selectedEdges = new HashSet<StreetEdge>(roads.Where(r=>r.StreetPath!=null).SelectMany(r=>r.StreetPath));
                if (visible)
                    foreach (var edge in CourierStreetMap.Edges.Where(e=>e.Surface!=StreetSurface.Normal))
                    {
                        if (index >= badges.Count) badges.Add(CreateBadge());
                        var badge = badges[index++]; badge.transform.parent.gameObject.SetActive(true);
                        badgeEdges.Add(edge);
                        badge.SetKind(edge.Surface == StreetSurface.Heat ? CourierIconKind.Heat : edge.Surface == StreetSurface.Water ? CourierIconKind.Dry : CourierIconKind.Fragile);
                        bool danger = graphic.SurfaceRisks != null && graphic.SurfaceRisks.ContainsKey(edge);
                        bool selected = selectedEdges.Contains(edge);
                        var paper = badge.transform.parent.GetComponent<Image>();
                        paper.color = danger ? Color.Lerp(CourierColors.Paper,CourierColors.Gold,.45f) : selected ? Color.Lerp(CourierColors.Paper,CourierColors.Mint,.25f) : CourierColors.Paper;
                        var label = badge.transform.parent.GetComponentInChildren<Text>();
                        CourierLocalization.Set(label,CourierRun.SurfaceLabel(edge.Surface));
                        badge.color = label.color = CourierColors.Night;
                        var hint = badge.transform.parent.GetComponent<CourierIconHint>();
                        hint.message = CourierRun.SurfaceLabel(edge.Surface) + "\n" + CourierRun.SurfaceRule(edge.Surface);
                        if (danger) hint.message += "\nWill damage: " + string.Join(", ",graphic.SurfaceRisks[edge].Select(p=>p.Name));
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
            for (int at=0;at<badgeEdges.Count;at++)
            {
                var edge=badgeEdges[at];
                var points = edge.Points; var p = CourierRouteGraphic.MapPoint(points[points.Length / 2], graphic.rectTransform.rect.size);
                var rect = (RectTransform)badges[at].transform.parent;
                bool danger=graphic.SurfaceRisks!=null&&graphic.SurfaceRisks.ContainsKey(edge);
                rect.anchoredPosition = new Vector2(p.x, -p.y - 20); rect.localScale = Vector3.one * (1 + (danger?emphasis:0) * .12f);
            }
            overlay.SetAsLastSibling();
        }
        private CourierIconGraphic CreateBadge()
        {
            var go = new GameObject("SurfaceRisk", typeof(RectTransform), typeof(Image), typeof(CourierIconHint));
            go.transform.SetParent(overlay, false);
            var paper = go.GetComponent<Image>(); paper.sprite = Resources.Load<Sprite>("NightVoyage/HUD/Panel");
            paper.type = Image.Type.Sliced; paper.color = CourierColors.Paper; paper.raycastTarget = true; go.GetComponent<CourierIconHint>().showOnClick=true;
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.sizeDelta = new Vector2(100,32);
            var icon = new GameObject("RiskSymbol", typeof(RectTransform), typeof(CanvasRenderer), typeof(CourierIconGraphic)).GetComponent<CourierIconGraphic>();
            icon.transform.SetParent(rect, false); icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0,.5f);
            icon.rectTransform.pivot = new Vector2(0,.5f); icon.rectTransform.anchoredPosition=new Vector2(5,0);icon.rectTransform.sizeDelta=Vector2.one*23;
            icon.color = CourierColors.Night;icon.raycastTarget=false;
            var label=new GameObject("SurfaceLabel",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(rect,false);label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;
            label.rectTransform.offsetMin=new Vector2(32,2);label.rectTransform.offsetMax=new Vector2(-4,-2);
            label.font=CourierTypography.Body;label.fontSize=18;label.alignment=TextAnchor.MiddleLeft;label.raycastTarget=false;
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
