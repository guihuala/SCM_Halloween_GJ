using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    public sealed class CourierCargoPanel : CourierPanel
    {
        public Text capacityText, waitingEmptyText, loadedEmptyText, adviceText;
        public Transform waitingContent, loadedContent;
        public CourierCargoRow rowPrefab;
        public Button continueButton;
        private readonly List<CourierCargoRow> rows = new List<CourierCargoRow>();
        private CourierCargoFeedback cargoFeedback;
        protected override void Awake()
        {
            base.Awake(); continueButton.onClick.AddListener(() => owner.ShowPlanner("CourierRoutePanel"));
        }
        public override void Bind(NightCourierPrototype prototype)
        {
            foreach (var row in rows) Destroy(row.gameObject);
            rows.Clear();
            foreach (var parcel in prototype.Run.Parcels)
            {
                var row = Instantiate(rowPrefab, waitingContent); row.Bind(prototype, parcel); rows.Add(row);
            }
            if(cargoFeedback==null)cargoFeedback=gameObject.AddComponent<CourierCargoFeedback>();
            cargoFeedback.Initialize(prototype,rows,capacityText.rectTransform,parcel=>waitingEmptyText.rectTransform,capacityText);
            base.Bind(prototype);
        }
        protected override void Refresh()
        {
            if(cargoFeedback!=null)cargoFeedback.Observe(reducedPanelMotion);
            capacityText.text = "Orders " + Run.CompletedBatches + " / " + Run.BatchCount;
            adviceText.gameObject.SetActive(Run.Overloaded);
            adviceText.text = "Deliver the active order before accepting another.";
            int waiting = 0, loaded = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                var parcel = Run.Parcels[i]; bool onboard = parcel.State == ParcelState.Loaded;
                bool visible = (onboard || parcel.State == ParcelState.Waiting) && Run.Batch(parcel).First() == parcel;
                rows[i].gameObject.SetActive(visible);
                if (!visible) continue;
                var parent = onboard ? loadedContent : waitingContent;
                if (rows[i].transform.parent != parent) rows[i].transform.SetParent(parent, false);
                rows[i].Refresh(); if (onboard) loaded++; else waiting++;
            }
            waitingEmptyText.gameObject.SetActive(waiting == 0); loadedEmptyText.gameObject.SetActive(loaded == 0);
            Caption(continueButton, loaded > 0 ? "Plan Route" : "Depart Empty");
        }
    }
}
