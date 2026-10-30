using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectMask2D progressMask;
    [SerializeField] private Text progressText;
    [SerializeField, Min(0f)] private float progressAnimationDuration = 0.25f;

    private float displayedProgress;
    private float targetProgress;

    public bool IsProgressComplete => Mathf.Approximately(displayedProgress, targetProgress);

    public void SetAlpha(float alpha)
    {
        canvasGroup.alpha = alpha;
        canvasGroup.blocksRaycasts = alpha > 0f;
        canvasGroup.interactable = false;
    }

    public void SetProgress(float progress, bool immediate = false)
    {
        targetProgress = Mathf.Clamp01(progress);
        if (immediate || targetProgress == 0f || progressAnimationDuration <= 0f)
            displayedProgress = targetProgress;
        UpdateProgressVisual();
    }

    private void LateUpdate()
    {
        displayedProgress = progressAnimationDuration <= 0f ? targetProgress :
            Mathf.MoveTowards(displayedProgress, targetProgress, Time.unscaledDeltaTime / progressAnimationDuration);
        // Recalculate clipping after resizing without changing the Image's rect,
        // scale, sprite or Sliced settings. The percentage text is outside the mask.
        UpdateProgressVisual();
    }

    private void UpdateProgressVisual()
    {
        if (progressMask != null)
        {
            var padding = new Vector4(0f, 0f,
                Mathf.Max(0f, progressMask.canvasRect.width) * (1f - displayedProgress), 0f);
            if (progressMask.padding != padding) progressMask.padding = padding;
        }
        if (progressText != null) progressText.text = $"{displayedProgress * 100:0}%";
    }
}
