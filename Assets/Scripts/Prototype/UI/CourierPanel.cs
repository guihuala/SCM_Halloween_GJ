using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NightCourier
{
    public abstract class CourierPanel : BasePanel
    {
        [Header("Shared Panel References")]
        public Button closeButton;
        protected NightCourierPrototype owner;
        protected CourierRun Run { get { return owner.Run; } }
        protected override void Awake()
        {
            base.Awake();
            if (closeButton != null) closeButton.onClick.AddListener(() => owner.ClosePlanner());
        }
        public virtual void Bind(NightCourierPrototype prototype)
        {
            owner = prototype;
            if (owner.UIFont != null)
                foreach (var text in GetComponentsInChildren<Text>(true)) text.font = owner.UIFont;
            Refresh();
        }
        protected virtual void Update() { if (owner != null) Refresh(); }
        protected abstract void Refresh();
        protected static void Caption(Button button, string text) { button.GetComponentInChildren<Text>().text = text; }
        public override void OpenPanel(string name)
        {
            base.OpenPanel(name);
            var first = GetComponentInChildren<Button>();
            if (first != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
        // Immediate removal prevents old modal controls remaining clickable after switching panels.
        public override void ClosePanel() { ClosePanelImmediate(); }
    }
}
