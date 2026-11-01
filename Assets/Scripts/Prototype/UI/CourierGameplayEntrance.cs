using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    // Scene-local cloud reveal. Resources keeps the runtime shader in player builds.
    public sealed class CourierGameplayEntrance : MaskableGraphic
    {
        private float elapsed;
        private float duration;
        private bool reducedMotion;
        private bool started;
        private Material fogMaterial;
        public bool IsRevealing => isActiveAndEnabled;

        public static CourierGameplayEntrance Play(Transform parent, float seconds, bool reduced)
        {
            if (seconds <= 0) return null;
            var go = new GameObject("GameplayEntrance", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasRenderer), typeof(GraphicRaycaster), typeof(CourierGameplayEntrance));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Above game panels, below the existing loading screen (999).
            canvas.sortingOrder = 998;
            var veil = go.GetComponent<CourierGameplayEntrance>();
            veil.duration = reduced ? Mathf.Min(seconds, .18f) : seconds;
            veil.reducedMotion = reduced;
            veil.color = new Color(.035f, .055f, .075f, 1);
            var shader = Resources.Load<Shader>("NightVoyage/GameplayEntranceFog");
            if (shader != null && shader.isSupported)
            {
                veil.fogMaterial = new Material(shader);
                veil.material = veil.fogMaterial;
                veil.fogMaterial.SetFloat("_ReducedMotion", reduced ? 1 : 0);
            }
            veil.raycastTarget = true;
            return veil;
        }

        private void Update()
        {
            // Otherwise the entire entrance would play behind the loading artwork.
            if (SceneLoader.Instance != null && SceneLoader.Instance.IsLoading()) return;
            // The first frame's delta includes scene setup before this veil existed.
            if (!started) { started = true; return; }
            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= duration)
            {
                raycastTarget = false;
                enabled = false;
                Destroy(gameObject);
                return;
            }
            if (fogMaterial != null)
            {
                fogMaterial.SetFloat("_Progress", Mathf.Clamp01(elapsed / duration));
                fogMaterial.SetFloat("_FogTime", elapsed);
            }
            else SetVerticesDirty();
        }

        protected override void OnDestroy()
        {
            if (fogMaterial != null) Destroy(fogMaterial);
            base.OnDestroy();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = rectTransform.rect;
            if (fogMaterial != null)
            {
                mesh.AddVert(new Vector3(rect.xMin, rect.yMin), Color.white, Vector2.zero);
                mesh.AddVert(new Vector3(rect.xMin, rect.yMax), Color.white, Vector2.up);
                mesh.AddVert(new Vector3(rect.xMax, rect.yMax), Color.white, Vector2.one);
                mesh.AddVert(new Vector3(rect.xMax, rect.yMin), Color.white, Vector2.right);
                mesh.AddTriangle(0, 1, 2);
                mesh.AddTriangle(0, 2, 3);
                return;
            }
            // Simple fade remains available on devices without shader support.
            float progress = duration > 0 ? Mathf.Clamp01(elapsed / duration) : 0;
            const int columns = 24, rows = 16;
            for (int y = 0; y <= rows; y++)
            for (int x = 0; x <= columns; x++)
            {
                float u = (float)x / columns, v = (float)y / rows;
                float edge = Mathf.Clamp01(new Vector2((u - .5f) * 2, (v - .5f) * 2).magnitude);
                float local = reducedMotion ? progress : Mathf.Clamp01((progress - edge * .24f) / .76f);
                var tint = color;
                tint.a *= 1 - Mathf.SmoothStep(0, 1, local);
                mesh.AddVert(new Vector3(Mathf.Lerp(rect.xMin, rect.xMax, u),
                    Mathf.Lerp(rect.yMin, rect.yMax, v)), tint, Vector2.zero);
                if (x == columns || y == rows) continue;
                int i = y * (columns + 1) + x;
                mesh.AddTriangle(i, i + columns + 1, i + 1);
                mesh.AddTriangle(i + 1, i + columns + 1, i + columns + 2);
            }
        }
    }
}
