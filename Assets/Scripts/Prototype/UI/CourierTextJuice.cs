using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    // Animate the generated quads so wrapping and rich text stay stable during playback.
    [RequireComponent(typeof(Text))]
    public sealed class CourierTextJuice : BaseMeshEffect
    {
        [Min(1)] public float charactersPerSecond = 42f;
        [Min(.01f)] public float settleDuration = .24f;
        [Min(0)] public float rise = 8f;
        [Range(0, .5f)] public float pop = .16f;
        public bool reducedMotion;
        private Text label;
        private string content;
        private float elapsed;
        private int glyphCount;
        private bool playing;
        public bool IsPlaying { get { return playing; } }

        public void Present(string value, bool animate)
        {
            if (label == null) label = GetComponent<Text>();
            value = value ?? "";
            if (content == value && label.text == value) return;
            content = value;
            label.text = value;
            elapsed = 0;
            playing = animate && !reducedMotion && value.Length > 0;
            // Establish the actual rendered glyph count, including rich-text handling.
            label.SetVerticesDirty();
            Canvas.ForceUpdateCanvases();
        }

        public bool Complete()
        {
            if (!playing) return false;
            playing = false;
            graphic.SetVerticesDirty();
            return true;
        }

        private void Update()
        {
            if (!playing) return;
            elapsed += Time.deltaTime;
            if (reducedMotion || elapsed >= Mathf.Max(0, glyphCount - 1) / Mathf.Max(1, charactersPerSecond) + Mathf.Max(.01f, settleDuration))
                playing = false;
            graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive()) return;
            glyphCount = mesh.currentVertCount / 4;
            if (!playing) return;
            for (int i = 0; i < glyphCount; i++)
            {
                float age = elapsed - i / Mathf.Max(1, charactersPerSecond);
                float t = Mathf.Clamp01(age / Mathf.Max(.01f, settleDuration));
                if (t >= 1) continue;
                var vertex = new UIVertex();
                var center = Vector3.zero;
                for (int corner = 0; corner < 4; corner++)
                {
                    mesh.PopulateUIVertex(ref vertex, i * 4 + corner);
                    center += vertex.position * .25f;
                }
                // A quick squash/pop, a small overshoot, then an exact rest pose.
                float scale = 1 + pop * Mathf.Sin(t * Mathf.PI * 2.5f) * (1 - t);
                float offset = -rise * Mathf.Pow(1 - t, 3) + rise * .18f * Mathf.Sin(t * Mathf.PI * 2) * (1 - t);
                float alpha = Mathf.Clamp01(t * 4);
                for (int corner = 0; corner < 4; corner++)
                {
                    int index = i * 4 + corner;
                    mesh.PopulateUIVertex(ref vertex, index);
                    vertex.position = center + (vertex.position - center) * scale + Vector3.up * offset;
                    var color = vertex.color;
                    color.a = (byte)(color.a * alpha);
                    vertex.color = color;
                    mesh.SetUIVertex(vertex, index);
                }
            }
        }

        protected override void OnDisable()
        {
            playing = false;
            base.OnDisable();
        }
    }
}
