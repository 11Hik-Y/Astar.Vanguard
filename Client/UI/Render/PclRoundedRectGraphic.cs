using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Astar.Vanguard.Client.UI.Render
{
    /// <summary>
    /// Lightweight rounded rectangle graphic so PCL-style radii do not require
    /// image assets or a runtime dependency on the reference repository.
    /// </summary>
    internal sealed class PclRoundedRectGraphic : MaskableGraphic
    {
        [SerializeField]
        private float _radius = PclDesignTokens.RadiusCard;

        [SerializeField]
        private int _segmentsPerCorner = 6;

        public float Radius
        {
            get => _radius;
            set
            {
                var next = Mathf.Max(0f, value);
                if (Mathf.Approximately(_radius, next))
                {
                    return;
                }

                _radius = next;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            var rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            var radius = Mathf.Min(_radius, Mathf.Min(rect.width, rect.height) * 0.5f);
            if (radius <= 0.01f)
            {
                AddQuad(vertexHelper, rect, color);
                return;
            }

            var segments = Mathf.Clamp(_segmentsPerCorner, 2, 12);
            var points = new List<Vector2>(4 * (segments + 1));
            AddArc(points, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f, segments);
            AddArc(points, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f, segments);
            AddArc(points, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f, 270f, segments);
            AddArc(points, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 270f, 360f, segments);

            var center = rect.center;
            var centerVertex = UIVertex.simpleVert;
            centerVertex.color = color;
            centerVertex.position = center;
            centerVertex.uv0 = new Vector2(0.5f, 0.5f);
            vertexHelper.AddVert(centerVertex);

            foreach (var point in points)
            {
                var vertex = UIVertex.simpleVert;
                vertex.color = color;
                vertex.position = point;
                vertex.uv0 = new Vector2(
                    Mathf.InverseLerp(rect.xMin, rect.xMax, point.x),
                    Mathf.InverseLerp(rect.yMin, rect.yMax, point.y)
                );
                vertexHelper.AddVert(vertex);
            }

            for (var index = 0; index < points.Count; index++)
            {
                var current = index + 1;
                var next = ((index + 1) % points.Count) + 1;
                vertexHelper.AddTriangle(0, current, next);
            }
        }

        private static void AddQuad(VertexHelper vertexHelper, Rect rect, Color vertexColor)
        {
            var points = new[]
            {
                new Vector2(rect.xMin, rect.yMin),
                new Vector2(rect.xMin, rect.yMax),
                new Vector2(rect.xMax, rect.yMax),
                new Vector2(rect.xMax, rect.yMin),
            };

            for (var index = 0; index < points.Length; index++)
            {
                var vertex = UIVertex.simpleVert;
                vertex.color = vertexColor;
                vertex.position = points[index];
                vertex.uv0 = new Vector2(index > 1 ? 1f : 0f, index is 1 or 2 ? 1f : 0f);
                vertexHelper.AddVert(vertex);
            }

            vertexHelper.AddTriangle(0, 1, 2);
            vertexHelper.AddTriangle(2, 3, 0);
        }

        private static void AddArc(
            ICollection<Vector2> points,
            Vector2 center,
            float radius,
            float startDegrees,
            float endDegrees,
            int segments
        )
        {
            for (var index = 0; index <= segments; index++)
            {
                var progress = index / (float)segments;
                var radians = Mathf.Lerp(startDegrees, endDegrees, progress) * Mathf.Deg2Rad;
                points.Add(center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius);
            }
        }
    }
}
