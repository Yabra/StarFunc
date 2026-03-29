using System;

namespace StarFunc.Data
{
    [Serializable]
    public struct AnswerOption
    {
        public string OptionId;
        public string Text;
        public float Value;
        public bool IsCorrect;
    }
}
