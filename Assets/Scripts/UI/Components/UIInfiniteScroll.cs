using UnityEngine;
using UnityEngine.UI;

/// <summary>Drives the postal tape material with unscaled time, only while enabled.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public sealed class UIInfiniteScroll : MonoBehaviour
{
    [SerializeField] private Material scrollMaterial;

    private static readonly int PhaseId = Shader.PropertyToID("_ScrollPhase");
    private static readonly int SpeedId = Shader.PropertyToID("_Speed");
    private Graphic graphic;
    private Material originalMaterial;
    private Material instance;
    private float phase;

    private void OnEnable()
    {
        graphic = GetComponent<Graphic>();
        originalMaterial = graphic.material;
        var source = scrollMaterial != null ? scrollMaterial : originalMaterial;
        if (source == null || !source.HasProperty(PhaseId) || !source.HasProperty(SpeedId)) return;
        instance = new Material(source) { name = source.name + " (Loading Instance)" };
        phase = 0f;
        instance.SetFloat(PhaseId, phase);
        graphic.material = instance;
    }

    private void Update()
    {
        if (instance == null) return;
        phase = Mathf.Repeat(phase + Time.unscaledDeltaTime * instance.GetFloat(SpeedId), 1f);
        instance.SetFloat(PhaseId, phase);
        // Mask creates a stencil material copy; keep its phase in sync as well.
        var renderedMaterial = graphic.materialForRendering;
        if (renderedMaterial != null && renderedMaterial != instance && renderedMaterial.HasProperty(PhaseId))
            renderedMaterial.SetFloat(PhaseId, phase);
    }

    private void OnDisable()
    {
        if (instance == null) return;
        if (graphic != null && graphic.material == instance) graphic.material = originalMaterial;
        if (Application.isPlaying) Destroy(instance);
        else DestroyImmediate(instance);
        instance = null;
    }
}
