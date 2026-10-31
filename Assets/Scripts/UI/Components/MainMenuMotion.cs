using System;
using UnityEngine;
using UnityEngine.UI;

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
    [Header("Atmosphere & departure")]
    public Image background;
    [Range(0f, 1f)] public float atmosphereStrength = .7f;
    [Min(.1f)] public float exitDuration = .65f;
    private Material sourceMaterial;
    private Material atmosphere;
    private Action onExitComplete;
    private bool exiting;
    private float exitElapsed;
    public bool IsExiting => exiting;
    private float elapsed;
    private Vector2 pointer;
    private void OnEnable()
    {
        elapsed = exitElapsed = 0f; exiting = false; onExitComplete = null;
        pointer = Vector2.zero;
        if (background != null && background.material.HasProperty("_AtmosphereTime"))
        {
            sourceMaterial = background.material;
            atmosphere = new Material(sourceMaterial) { name = "Main Menu Atmosphere (Instance)" };
            background.material = atmosphere;
        }
        Evaluate(0f, pointer);
    }

    public void BeginExit(Action onComplete)
    {
        if (exiting) return;
        exiting = true; exitElapsed = 0f; onExitComplete = onComplete;
        // Disable the whole menu immediately, including keyboard submit during departure.
        Evaluate(elapsed, pointer);
    }
    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        if (exiting) exitElapsed += Time.unscaledDeltaTime;
        var target = new Vector2(Mathf.Clamp(Input.mousePosition.x / Mathf.Max(1, Screen.width) * 2f - 1f, -1, 1),
            Mathf.Clamp(Input.mousePosition.y / Mathf.Max(1, Screen.height) * 2f - 1f, -1, 1));
        pointer = Vector2.Lerp(pointer, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 4f));
        Evaluate(elapsed, pointer);
        if (exiting && exitElapsed >= (reducedMotion ? .15f : Mathf.Max(.1f, exitDuration)))
        {
            var completed = onExitComplete;
            onExitComplete = null;
            completed?.Invoke();
        }
    }
    public void Evaluate(float seconds, Vector2 pointerOffset)
    {
        float departure = exiting ? Mathf.Clamp01(exitElapsed / (reducedMotion ? .15f : Mathf.Max(.1f, exitDuration))) : 0f;
        if (atmosphere != null)
        {
            atmosphere.SetFloat("_AtmosphereTime", reducedMotion ? 0f : seconds);
            atmosphere.SetFloat("_Strength", reducedMotion ? 0f : atmosphereStrength);
            atmosphere.SetFloat("_Departure", Mathf.SmoothStep(0f, 1f, departure));
        }
        foreach (var layer in layers)
        {
            if (layer.rect == null) continue;
            float arrival = reducedMotion ? 1f : Mathf.SmoothStep(0, 1, Mathf.Clamp01((seconds - layer.delay) / .8f));
            float wave = reducedMotion ? 0f : Mathf.Sin(seconds * .85f + layer.phase);
            layer.rect.anchoredPosition = layer.origin + new Vector2(layer.drift.x * wave,
                layer.drift.y * wave + (1f - arrival) * -22f) + (reducedMotion ? Vector2.zero : pointerOffset * layer.depth);
            layer.rect.localRotation = Quaternion.Euler(0, 0, layer.angle * wave);
            // The ghost rises while the paper slips away, like dispatching a letter.
            float leave = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((departure - layer.delay * .3f) / .85f));
            if (!reducedMotion)
            {
                layer.rect.anchoredPosition += new Vector2(layer.interactive ? -85f : layer.depth * 5f,
                    layer.drift.y > 10f ? 125f : -35f) * leave;
                layer.rect.localRotation *= Quaternion.Euler(0, 0, layer.interactive ? -2f * leave : layer.angle * 3f * leave);
            }
            if (layer.group != null)
            {
                layer.group.alpha = arrival * (1f - leave);
                layer.group.interactable = layer.interactive && !exiting && arrival >= .999f;
                layer.group.blocksRaycasts = layer.interactive && !exiting && arrival >= .999f;
            }
        }
    }
    private void OnDisable()
    {
        onExitComplete = null; exiting = false;
        if (atmosphere != null)
        {
            if (background != null) background.material = sourceMaterial;
            if (Application.isPlaying) Destroy(atmosphere); else DestroyImmediate(atmosphere);
            atmosphere = null;
        }
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
