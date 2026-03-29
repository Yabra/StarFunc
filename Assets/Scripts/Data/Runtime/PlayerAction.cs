using System;

namespace StarFunc.Data
{
    public enum PlayerActionType
    {
        PlaceStar,
        RemoveStar,
        AdjustFunction,
        SelectAnswer
    }

    [Serializable]
    public class PlayerAction
    {
        public PlayerActionType ActionType;
        public string TargetId;
        public string PreviousState;
        public string NewState;
    }
}
