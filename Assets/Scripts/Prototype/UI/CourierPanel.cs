using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NightCourier
{
    public abstract class CourierPanel : BasePanel
    {
        [Header("Shared Panel References")]
        public Button closeButton,pauseButton;
        protected override bool SlideContent { get { return !(this is CourierRoutePanel) && !(this is CourierHudPanel); } }
        protected NightCourierPrototype owner;
        protected CourierRun Run { get { return owner.Run; } }
        protected override void Awake()
        {
            base.Awake();
            if(pauseButton!=null)pauseButton.onClick.AddListener(()=>{if(owner!=null&&owner.Active)GameManager.Instance.PauseGame();});
            if (closeButton != null) closeButton.onClick.AddListener(() => owner.ClosePlanner());
        }
        public virtual void Bind(NightCourierPrototype prototype)
        {
            owner = prototype;
            CourierTypography.Apply(gameObject);
            Refresh();
        }
        protected virtual void Update() { if (owner != null && !hasRemoved) {
            if(pauseButton!=null)pauseButton.gameObject.SetActive(owner.Active&&!Run.Finished&&(!(this is CourierHudPanel)||(!owner.HasPlanner&&Run.State!=ShiftState.Encounter)));
            Refresh(); } }
        protected abstract void Refresh();
        protected static void Caption(Button button, string text) { button.GetComponentInChildren<Text>().text = text; }
        public override void OpenPanel(string name)
        {
            base.OpenPanel(name);
            var first = GetComponentInChildren<Button>();
            if (first != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
        public override void ClosePanel()
        {
            var hints = GetComponent<CourierTooltip>();
            if(hints!=null)hints.enabled=false;
            var cargoFeedback=GetComponent<CourierCargoFeedback>();
            if(cargoFeedback!=null)cargoFeedback.Clear();
            base.ClosePanel();
        }
    }
}
