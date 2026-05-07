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

        [Tooltip("Coordinate this option represents (used by ChooseCoordinate to " +
                 "match against the current solution star). Ignored for other task types.")]
        public Vector2 Coordinate;

        [Tooltip("Function linked to this option (used by ChooseFunction task type).")]
        public FunctionDefinition Function;
    }
}
