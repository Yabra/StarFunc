using DG.Tweening;
using StarFunc.Core;
using StarFunc.Data;
using UnityEngine;

namespace StarFunc.Gameplay
{
    [RequireComponent(typeof(LineRenderer))]
    public class CurveRenderer : MonoBehaviour
    {
        [Header("Sample Counts")]
        [SerializeField, Range(10, 200)] int _linearSamples = 40;
        [SerializeField, Range(10, 200)] int _quadraticSamples = 80;
        [SerializeField, Range(10, 200)] int _sinusoidalSamples = 140;
        [SerializeField, Range(10, 200)] int _mixedSamples = 140;

        [Header("Visual Override")]
        [Tooltip("Vertex colour applied to start and end of the line. Multiplies " +
                 "with the texture, so a white dashed texture renders in this colour.")]
        [SerializeField] Color _lineColor = ColorTokens.LINE_PRIMARY;
        [Tooltip("Width of the line in world units.")]
        [SerializeField] float _lineWidth = 0.06f;
        [Tooltip("Optional material override (e.g. a dashed-line material). " +
                 "Leave null to keep the LineRenderer's existing material — usually " +
                 "the project's default Sprites material.")]
        [SerializeField] Material _lineMaterial;
        [Tooltip("How the LineRenderer maps its texture along the line. Use Tile " +
                 "for repeating dash patterns, Stretch for solid lines.")]
        [SerializeField] LineTextureMode _textureMode = LineTextureMode.Stretch;
        [Tooltip("Sorting order for the LineRenderer. 0 keeps it in plane with the " +
                 "default; raise to draw on top of the primary curve.")]
        [SerializeField] int _sortingOrder;

        [Tooltip("Seconds to animate the curve in from start to end on Draw(). " +
                 "0 = instant. The preview/dashed renderer typically keeps this " +
                 "at 0; the main answer curve sweeps in over ~2s.")]
        [SerializeField, Min(0f)] float _drawDuration;

        [Tooltip("Easing applied during the animated draw. OutQuad gives a " +
                 "satisfying 'fast then settle' reveal.")]
        [SerializeField] Ease _drawEase = Ease.OutQuad;

        LineRenderer _line;
        int _lastFullSampleCount;
        Tween _drawTween;
        Vector3[] _positionsBuffer;

        void Awake()
        {
            _line = GetComponent<LineRenderer>();
            ConfigureLineRenderer(_line);
        }

        public void Draw(FunctionDefinition function)
        {
            KillDrawTween();

            int samples = SamplesFor(function.Type);
            _lastFullSampleCount = samples;

            float xMin = function.DomainRange.x;
            float xMax = function.DomainRange.y;
            float step = (xMax - xMin) / (samples - 1);

            // Pre-compute every sample once; the tween just reveals the prefix
            // each frame instead of recomputing the function repeatedly.
            if (_positionsBuffer == null || _positionsBuffer.Length < samples)
                _positionsBuffer = new Vector3[samples];
            for (int i = 0; i < samples; i++)
            {
                float x = xMin + step * i;
                float y = FunctionEvaluator.Evaluate(function, x);
                _positionsBuffer[i] = new Vector3(x, y, 0f);
            }

            if (_drawDuration <= 0f)
            {
                _line.positionCount = samples;
                for (int i = 0; i < samples; i++)
                    _line.SetPosition(i, _positionsBuffer[i]);
                _line.enabled = true;
                return;
            }

            // Animated: start with a single point and grow positionCount each
            // tween tick, re-writing all visible positions so that shrinking
            // and growing the buffer never leaves stale (0,0,0) entries.
            _line.positionCount = 1;
            _line.SetPosition(0, _positionsBuffer[0]);
            _line.enabled = true;

            int totalSamples = samples;
            _drawTween = DOTween.To(() => 0f, t =>
            {
                int n = Mathf.Clamp(Mathf.CeilToInt(t * (totalSamples - 1)) + 1, 1, totalSamples);
                _line.positionCount = n;
                for (int i = 0; i < n; i++)
                    _line.SetPosition(i, _positionsBuffer[i]);
            }, 1f, _drawDuration)
                .SetEase(_drawEase)
                .SetLink(gameObject);
        }

        void KillDrawTween()
        {
            if (_drawTween != null && _drawTween.IsActive())
                _drawTween.Kill();
            _drawTween = null;
        }

        /// <summary>
        /// Truncate the curve to the first <paramref name="segmentCount"/> segments
        /// (i.e. <c>segmentCount + 1</c> position samples). Pass <c>0</c> to hide
        /// entirely, or any value &gt;= the full sample count to show the whole curve.
        /// Used by <c>GraphVisibility.PartialReveal</c>.
        /// </summary>
        public void SetVisibleSegments(int segmentCount)
        {
            if (_line == null) return;

            if (segmentCount <= 0)
            {
                _line.positionCount = 0;
                _line.enabled = false;
                return;
            }

            int maxPositions = _lastFullSampleCount > 0 ? _lastFullSampleCount : _line.positionCount;
            int positions = Mathf.Min(segmentCount + 1, maxPositions);
            _line.positionCount = positions;
            _line.enabled = positions > 1;
        }

        int SamplesFor(FunctionType type) => type switch
        {
            FunctionType.Linear => _linearSamples,
            FunctionType.Quadratic => _quadraticSamples,
            FunctionType.Sinusoidal => _sinusoidalSamples,
            FunctionType.Mixed => _mixedSamples,
            _ => _quadraticSamples
        };

        public void Clear()
        {
            KillDrawTween();
            _line.positionCount = 0;
            _line.enabled = false;
        }

        public void SetColor(Color color)
        {
            _line.startColor = color;
            _line.endColor = color;
        }

        public void SetWidth(float width)
        {
            _line.startWidth = width;
            _line.endWidth = width;
        }

        void ConfigureLineRenderer(LineRenderer lr)
        {
            lr.useWorldSpace = false;
            lr.sortingOrder = _sortingOrder;
            lr.startWidth = _lineWidth;
            lr.endWidth = _lineWidth;
            lr.startColor = _lineColor;
            lr.endColor = _lineColor;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 4;
            lr.textureMode = _textureMode;
            if (_lineMaterial != null) lr.sharedMaterial = _lineMaterial;
            lr.positionCount = 0;
            lr.enabled = false;
        }
    }
}
