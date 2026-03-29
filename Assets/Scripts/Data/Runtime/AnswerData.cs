using System;

namespace StarFunc.Data
{
    [Serializable]
    public class AnswerData
    {
        public string OptionId;
        public string DisplayText;
        public float Value;
        public bool IsCorrect;
    }
}
