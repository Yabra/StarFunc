using System;

namespace StarFunc.Data
{
    [Serializable]
    public struct GraphVisibilityConfig
    {
        public bool PartialReveal;
        public int InitialVisibleSegments;
        public int RevealPerCorrectAction;
    }
}
