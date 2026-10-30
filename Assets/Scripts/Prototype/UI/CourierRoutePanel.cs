using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    public sealed class CourierRoutePanel : CourierPanel
    {
        public Button[] stopButtons;
        public Text[] stopLabels;
        public CourierRouteGraphic mapGraphic;
        public Text locationText, destinationText, costText, riskText;
        public Button departButton, cargoButton;
        private Road selected;
        protected override void Awake()
        {
            base.Awake();
            for (int i = 0; i < stopButtons.Length; i++) { int node = i; stopButtons[i].onClick.AddListener(() => Select(node)); }
            departButton.onClick.AddListener(() => { if (selected != null) owner.Depart(selected); });
            cargoButton.onClick.AddListener(() => owner.ShowPlanner("CourierCargoPanel"));
        }
        public override void Bind(NightCourierPrototype prototype) { mapGraphic.Owner = prototype; base.Bind(prototype); }
        private void Select(int node)
        {
            selected = Run.Roads.FirstOrDefault(r => r.Connects(Run.Node) && r.Other(Run.Node) == node);
            mapGraphic.Selected = selected; mapGraphic.SetVerticesDirty(); Refresh();
        }
        protected override void Refresh()
        {
            locationText.text = "路线 · " + Run.Stops[Run.Node].Name;
            for (int i = 0; i < stopButtons.Length; i++)
            {
                bool current = i == Run.Node;
                bool reachable = Run.Roads.Any(r => r.Connects(Run.Node) && r.Other(Run.Node) == i);
                stopButtons[i].interactable = reachable;
                stopLabels[i].text = Run.Stops[i].Name + (current ? "\n当前" : selected != null && selected.Other(Run.Node) == i ? "\n已选" : "");
            }
            departButton.interactable = selected != null;
            if (selected == null)
            {
                destinationText.text = "选择目的地";
                costText.text = "";
                riskText.text = "";
            }
            else
            {
                destinationText.text = "前往 " + Run.Stops[selected.Other(Run.Node)].Name;
                costText.text = "耗时 " + NightCourierPrototype.Minutes(Run.TimeCost(selected)) + "\n损耗 " + Mathf.CeilToInt(Run.WearCost(selected)) + " 耐久";
                riskText.text = selected.Gravel ? (Run.VehicleIndex == 2 ? "碎石路 · 减震保护" : "碎石路 · 易碎件会破损") : "平稳公路";
            }
        }
    }
}
