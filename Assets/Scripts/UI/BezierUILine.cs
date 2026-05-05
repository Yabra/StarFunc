using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    /// <summary>
    /// Lightweight quadratic-Bezier polyline drawn into a UI canvas. Used by
    /// <c>SectorScreen</c> to render organic connections between level nodes
    /// (replacing the static per-node connection Image). The curve is built
    /// from <c>start</c> → midpoint-with-perpendicular-offset → <c>end</c>,
    /// then extruded into a thick strip and pushed as a single mesh through
    /// <see cref="MaskableGraphic"/>'s draw path — no prefab, no LineRenderer
    /// world-space dance.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class BezierUILine : MaskableGraphic
    {
        [SerializeField] Vector2 _start;
        [SerializeField] Vector2 _end;
        [SerializeField] float _thickness = 8f;
        [Tooltip("Perpendicular offset of the Bezier control point from the " +
                 "straight-line midpoint. Sign flips the bend direction.")]
        [SerializeField] float _curveOffset = 80f;
        [SerializeField, Range(2, 64)] int _segments = 24;

        public float Thickness
        {
            get => _thickness;
            set { _thickness = value; SetVerticesDirty(); }
        }

        public float CurveOffset
        {
            get => _curveOffset;
            set { _curveOffset = value; SetVerticesDirty(); }
        }

        public void SetEndpoints(Vector2 start, Vector2 end)
        {
            _start = start;
            _end = end;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Vector2 dir = _end - _start;
            float len = dir.magnitude;
            if (len < 0.0001f) return;

            // Quadratic Bezier control point: midpoint nudged perpendicular
            // to the start→end line. Caller can flip _curveOffset's sign per
            // segment to alternate the bend direction (zigzag uses ±).
            Vector2 mid = (_start + _end) * 0.5f;
            Vector2 perp = new Vector2(-dir.y, dir.x).normalized;
            Vector2 ctrl = mid + perp * _curveOffset;

            int n = _segments;
            var pts = new Vector2[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                float omt = 1f - t;
                pts[i] = (omt * omt) * _start
                       + (2f * omt * t) * ctrl
                       + (t * t) * _end;
            }

            float halfThick = _thickness * 0.5f;
            var vert = UIVertex.simpleVert;
            vert.color = color;

            // Extrude every segment into a quad. Each pair of consecutive
            // points produces 4 verts + 2 tris. Tiny gaps may appear at sharp
            // joins; for the level-path's gentle curves this is invisible.
            for (int i = 0; i < n; i++)
            {
                Vector2 a = pts[i];
                Vector2 b = pts[i + 1];
                Vector2 d = b - a;
                if (d.sqrMagnitude < 1e-8f) continue;
                Vector2 nrm = new Vector2(-d.y, d.x).normalized * halfThick;

                int idx = vh.currentVertCount;
                vert.position = a + nrm; vh.AddVert(vert);
                vert.position = a - nrm; vh.AddVert(vert);
                vert.position = b - nrm; vh.AddVert(vert);
                vert.position = b + nrm; vh.AddVert(vert);
                vh.AddTriangle(idx + 0, idx + 2, idx + 1);
                vh.AddTriangle(idx + 0, idx + 3, idx + 2);
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetVerticesDirty();
        }
    }
}
