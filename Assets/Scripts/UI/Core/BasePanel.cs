using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI面板的基类：先显示遮罩背景，再显示卡片内容。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class BasePanel : MonoBehaviour
{
    public const float EnterMotionDuration = .28f, ExitMotionDuration = .18f;
    protected bool hasRemoved = false;
    protected string panelName;
    protected CanvasGroup canvasGroup;

    [Header("动画设置")]
    [SerializeField, Min(0f)] protected float backgroundFadeDuration = 0.08f;
    [SerializeField] protected float fadeInDuration = EnterMotionDuration;
    [SerializeField] protected float fadeOutDuration = ExitMotionDuration;
    [SerializeField] protected Ease fadeInEase = Ease.OutCubic;
    [SerializeField] protected Ease fadeOutEase = Ease.InCubic;
    [SerializeField] protected bool scaleAnimation = false;
    [SerializeField] protected bool reducedPanelMotion;
    protected virtual bool SlideContent { get { return true; } }
    private static readonly Vector2 SlideOffset = new Vector2(0, -48);
    [SerializeField] protected Vector2 scaleFrom = new Vector2(0.8f, 0.8f);
    [SerializeField] protected float scaleDuration = 0.3f;

    private Sequence transition;
    private Graphic background;
    private float backgroundAlpha;
    private readonly List<CardState> cards = new List<CardState>();

    private class CardState
    {
        public Transform transform;
        public CanvasGroup group;
        public float alpha;
        public Vector3 scale;
        public Vector2 position;
    }

    protected virtual void OnDestroy() => transition?.Kill();

    protected virtual void Awake()
    {
        NightCourier.CourierTheme.Apply(gameObject);
        canvasGroup = GetComponent<CanvasGroup>();
        background = GetComponent<Graphic>();
        if (background != null) backgroundAlpha = background.color.a;

        // Keep the hierarchy and serialized button references intact. Existing
        // panels use a root Image as the backdrop and direct children as content.
        foreach (Transform child in transform)
        {
            var group = child.GetComponent<CanvasGroup>();
            if (group == null) group = child.gameObject.AddComponent<CanvasGroup>();
            cards.Add(new CardState
            {
                transform = child,
                group = group,
                alpha = group.alpha,
                scale = child.localScale,
                position = child is RectTransform rect ? rect.anchoredPosition : Vector2.zero
            });
        }
    }

    public virtual void OpenPanel(string name)
    {
        bool interrupted = transition != null && transition.IsActive();
        transition?.Kill();
        panelName = name;
        hasRemoved = false;
        gameObject.SetActive(true);
        if (!interrupted) canvasGroup.alpha = 1f;
        SetInteractable(false);
        // The backdrop blocks clicks immediately, including while it is fading in.
        canvasGroup.blocksRaycasts = true;

        if (background != null)
        {
            Color color = background.color;
            if (!interrupted) color.a = 0f;
            background.color = color;
        }
        foreach (var card in cards)
        {
            if (card.transform == null) continue;
            if (!interrupted)
            {
                card.group.alpha = 0f;
                card.transform.localScale = scaleAnimation && !reducedPanelMotion ? GetFromScale(card.scale) : card.scale;
                if (SlideContent && card.transform is RectTransform rect)
                    rect.anchoredPosition = card.position + (reducedPanelMotion ? Vector2.zero : SlideOffset);
            }
        }

        Sequence s = transition = DOTween.Sequence().SetUpdate(true).SetTarget(this);
        float contentStart = !reducedPanelMotion && background != null ? Mathf.Max(0f, backgroundFadeDuration) : 0f;
        float enterDuration = reducedPanelMotion ? 0 : Mathf.Max(0f, fadeInDuration);
        s.Insert(0f, canvasGroup.DOFade(1f, enterDuration));
        if (background != null)
            s.Insert(0f, background.DOFade(backgroundAlpha, contentStart).SetEase(fadeInEase));

        foreach (var card in cards)
        {
            if (card.transform == null) continue;
            s.Insert(contentStart, card.group.DOFade(card.alpha, enterDuration).SetEase(fadeInEase));
            if (SlideContent && card.transform is RectTransform rect)
                s.Insert(contentStart, rect.DOAnchorPos(card.position, enterDuration).SetEase(fadeInEase));
            if (scaleAnimation && !reducedPanelMotion)
                s.Insert(contentStart, card.transform.DOScale(card.scale, Mathf.Max(0f, scaleDuration)).SetEase(fadeInEase));
        }
        s.OnComplete(() =>
        {
            if (!hasRemoved) SetInteractable(true);
        });
    }

    public virtual void ClosePanel()
    {
        if (hasRemoved) return;
        hasRemoved = true;
        SetInteractable(false);
        transition?.Kill();
        Sequence s = transition = DOTween.Sequence().SetUpdate(true).SetTarget(this);
        float exitDuration = reducedPanelMotion ? 0 : Mathf.Max(0f, fadeOutDuration);
        s.Insert(0f, canvasGroup.DOFade(0f, exitDuration).SetEase(fadeOutEase));
        if (SlideContent && !reducedPanelMotion)
            foreach (var card in cards)
                if (card.transform is RectTransform rect)
                    s.Insert(0f, rect.DOAnchorPos(card.position + SlideOffset, exitDuration).SetEase(fadeOutEase));
        if (scaleAnimation && !reducedPanelMotion)
        {
            foreach (var card in cards)
            {
                if (card.transform == null) continue;
                s.Insert(0f, card.transform.DOScale(GetFromScale(card.scale), Mathf.Max(0f, Mathf.Min(fadeOutDuration, scaleDuration)))
                    .SetEase(fadeOutEase));
            }
        }
        s.OnComplete(() =>
        {
            if (gameObject != null) Destroy(gameObject);
        });
    }

    public virtual void ClosePanelImmediate()
    {
        hasRemoved = true;
        transition?.Kill();
        SetInteractable(false);
        if (gameObject != null) Destroy(gameObject);
    }

    private Vector3 GetFromScale(Vector3 target)
    {
        return Vector3.Scale(target, new Vector3(scaleFrom.x, scaleFrom.y, 1f));
    }

    public virtual void SetInteractable(bool interactable)
    {
        if (canvasGroup != null)
        {
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }
    }
}
