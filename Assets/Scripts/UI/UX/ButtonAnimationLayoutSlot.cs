using UnityEngine;
using UnityEngine.UI;

// A stable layout item keeps the animated button out of the parent's layout pass.
// Layout measurements still come from the button (including its LayoutElement).
[AddComponentMenu("")]
public class ButtonAnimationLayoutSlot : MonoBehaviour, ILayoutElement, ILayoutIgnorer
{
    private RectTransform button;
    private Vector2 fallbackSize;

    public bool ignoreLayout => button == null || !button.gameObject.activeSelf;
    public float minWidth => Measure(0, false);
    public float minHeight => Measure(1, false);
    public float preferredWidth => Measure(0, true);
    public float preferredHeight => Measure(1, true);
    public float flexibleWidth => button == null ? 0f : LayoutUtility.GetFlexibleWidth(button);
    public float flexibleHeight => button == null ? 0f : LayoutUtility.GetFlexibleHeight(button);
    public int layoutPriority => 1;

    public static void Wrap(RectTransform button)
    {
        var group = button.parent == null ? null : button.parent.GetComponent<LayoutGroup>();
        if (group == null || !group.isActiveAndEnabled) return;
        foreach (var ignorer in button.GetComponents<ILayoutIgnorer>())
            if ((!(ignorer is Behaviour behaviour) || behaviour.isActiveAndEnabled) && ignorer.ignoreLayout) return;

        var slot = new GameObject(button.name + " (Animation Layout)", typeof(RectTransform), typeof(ButtonAnimationLayoutSlot));
        slot.layer = button.gameObject.layer;
        var rect = (RectTransform)slot.transform;
        rect.SetParent(button.parent, false);
        rect.SetSiblingIndex(button.GetSiblingIndex());
        rect.anchorMin = button.anchorMin;
        rect.anchorMax = button.anchorMax;
        rect.pivot = button.pivot;
        rect.sizeDelta = button.sizeDelta;
        rect.anchoredPosition3D = button.anchoredPosition3D;
        rect.localScale = button.localScale;
        rect.localRotation = button.localRotation;
        var layout = slot.GetComponent<ButtonAnimationLayoutSlot>();
        layout.button = button;
        layout.fallbackSize = button.rect.size;

        button.SetParent(rect, false);
        button.anchorMin = Vector2.zero;
        button.anchorMax = Vector2.one;
        button.sizeDelta = Vector2.zero;
        button.anchoredPosition3D = Vector3.zero;
        button.localScale = Vector3.one;
        button.localRotation = Quaternion.identity;
        LayoutRebuilder.MarkLayoutForRebuild(rect);
    }

    private float Measure(int axis, bool preferred)
    {
        if (button == null) return 0f;
        float value = preferred ? LayoutUtility.GetPreferredSize(button, axis) : LayoutUtility.GetMinSize(button, axis);
        // A plain Image reports no preferred size without a sprite. Keep its authored size.
        return preferred && value <= 0f ? fallbackSize[axis] : value;
    }

    public void CalculateLayoutInputHorizontal()
    {
        if (button == null) return;
        foreach (var element in button.GetComponents<ILayoutElement>()) element.CalculateLayoutInputHorizontal();
    }

    public void CalculateLayoutInputVertical()
    {
        if (button == null) return;
        foreach (var element in button.GetComponents<ILayoutElement>()) element.CalculateLayoutInputVertical();
    }

    private void LateUpdate()
    {
        if (button == null) Destroy(gameObject);
    }
}
