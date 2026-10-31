using UnityEngine;

namespace NightCourier
{
    // One scene-level overlay follows the active controls across all gameplay panels.
    public sealed class CourierTutorialPanel : BasePanel
    {
        public CourierTutorialView view;
        private NightCourierPrototype owner;
        protected override bool SlideContent { get { return false; } }
        public void Bind(NightCourierPrototype prototype)
        {
            owner=prototype;
            if(GetComponent<CourierTooltip>()==null)gameObject.AddComponent<CourierTooltip>();
            if(owner.UIFont!=null)
                foreach(var label in GetComponentsInChildren<UnityEngine.UI.Text>(true))label.font=owner.UIFont;
            view.Bind(owner);
        }
        private void LateUpdate()
        {
            if(owner==null||hasRemoved)return;
            view.Refresh();
            // Modal encounters are created after this panel; keep the guide above them.
            // During pause all guide graphics are hidden, leaving the pause UI in control.
            if(owner.Active&&!owner.Tutorial.Finished&&!owner.Run.Finished&&transform.GetSiblingIndex()!=transform.parent.childCount-1)
                transform.SetAsLastSibling();
        }
    }
}
