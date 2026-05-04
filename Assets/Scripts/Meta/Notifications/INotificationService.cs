using System;

namespace StarFunc.Meta
{
    /// <summary>
    /// Tracks which one-shot notifications the player has not yet acknowledged
    /// — newly unlocked sectors, restored lives, available content, etc.
    /// Drives red-dot badges on hub UI elements.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>True if <paramref name="sectorId"/> is unlocked but not yet acknowledged.</summary>
        bool HasNewContent(string sectorId);

        /// <summary>True if there is at least one unclaimed reward awaiting the player.</summary>
        bool HasUnclaimedRewards();

        /// <summary>Mark a content id as acknowledged. Persists.</summary>
        void MarkSeen(string contentId);

        /// <summary>
        /// Number of unseen items in a context (e.g. <c>"hub"</c> = unread sectors).
        /// </summary>
        int GetBadgeCount(string context);

        /// <summary>Fires whenever the seen/unseen state changes.</summary>
        event Action OnChanged;

        /// <summary>Stable content-id helpers — keep callers off raw string keys.</summary>
        public static string SectorUnlockId(string sectorId) => $"sector_unlock:{sectorId}";

        /// <summary>Content id for the "lives just refilled" notification.</summary>
        public const string LivesRefilledId = "lives_refilled";
    }
}
