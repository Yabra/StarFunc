using StarFunc.Data;

namespace StarFunc.Meta
{
    public interface IProgressionService
    {
        // Sector state
        SectorState GetSectorState(string sectorId);
        bool IsSectorUnlocked(string sectorId);
        bool IsSectorCompleted(string sectorId);
        void CompleteSector(string sectorId);

        // Level state
        bool IsLevelUnlocked(string levelId);
        bool IsLevelCompleted(string levelId);
        int GetBestStars(string levelId);
        void CompleteLevel(string levelId, LevelResult result);

        // Stars
        int GetTotalStars();
        int GetSectorStars(string sectorId);

        // Unlock check
        bool CanUnlockSector(string sectorId);

        // Skip
        bool CanSkipLevel(string levelId);
        void SkipLevel(string levelId);
    }
}
