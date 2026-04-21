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
    }
}
