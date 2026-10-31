using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    // A continuous, softly antialiased outline that adapts to each control's bounds.
    public sealed class CourierRoundedRing : MaskableGraphic
    {
        [Min(0)] public float radius = 12f;
        [Min(0)] public float thickness = 2f;
        private const int CornerSteps = 10;
        private const int Points = (CornerSteps + 1) * 4;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect bounds = rectTransform.rect;
            float halfSize = Mathf.Min(bounds.width, bounds.height) * .5f;
            if (halfSize <= 0) return;
            float width = Mathf.Min(thickness, halfSize);
            float corner = Mathf.Clamp(radius, width, halfSize);
            const float feather = .65f;
            // Four nested contours form transparent fringes around a solid stroke.
            for (int band = 0; band < 4; band++)
            {
                float inset = band == 0 ? -feather : band == 1 ? 0 : band == 2 ? width : width + feather;
                float arc = Mathf.Max(0, corner - inset);
                Color tint = color;
                if (band == 0 || band == 3) tint.a = 0;
                for (int c = 0; c < 4; c++)
                {
                    Vector2 center = new Vector2(c == 0 || c == 3 ? bounds.xMax - corner : bounds.xMin + corner,
                        c < 2 ? bounds.yMax - corner : bounds.yMin + corner);
                    for (int i = 0; i <= CornerSteps; i++)
                    {
                        float angle = (c * 90f + i * 90f / CornerSteps) * Mathf.Deg2Rad;
                        var vertex = UIVertex.simpleVert;
                        vertex.position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * arc;
                        vertex.color = tint;
                        mesh.AddVert(vertex);
                    }
                }
            }
            for (int band = 0; band < 3; band++)
                for (int i = 0; i < Points; i++)
                {
                    int next = (i + 1) % Points;
                    int outer = band * Points, inner = outer + Points;
                    mesh.AddTriangle(outer + i, inner + i, inner + next);
                    mesh.AddTriangle(outer + i, inner + next, outer + next);
                }
        }
    }
}
