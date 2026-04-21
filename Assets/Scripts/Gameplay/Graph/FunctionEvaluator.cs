using System;
using StarFunc.Data;

namespace StarFunc.Gameplay
{
    public static class FunctionEvaluator
    {
        public static float Evaluate(FunctionDefinition function, float x)
        {
            return function.Type switch
            {
                FunctionType.Linear => EvaluateLinear(function.Coefficients, x),
                FunctionType.Quadratic => throw new NotImplementedException("Quadratic evaluation is planned for Phase 3."),
                FunctionType.Sinusoidal => throw new NotImplementedException("Sinusoidal evaluation is planned for Phase 3."),
                FunctionType.Mixed => throw new NotImplementedException("Mixed evaluation is planned for Phase 3."),
                _ => throw new ArgumentOutOfRangeException(nameof(function), $"Unknown FunctionType: {function.Type}")
            };
        }

        // y = a*x + b
        // Coefficients[0] = a (slope), Coefficients[1] = b (intercept)
        static float EvaluateLinear(float[] coefficients, float x)
        {
            float a = coefficients.Length > 0 ? coefficients[0] : 0f;
            float b = coefficients.Length > 1 ? coefficients[1] : 0f;
            return a * x + b;
        }
    }
}
