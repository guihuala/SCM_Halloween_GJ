using System;
using UnityEngine;

/// <summary>Gentle paper movement, pointer depth and staggered arrival for authored menu layers.</summary>
public sealed class MainMenuMotion : MonoBehaviour
{
    [Serializable]
    public class Layer
    {
        public RectTransform rect;
        public CanvasGroup group;
        public Vector2 origin;
        public Vector2 drift;
        public float angle;
        public float phase;
        public float depth;
        public float delay;
        public bool interactive;
    }
    public Layer[] layers = new Layer[0];
    public bool reducedMotion;
    private float elapsed;
    private Vector2 pointer;
    private void OnEnable() { elapsed = 0f; pointer = Vector2.zero; Evaluate(0f, pointer); }
    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        var target = new Vector2(Mathf.Clamp(Input.mousePosition.x / Mathf.Max(1, Screen.width) * 2f - 1f, -1, 1),
            Mathf.Clamp(Input.mousePosition.y / Mathf.Max(1, Screen.height) * 2f - 1f, -1, 1));
        pointer = Vector2.Lerp(pointer, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 4f));
        Evaluate(elapsed, pointer);
    }
    public void Evaluate(float seconds, Vector2 pointerOffset)
    {
        foreach (var layer in layers)
        {
            if (layer.rect == null) continue;
            float arrival = reducedMotion ? 1f : Mathf.SmoothStep(0, 1, Mathf.Clamp01((seconds - layer.delay) / .8f));
            float wave = reducedMotion ? 0f : Mathf.Sin(seconds * .85f + layer.phase);
            layer.rect.anchoredPosition = layer.origin + new Vector2(layer.drift.x * wave,
                layer.drift.y * wave + (1f - arrival) * -22f) + (reducedMotion ? Vector2.zero : pointerOffset * layer.depth);
            layer.rect.localRotation = Quaternion.Euler(0, 0, layer.angle * wave);
            if (layer.group != null)
            {
                layer.group.alpha = arrival;
                layer.group.interactable = layer.interactive && arrival >= .999f;
                layer.group.blocksRaycasts = layer.interactive && arrival >= .999f;
            }
        }
    }
    private void OnDisable()
    {
        foreach (var layer in layers)
        {
            if (layer.rect == null) continue;
            layer.rect.anchoredPosition = layer.origin;
            layer.rect.localRotation = Quaternion.identity;
            if (layer.group != null)
            {
                layer.group.alpha = 1f;
                layer.group.interactable = layer.group.blocksRaycasts = layer.interactive;
            }
        }
    }
}
