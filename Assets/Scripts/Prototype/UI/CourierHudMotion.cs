using System.Collections.Generic;
using UnityEngine;

namespace NightCourier
{
    // Restore offsets before HUD layout runs; animate afterwards without moving its home pose.
    public sealed class CourierHudMotion
    {
        private sealed class Item
        {
            public RectTransform rect;
            public CanvasGroup group;
            public Vector2 applied;
            public float visibility;
        }
        private readonly Dictionary<GameObject, Item> items = new Dictionary<GameObject, Item>();

        public void RestoreLayout()
        {
            foreach (var item in items.Values)
                if (item.rect != null) { item.rect.anchoredPosition -= item.applied; item.applied = Vector2.zero; }
        }

        public void Present(GameObject target, bool visible, Vector2 offset, bool reducedMotion, float deltaTime = -1)
        {
            if (target == null) return;
            if (!items.TryGetValue(target, out var item))
            {
                var group = target.GetComponent<CanvasGroup>();
                if (group == null) group = target.AddComponent<CanvasGroup>();
                item = new Item { rect = target.GetComponent<RectTransform>(), group = group };
                items.Add(target, item);
            }
            // Outgoing controls become inert immediately, while their visuals finish leaving.
            item.group.interactable = visible;
            item.group.blocksRaycasts = visible;
            float destination = visible ? 1 : 0;
            item.visibility = reducedMotion ? destination : Mathf.MoveTowards(item.visibility, destination,
                (deltaTime < 0 ? Time.unscaledDeltaTime : deltaTime) / (visible ? BasePanel.EnterMotionDuration : BasePanel.ExitMotionDuration));
            float eased = 1 - Mathf.Pow(1 - item.visibility, 3);
            item.group.alpha = eased;
            target.SetActive(visible || item.visibility > 0);
            item.applied = offset * (1 - eased);
            if (item.rect != null) item.rect.anchoredPosition += item.applied;
        }
    }
}
