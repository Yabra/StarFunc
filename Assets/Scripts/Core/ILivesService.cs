namespace StarFunc.Core
{
    /// <summary>
    /// Service contract for the lives system.
    /// Implemented by LocalLivesService (Phase 1) and HybridLivesService (Phase 2).
    /// Lives are deducted inside the answer-check flow (LevelController / POST /check/level);
    /// client receives updated count from the response (or local validation when offline).
    /// DeductLife() is intentionally absent — it is an internal method on the local implementation.
    /// </summary>
    public interface ILivesService
    {
        int GetCurrentLives();
        int GetMaxLives();
        bool HasLives();
        bool RestoreLife();
        bool RestoreAllLives();
        float GetTimeUntilNextRestore();

        /// <summary>
        /// Deduct one life. Called by LevelController on every incorrect answer
        /// in local-only mode. In an online-authoritative flow the server's
        /// answer-check response would be the source of truth and this becomes
        /// a local fallback / optimistic update.
        /// </summary>
        void DeductLife();

        /// <summary>
        /// Grant <paramref name="quantity"/> lives without spending fragments.
        /// Used by the shop after a Lives-category purchase has already been
        /// debited via IEconomyService — RestoreLife/RestoreAllLives can't be
        /// reused there because they spend a second time.
        /// </summary>
        void GrantFreeLives(int quantity);
    }
}
