using System.Collections.Generic;
using StarFunc.Data;
using UnityEngine;

namespace StarFunc.Gameplay
{
    public class GraphRenderer : MonoBehaviour
    {
        [Header("Sub-components")]
        [SerializeField] CurveRenderer _curveRenderer;
        [Tooltip("Optional second CurveRenderer used as a 'pre-line' preview. " +
                 "Tutorial ChooseFunction levels draw the player's currently-selected " +
                 "function here on selection, while the confirmed answer goes to " +
                 "_curveRenderer. Leave null to disable previews.")]
        [SerializeField] CurveRenderer _previewCurveRenderer;
        [SerializeField] ControlPointsRenderer _controlPointsRenderer;
        [SerializeField] ComparisonOverlay _comparisonOverlay;

        [Header("Live Updates")]
        [SerializeField] FunctionParamsEvent _onFunctionChanged;

        Vector2 _domainRange = new(-5f, 5f);
        FunctionDefinition _liveFunction;

        void OnEnable()
        {
            if (_onFunctionChanged) _onFunctionChanged.AddListener(HandleFunctionChanged);
        }

        void OnDisable()
        {
            if (_onFunctionChanged) _onFunctionChanged.RemoveListener(HandleFunctionChanged);
        }

        void OnDestroy()
        {
            if (_liveFunction)
            {
                Destroy(_liveFunction);
                _liveFunction = null;
            }
        }

        public void DrawFunction(FunctionDefinition function)
        {
            if (function == null) return;
            _domainRange = function.DomainRange;
            _curveRenderer.Draw(function);
        }

        /// <summary>
        /// Draw <paramref name="function"/> on the dedicated preview curve renderer
        /// (if assigned). Used by ChooseFunction tutorials to show the
        /// player's currently-selected function as a "pre-line" alongside the
        /// confirmed answer.
        /// </summary>
        public void DrawPreviewFunction(FunctionDefinition function)
        {
            if (function == null || _previewCurveRenderer == null) return;
            _previewCurveRenderer.Draw(function);
        }

        public void ClearPreview()
        {
            if (_previewCurveRenderer != null)
                _previewCurveRenderer.Clear();
        }

        public void DrawControlPoints(IReadOnlyList<StarConfig> stars)
        {
            _controlPointsRenderer.Draw(stars);
        }

        public void SetComparison(FunctionDefinition reference)
        {
            if (reference == null) return;
            _domainRange = reference.DomainRange;
            _comparisonOverlay.Show(reference);
        }

        public void ClearComparison()
        {
            _comparisonOverlay.Hide();
        }

        /// <summary>
        /// Truncate the comparison overlay to the first <paramref name="segmentCount"/>
        /// segments. Used by <c>GraphVisibility.PartialReveal</c>.
        /// </summary>
        public void SetComparisonVisibleSegments(int segmentCount)
        {
            _comparisonOverlay.SetVisibleSegments(segmentCount);
        }

        public void Clear()
        {
            _curveRenderer.Clear();
            if (_previewCurveRenderer != null) _previewCurveRenderer.Clear();
            _controlPointsRenderer.Clear();
            _comparisonOverlay.Hide();
        }

        /// <summary>
        /// Handler for the OnFunctionChanged SO event — repaints the main curve from
        /// the player's live coefficients while the FunctionEditor is in use.
        /// </summary>
        void HandleFunctionChanged(FunctionParams p)
        {
            if (p == null || p.Coefficients == null) return;

            if (_liveFunction == null)
                _liveFunction = ScriptableObject.CreateInstance<FunctionDefinition>();

            _liveFunction.Type = p.Type;
            _liveFunction.Coefficients = p.Coefficients;
            _liveFunction.DomainRange = _domainRange;

            _curveRenderer.Draw(_liveFunction);
        }
    }
}
