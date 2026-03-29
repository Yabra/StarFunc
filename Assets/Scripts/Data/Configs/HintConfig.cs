using System;
using UnityEngine;

namespace StarFunc.Data
{
    [Serializable]
    public struct HintConfig
    {
        public HintTrigger Trigger;
        public string HintText;
        public Vector2 HighlightPosition;
        public int TriggerAfterErrors;
    }
}
