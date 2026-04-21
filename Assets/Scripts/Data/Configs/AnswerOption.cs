using System;
using UnityEngine;

namespace StarFunc.Data
{
    [Serializable]
    public struct AnswerOption
    {
        public string OptionId;
        public string Text;
        public float Value;
        public bool IsCorrect;

        [Tooltip("Function linked to this option (used by ChooseFunction task type).")]
        public FunctionDefinition Function;
    }
}
