using System.Collections.Generic;
using StarFunc.Data;
using UnityEngine;

namespace StarFunc.Gameplay
{
    public class GraphRenderer : MonoBehaviour
    {
        [Header("Sub-components")]
        [SerializeField] CurveRenderer _curveRenderer;
        [SerializeField] ControlPointsRenderer _controlPointsRenderer;
        [SerializeField] ComparisonOverlay _comparisonOverlay;

        public void DrawFunction(FunctionDefinition function)
        {
            _curveRenderer.Draw(function);
        }

        public void DrawControlPoints(IReadOnlyList<StarConfig> stars)
        {
            _controlPointsRenderer.Draw(stars);
        }

        public void SetComparison(FunctionDefinition reference)
        {
            _comparisonOverlay.Show(reference);
        }

        public void ClearComparison()
        {
            _comparisonOverlay.Hide();
        }

        public void Clear()
        {
            _curveRenderer.Clear();
            _controlPointsRenderer.Clear();
            _comparisonOverlay.Hide();
        }
    }
}
