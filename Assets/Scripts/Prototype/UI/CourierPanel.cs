using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace NightCourier
{
    public abstract class CourierPanel : BasePanel
    {
        [Header("Shared Panel References")]
        public Button closeButton,pauseButton;
        private RectTransform window;
        private Vector2 windowHome;
        private Tween entrance;
        protected NightCourierPrototype owner;
        protected CourierRun Run { get { return owner.Run; } }
        protected override void Awake()
        {
            base.Awake();
            window=transform.Find("Window") as RectTransform;if(window!=null)windowHome=window.anchoredPosition;
            if(pauseButton!=null)pauseButton.onClick.AddListener(()=>{if(owner!=null&&owner.Active)GameManager.Instance.PauseGame();});
            if (closeButton != null) closeButton.onClick.AddListener(() => owner.ClosePlanner());
        }
        public virtual void Bind(NightCourierPrototype prototype)
        {
            owner = prototype;
            if (owner.UIFont != null)
                foreach (var text in GetComponentsInChildren<Text>(true)) text.font = owner.UIFont;
            Refresh();
        }
        protected virtual void Update() { if (owner != null) {
            if(pauseButton!=null)pauseButton.gameObject.SetActive(owner.Active&&!Run.Finished&&(!(this is CourierHudPanel)||(!owner.HasPlanner&&Run.State!=ShiftState.Encounter)));
            Refresh(); } }
        protected abstract void Refresh();
        protected static void Caption(Button button, string text) { button.GetComponentInChildren<Text>().text = text; }
        public override void OpenPanel(string name)
        {
            base.OpenPanel(name);
            if(window!=null&&!(this is CourierRoutePanel)){entrance?.Kill();window.anchoredPosition=windowHome+new Vector2(0,-18);entrance=window.DOAnchorPos(windowHome,.18f).SetEase(Ease.OutCubic).SetUpdate(true);}
            var first = GetComponentInChildren<Button>();
            if (first != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
        protected override void OnDestroy(){entrance?.Kill();base.OnDestroy();}
        // Immediate removal prevents old modal controls remaining clickable after switching panels.
        public override void ClosePanel() { ClosePanelImmediate(); }
    }
}
