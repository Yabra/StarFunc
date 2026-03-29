using System;
using System.Collections.Generic;

namespace StarFunc.Data
{
    [Serializable]
    public class ValidationResult
    {
        public bool IsValid;
        public List<string> Errors;
        public float MatchPercentage;
    }
}
