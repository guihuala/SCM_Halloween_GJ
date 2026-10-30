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
    protected bool hasRemoved = false;
    protected string panelName;
    protected CanvasGroup canvasGroup;

    [Header("动画设置")]
    [SerializeField, Min(0f)] protected float backgroundFadeDuration = 0.15f;
    [SerializeField] protected float fadeInDuration = 0.5f;
    [SerializeField] protected float fadeOutDuration = 0.5f;
    [SerializeField] protected Ease fadeInEase = Ease.OutQuad;
    [SerializeField] protected Ease fadeOutEase = Ease.InQuad;
    [SerializeField] protected bool scaleAnimation = true;
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
    }

    protected virtual void OnDestroy() => transition?.Kill();

    protected virtual void Awake()
    {
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
                scale = child.localScale
            });
        }
    }

    public virtual void OpenPanel(string name)
    {
        transition?.Kill();
        panelName = name;
        hasRemoved = false;
        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        SetInteractable(false);
        // The backdrop blocks clicks immediately, including while it is fading in.
        canvasGroup.blocksRaycasts = true;

        if (background != null)
        {
            Color color = background.color;
            color.a = 0f;
            background.color = color;
        }
        foreach (var card in cards)
        {
            if (card.transform == null) continue;
            card.group.alpha = 0f;
            card.transform.localScale = scaleAnimation ? GetFromScale(card.scale) : card.scale;
        }

        Sequence s = transition = DOTween.Sequence().SetUpdate(true).SetTarget(this);
        float contentStart = background != null ? Mathf.Max(0f, backgroundFadeDuration) : 0f;
        if (background != null)
            s.Insert(0f, background.DOFade(backgroundAlpha, contentStart).SetEase(fadeInEase));

        foreach (var card in cards)
        {
            if (card.transform == null) continue;
            s.Insert(contentStart, card.group.DOFade(card.alpha, Mathf.Max(0f, fadeInDuration)).SetEase(fadeInEase));
            if (scaleAnimation)
                s.Insert(contentStart, card.transform.DOScale(card.scale, Mathf.Max(0f, scaleDuration)).SetEase(fadeInEase));
        }
        s.OnComplete(() =>
        {
            if (!hasRemoved) SetInteractable(true);
        });
    }

    public virtual void ClosePanel()
    {
        hasRemoved = true;
        SetInteractable(false);
        transition?.Kill();
        Sequence s = transition = DOTween.Sequence().SetUpdate(true).SetTarget(this);
        s.Append(canvasGroup.DOFade(0f, Mathf.Max(0f, fadeOutDuration)).SetEase(fadeOutEase));
        if (scaleAnimation)
        {
            foreach (var card in cards)
            {
                if (card.transform == null) continue;
                s.Join(card.transform.DOScale(GetFromScale(card.scale), Mathf.Max(0f, Mathf.Min(fadeOutDuration, scaleDuration)))
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
