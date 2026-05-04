using System;
using UnityEngine;

namespace StarFunc.Data
{
    [Serializable]
    public struct CutsceneFrame
    {
        public Sprite Background;
        public Sprite CharacterSprite;
        [TextArea] public string Text;
        public float Duration;
        public CutsceneFrameAnimation Animation;
    }
}
