using System.Collections.Generic;
using StarFunc.Core;
using StarFunc.Data;
using UnityEngine;

namespace StarFunc.Gameplay
{
    public class ControlPointsRenderer : MonoBehaviour
    {
        [SerializeField] float _markerRadius = 0.15f;
        [SerializeField] int _circleSegments = 24;

        readonly List<GameObject> _markers = new();

        public void Draw(IReadOnlyList<StarConfig> stars)
        {
            Clear();

            foreach (StarConfig star in stars)
            {
                if (!star.IsControlPoint) continue;
                CreateMarker(star.Coordinate);
            }
        }

        public void Clear()
        {
            foreach (GameObject marker in _markers)
            {
                if (marker != null) Destroy(marker);
            }
            _markers.Clear();
        }

        void CreateMarker(Vector2 position)
        {
            var go = new GameObject("ControlPoint");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(position.x, position.y, 0f);

            var lr = go.AddComponent<LineRenderer>();
            ConfigureCircle(lr, _markerRadius, _circleSegments);

            _markers.Add(go);
        }

        void ConfigureCircle(LineRenderer lr, float radius, int segments)
        {
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.sortingOrder = 6;
            lr.startWidth = 0.04f;
            lr.endWidth = 0.04f;
            lr.startColor = ColorTokens.POINT_PRIMARY;
            lr.endColor = ColorTokens.POINT_PRIMARY;
            lr.positionCount = segments;

            float angleStep = 360f / segments * Mathf.Deg2Rad;
            for (int i = 0; i < segments; i++)
            {
                float angle = angleStep * i;
                lr.SetPosition(i, new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0f));
            }
        }
    }
}
