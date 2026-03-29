using System;
using UnityEngine;

namespace StarFunc.Data
{
    [Serializable]
    public struct CutsceneFrame
    {
        public Sprite Background;
        public Sprite CharacterSprite;
        public GhostEmotion Emotion;
        [TextArea] public string Text;
        public float Duration;
        public string FrameAnimation;
    }
}
