using StarFunc.Data;
using UnityEngine;

namespace StarFunc.Meta
{
    public interface IFeedbackService
    {
        bool HapticsEnabled { get; }
        void PlayFeedback(FeedbackType type);

        /// <summary>
        /// Same as <see cref="PlayFeedback(FeedbackType)"/> but spawns the
        /// configured VFX prefab at <paramref name="worldPosition"/>. Use this
        /// from gameplay sites where the effect should track a star, the level
        /// origin, or any specific world point.
        /// </summary>
        void PlayFeedback(FeedbackType type, Vector3 worldPosition);

        void SetHapticsEnabled(bool enabled);
    }
}
