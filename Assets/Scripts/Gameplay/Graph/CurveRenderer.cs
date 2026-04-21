using StarFunc.Core;
using StarFunc.Data;
using UnityEngine;

namespace StarFunc.Gameplay
{
    [RequireComponent(typeof(LineRenderer))]
    public class CurveRenderer : MonoBehaviour
    {
        [SerializeField, Range(10, 200)] int _sampleCount = 80;

        LineRenderer _line;

        void Awake()
        {
            _line = GetComponent<LineRenderer>();
            ConfigureLineRenderer(_line);
        }

        public void Draw(FunctionDefinition function)
        {
            float xMin = function.DomainRange.x;
            float xMax = function.DomainRange.y;

            _line.positionCount = _sampleCount;

            float step = (xMax - xMin) / (_sampleCount - 1);
            for (int i = 0; i < _sampleCount; i++)
            {
                float x = xMin + step * i;
                float y = FunctionEvaluator.Evaluate(function, x);
                _line.SetPosition(i, new Vector3(x, y, 0f));
            }

            _line.enabled = true;
        }

        public void Clear()
        {
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

        static void ConfigureLineRenderer(LineRenderer lr)
        {
            lr.useWorldSpace = false;
            lr.sortingOrder = 5;
            lr.startWidth = 0.06f;
            lr.endWidth = 0.06f;
            lr.startColor = ColorTokens.LINE_PRIMARY;
            lr.endColor = ColorTokens.LINE_PRIMARY;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 4;
            lr.positionCount = 0;
            lr.enabled = false;
        }
    }
}
