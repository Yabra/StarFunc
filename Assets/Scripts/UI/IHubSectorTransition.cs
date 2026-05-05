using System;
using UnityEngine;

namespace StarFunc.UI
{
    /// <summary>
    /// In-canvas zoom transition between two <see cref="UIScreen"/>s
    /// (currently HubScreen ↔ SectorScreen). Both screens share the same
    /// Canvas, so this animates them together — source scales up around
    /// <paramref name="focusNode"/> while fading out, destination starts
    /// slightly oversized + invisible and tweens to its resting state.
    /// </summary>
    public interface IHubSectorTransition
    {
        /// <summary>
        /// Hub→Sector entry. <paramref name="focusNode"/> is the tapped
        /// sector node — the source screen scales outward from its centre
        /// so the node grows to fill the screen. <paramref name="onComplete"/>
        /// fires once both screens have reached their end state; caller is
        /// responsible for then registering the screen swap with the
        /// <see cref="IUIService"/> stack (use the no-default-transition
        /// overload).
        /// </summary>
        void ZoomIn(UIScreen source, UIScreen dest, RectTransform focusNode,
            Action onComplete);

        /// <summary>
        /// Sector→Hub exit. Pass <paramref name="focusNode"/>=null to reuse
        /// the node from the most recent <see cref="ZoomIn"/> — the
        /// destination grows out from that point, mirroring the entry.
        /// </summary>
        void ZoomOut(UIScreen source, UIScreen dest, RectTransform focusNode,
            Action onComplete);
    }
}
