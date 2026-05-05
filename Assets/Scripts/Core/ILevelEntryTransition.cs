using System;
using UnityEngine;

namespace StarFunc.Core
{
    /// <summary>
    /// Camera-driven "dive into the level node" transition for Hub → Level
    /// scene loads. Pairs a camera move/orthoSize tween with a fullscreen
    /// fade so the player feels the camera plunging into the tapped node;
    /// <see cref="ZoomOut"/> reverses both on the way back to the Hub.
    /// </summary>
    public interface ILevelEntryTransition
    {
        /// <summary>
        /// Tween the hub camera toward <paramref name="worldPos"/> while
        /// fading the screen to opaque. <paramref name="onComplete"/> fires
        /// once the screen is fully covered — caller should then load the
        /// level scene.
        /// </summary>
        void ZoomIn(Vector3 worldPos, Action onComplete);

        /// <summary>
        /// Reverse the camera tween while fading the screen back in.
        /// Caller should invoke this after the level scene is unloaded so
        /// the player sees the camera pulling back to its original framing.
        /// </summary>
        void ZoomOut(Action onComplete);
    }
}
