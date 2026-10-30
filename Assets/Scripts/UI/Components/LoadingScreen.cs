using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image progressBar;
    [SerializeField] private Text progressText;

    public void SetAlpha(float alpha)
    {
        canvasGroup.alpha = alpha;
        canvasGroup.blocksRaycasts = alpha > 0f;
        canvasGroup.interactable = false;
    }

    public void SetProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        if (progressBar != null) progressBar.fillAmount = progress;
        if (progressText != null) progressText.text = $"{progress * 100:0}%";
    }
}
