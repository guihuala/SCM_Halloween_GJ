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
        public Text locationText, destinationText, costText, riskText, tipText;
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
            locationText.text = "你在 " + Run.Stops[Run.Node].Name + " · 只可前往相邻站点";
            for (int i = 0; i < stopButtons.Length; i++)
            {
                bool current = i == Run.Node;
                bool reachable = Run.Roads.Any(r => r.Connects(Run.Node) && r.Other(Run.Node) == i);
                stopButtons[i].interactable = reachable;
                stopLabels[i].text = Run.Stops[i].Name + (current ? "\n当前位置" : selected != null && selected.Other(Run.Node) == i ? "\n已选择" : reachable ? "\n点击选择" : "\n尚不可直达");
            }
            departButton.interactable = selected != null;
            if (selected == null)
            {
                destinationText.text = "先选一个相邻站点";
                costText.text = "点击左侧亮起的站点，\n这里会显示耗时和耐久代价。";
                riskText.text = "实线是平稳公路，虚线是碎石路。";
                tipText.text = Run.Node == 0 ? "首次建议：先装上「余温尚存的信」，送到霓虹餐厅。" : "先在货舱查看收件站，再规划下一段路线。";
            }
            else
            {
                destinationText.text = "前往 " + Run.Stops[selected.Other(Run.Node)].Name;
                costText.text = "预计耗时   " + NightCourierPrototype.Minutes(Run.TimeCost(selected)) + "\n耐久消耗   " + Mathf.CeilToInt(Run.WearCost(selected)) + " 点\n当前载重   " + Run.Load + " / " + Run.Car.Capacity + " 格";
                riskText.text = selected.Gravel ? (Run.VehicleIndex == 2 ? "碎石路 · 货车减震可保护易碎件" : "碎石路 · 易碎件会被颠坏！") : "平稳公路 · 不会因路况损坏易碎件";
                tipText.text = Run.Overloaded ? "当前满载：此预估已计入减速和额外损耗。" : "途中可能有事件，请预留时间与耐久。";
            }
        }
    }
}
