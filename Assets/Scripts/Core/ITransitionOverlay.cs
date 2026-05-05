using System;

namespace StarFunc.Core
{
    /// <summary>
    /// Visual screen-cover transition for switching between UI screens.
    /// Implementation is a fullscreen <c>CanvasGroup</c> that fades to opaque
    /// on <see cref="TransitionIn"/> (cover) and back to transparent on
    /// <see cref="TransitionOut"/> (reveal).
    /// </summary>
    public interface ITransitionOverlay
    {
        /// <summary>Fade overlay from transparent to opaque (cover the current screen).</summary>
        void TransitionIn(Action onComplete);

        /// <summary>Fade overlay from opaque to transparent (reveal the new screen).</summary>
        void TransitionOut(Action onComplete);

        /// <summary>
        /// Fade overlay to opaque using <paramref name="duration"/> instead
        /// of the implementation's default. Lets long transitions (e.g. the
        /// camera-zoom level entry) keep the cover in sync with their own
        /// tween rather than reaching black ahead of time.
        /// </summary>
        void TransitionIn(float duration, Action onComplete);

        /// <summary>
        /// Fade overlay to transparent using <paramref name="duration"/>
        /// instead of the implementation's default.
        /// </summary>
        void TransitionOut(float duration, Action onComplete);
    }
}
