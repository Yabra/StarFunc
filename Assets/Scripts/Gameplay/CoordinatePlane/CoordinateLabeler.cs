using StarFunc.Core;
using TMPro;
using UnityEngine;

namespace StarFunc.Gameplay
{
    public class CoordinateLabeler : MonoBehaviour
    {
        [Header("Label Settings")]
        [SerializeField] float _fontSize = 2.5f;
        [SerializeField] float _xLabelOffset = -0.3f;
        [SerializeField] float _yLabelOffset = 0.3f;

        readonly System.Collections.Generic.List<TextMeshPro> _pool = new();

        int _activeCount;

        public void Rebuild(Vector2 planeMin, Vector2 planeMax, float gridStep)
        {
            _activeCount = 0;

            // X-axis labels
            for (float x = planeMin.x; x <= planeMax.x + gridStep * 0.01f; x += gridStep)
            {
                int rounded = Mathf.RoundToInt(x);
                if (Mathf.Approximately(x, 0f)) continue; // skip origin for X; we'll place "0" with Y

                var label = GetOrCreateLabel();
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

                var label = GetOrCreateLabel();
                label.text = text;
                label.transform.localPosition = new Vector3(2.0f + _yLabelOffset, y + verticalOffset, 0f);
                label.alignment = Mathf.Approximately(y, 0f) ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
            }

            // Deactivate unused labels
            for (int i = _activeCount; i < _pool.Count; i++)
                _pool[i].gameObject.SetActive(false);
        }

        /// <summary>
        /// Called by PlaneCamera when zoom changes.
        /// Scales label font size proportionally so labels remain readable.
        /// </summary>
        public void UpdateForZoom(float orthoSize)
        {
            const float referenceOrthoSize = 6f;
            float scale = orthoSize / referenceOrthoSize;

            for (int i = 0; i < _activeCount; i++)
                _pool[i].fontSize = _fontSize * scale;
        }

        TextMeshPro GetOrCreateLabel()
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
                tmp.sortingOrder = 1;

                // Size the RectTransform small so TMP doesn't try to wrap
                var rect = tmp.rectTransform;
                rect.sizeDelta = new Vector2(2f, 1f);

                _pool.Add(tmp);
            }

            _activeCount++;
            return tmp;
        }
    }
}
