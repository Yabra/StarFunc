using System;

namespace StarFunc.Data
{
    [Serializable]
    public class FunctionParams
    {
        public FunctionType Type;
        public float[] Coefficients;
    }
}
