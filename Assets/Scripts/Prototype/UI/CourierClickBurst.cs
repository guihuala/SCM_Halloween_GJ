using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    // A small procedural ring and four sparks. No textures, particles or raycast surface.
    public sealed class CourierClickBurst : MaskableGraphic
    {
        private float age = 1;
        private const float Duration = .38f;
        public void Play(Color tint)
        {
            color = tint; age = 0; enabled = true; SetVerticesDirty();
        }
        protected override void Awake() { base.Awake(); raycastTarget = false; }
        private void Update()
        {
            if(age >= Duration)return;
            age += Time.unscaledDeltaTime;
            SetVerticesDirty();
            if(age >= Duration)enabled = false;
        }
        protected override void OnDisable() { age = Duration; base.OnDisable(); }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if(age >= Duration)return;
            float t = Mathf.Clamp01(age / Duration);
            float ease = 1 - Mathf.Pow(1 - t, 3);
            var rect = rectTransform.rect;
            var center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * (.28f + ease * .42f);
            var tint = color; tint.a *= (1 - t) * .7f;
            const int segments = 40;
            for(int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2 / segments, b = (i + 1) * Mathf.PI * 2 / segments;
                var u = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var v = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                Quad(mesh, center + u * radius, center + v * radius,
                    center + v * (radius + 1.8f), center + u * (radius + 1.8f), tint);
            }
            for(int i = 0; i < 4; i++)
            {
                float a = (i + .5f) * Mathf.PI * .5f;
                var direction = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var side = new Vector2(-direction.y, direction.x);
                var point = center + direction * (radius + 5 + ease * 9);
                float length = 5 * (1 - t);
                Quad(mesh, point - direction * length, point + side * 1.8f,
                    point + direction * length, point - side * 1.8f, tint);
            }
        }
        private static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(a,tint,Vector2.zero);mesh.AddVert(b,tint,Vector2.zero);
            mesh.AddVert(c,tint,Vector2.zero);mesh.AddVert(d,tint,Vector2.zero);
            mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
        }
    }
}
