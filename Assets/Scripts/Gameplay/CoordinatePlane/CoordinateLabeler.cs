using StarFunc.Core;
using TMPro;
using UnityEngine;

namespace StarFunc.Gameplay
{
    public class CoordinateLabeler : MonoBehaviour
    {
        [System.Serializable]
        public struct DensityBand
        {
            [Tooltip("This band wins when camera orthoSize ≤ this value.")]
            public float maxOrthoSize;
            [Tooltip("Coordinate spacing between visible labels. 1 = every " +
                     "label; 5 = only multiples of 5; etc. Origin (0) always " +
                     "stays visible regardless.")]
            public float step;
        }

        [Header("Label Settings")]
        [SerializeField] float _fontSize = 2.5f;
        [SerializeField] float _xLabelOffset = -0.3f;
        [SerializeField] float _yLabelOffset = 0.3f;

        [Tooltip("Sorting order of the world-space TMP labels. Lower values " +
                 "push the labels behind the UI canvas (default 0). Make " +
                 "this higher than the grid sprites' sortingOrder so labels " +
                 "still read above the grid lines, but lower than the UI " +
                 "Canvas so HUD elements stay on top.")]
        [SerializeField] int _sortingOrder = -1;

        [Header("Zoom-Aware Visibility")]
        [Tooltip("Ortho-size → label-density bands, sorted ascending by " +
                 "maxOrthoSize. The first band whose maxOrthoSize ≥ current " +
                 "ortho wins; only labels whose coordinate is a multiple of " +
                 "that band's step remain visible. Lets the axes thin out " +
                 "as the camera pulls back so labels don't cluster.")]
        [SerializeField] DensityBand[] _densityBands =
        {
            new() { maxOrthoSize = 4f,  step = 1f },
            new() { maxOrthoSize = 8f,  step = 2f },
            new() { maxOrthoSize = 14f, step = 5f },
            new() { maxOrthoSize = float.PositiveInfinity, step = 10f },
        };

        readonly System.Collections.Generic.List<TextMeshPro> _pool = new();
        // Coordinate value (x or y, whichever axis the label sits on) for
        // each pooled label, in the same index order. UpdateForZoom uses
        // this to decide which labels to hide for the current density band.
        readonly System.Collections.Generic.List<float> _values = new();

        int _activeCount;

        public void Rebuild(Vector2 planeMin, Vector2 planeMax, float gridStep)
        {
            _activeCount = 0;

            // X-axis labels
            for (float x = planeMin.x; x <= planeMax.x + gridStep * 0.01f; x += gridStep)
            {
                int rounded = Mathf.RoundToInt(x);
                if (Mathf.Approximately(x, 0f)) continue; // skip origin for X; we'll place "0" with Y

                var label = GetOrCreateLabel(x);
                label.text = Mathf.Approximately(x, rounded) ? rounded.ToString() : x.ToString("F1");
                label.transform.localPosition = new Vector3(x, _xLabelOffset, 0f);
                label.alignment = TextAlignmentOptions.Top;
            }

            // Y-axis labels
            for (float y = planeMin.y; y <= planeMax.y + gridStep * 0.01f; y += gridStep)
            {
                int rounded = Mathf.RoundToInt(y);
                string text = Mathf.Approximately(y, rounded) ? rounded.ToString() : y.ToString("F1");

                float verticalOffset = Mathf.Approximately(y, 0f) ? _xLabelOffset : 0;

                var label = GetOrCreateLabel(y);
                label.text = text;
                label.transform.localPosition = new Vector3(2.0f + _yLabelOffset, y + verticalOffset, 0f);
                label.alignment = Mathf.Approximately(y, 0f) ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
            }

            // Deactivate unused labels
            for (int i = _activeCount; i < _pool.Count; i++)
                _pool[i].gameObject.SetActive(false);
        }

        /// <summary>
        /// Called by PlaneCamera when zoom changes. Scales font size for
        /// readability and hides labels that fall outside the current
        /// density band so the axes thin out when zoomed far out.
        /// </summary>
        public void UpdateForZoom(float orthoSize)
        {
            const float referenceOrthoSize = 6f;
            float scale = orthoSize / referenceOrthoSize;
            float step = ResolveStep(orthoSize);

            for (int i = 0; i < _activeCount; i++)
            {
                var label = _pool[i];
                bool visible = step <= 0f || IsAtStep(_values[i], step);
                if (label.gameObject.activeSelf != visible)
                    label.gameObject.SetActive(visible);
                if (visible)
                    label.fontSize = _fontSize * scale;
            }
        }

        float ResolveStep(float orthoSize)
        {
            if (_densityBands == null || _densityBands.Length == 0) return 1f;

            for (int i = 0; i < _densityBands.Length; i++)
                if (orthoSize <= _densityBands[i].maxOrthoSize)
                    return Mathf.Max(0f, _densityBands[i].step);

            return Mathf.Max(0f, _densityBands[_densityBands.Length - 1].step);
        }

        static bool IsAtStep(float value, float step)
        {
            if (step <= 0f) return true;
            float div = value / step;
            int rounded = Mathf.RoundToInt(div);
            return Mathf.Abs(div - rounded) < 1e-3f;
        }

        TextMeshPro GetOrCreateLabel(float value)
        {
            TextMeshPro tmp;

            if (_activeCount < _pool.Count)
            {
                tmp = _pool[_activeCount];
                tmp.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject($"Label_{_activeCount}");
                go.transform.SetParent(transform, false);
                tmp = go.AddComponent<TextMeshPro>();
                tmp.fontSize = _fontSize;
                tmp.color = ColorTokens.UI_NEUTRAL;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.overflowMode = TextOverflowModes.Overflow;

                // Size the RectTransform small so TMP doesn't try to wrap
                var rect = tmp.rectTransform;
                rect.sizeDelta = new Vector2(2f, 1f);

                _pool.Add(tmp);
            }

            // Apply the configured sortingOrder every time — so changes via
            // the Inspector at runtime propagate to the entire pool, not
            // just newly-created labels.
            tmp.sortingOrder = _sortingOrder;

            // Maintain the parallel value list in lockstep with _pool so
            // UpdateForZoom can decide visibility per label.
            if (_activeCount < _values.Count) _values[_activeCount] = value;
            else _values.Add(value);

            _activeCount++;
            return tmp;
        }
    }
}
