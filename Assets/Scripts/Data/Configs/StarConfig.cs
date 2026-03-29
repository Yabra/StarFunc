using System;
using UnityEngine;

namespace StarFunc.Data
{
    [Serializable]
    public struct StarConfig
    {
        public string StarId;
        public Vector2 Coordinate;
        public StarState InitialState;
        public bool IsControlPoint;
        public bool IsDistractor;
        public bool BelongsToSolution;
        public int RevealAfterAction;
    }
}
