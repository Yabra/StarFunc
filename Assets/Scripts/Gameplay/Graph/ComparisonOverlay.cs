using StarFunc.Core;
using StarFunc.Data;
using UnityEngine;

namespace StarFunc.Gameplay
{
    [RequireComponent(typeof(CurveRenderer))]
    public class ComparisonOverlay : MonoBehaviour
    {
        const float OverlayAlpha = 0.35f;

        CurveRenderer _curve;

        void Awake()
        {
            _curve = GetComponent<CurveRenderer>();
            Color overlay = ColorTokens.LINE_PRIMARY;
            overlay.a = OverlayAlpha;
            _curve.SetColor(overlay);
            _curve.SetWidth(0.04f);
        }

        public void Show(FunctionDefinition reference)
        {
            _curve.Draw(reference);
        }

        public void Hide()
        {
            _curve.Clear();
        }
    }
}
