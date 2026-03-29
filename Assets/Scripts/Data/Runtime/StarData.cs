using System;
using UnityEngine;

namespace StarFunc.Data
{
    [Serializable]
    public class StarData
    {
        public string StarId;
        public Vector2 Coordinate;
        public StarState State;
        public bool IsControlPoint;
    }
}
