using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarFunc.Data
{
    [Serializable]
    public class PlayerAnswer
    {
        public TaskType TaskType;
        public Vector2 SelectedCoordinate;
        public string SelectedOptionId;
        public float[] FunctionCoefficients;
        public List<Vector2> PlacedPoints;
    }
}
