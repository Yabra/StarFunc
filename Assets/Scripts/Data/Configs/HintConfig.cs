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

        [Tooltip("Mask cutout / highlight marker size in canvas units. " +
                 "Leave at (0, 0) to use the popup's default _highlightSize.")]
        public Vector2 HighlightSize;

        public int TriggerAfterErrors;
    }
}
