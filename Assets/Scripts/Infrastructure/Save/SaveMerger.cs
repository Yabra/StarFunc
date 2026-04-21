using System;
using System.Collections.Generic;
using System.Linq;
using StarFunc.Data;

namespace StarFunc.Infrastructure
{
    /// <summary>
    /// Merges two PlayerSaveData instances using the "progress always forward" strategy (API.md §8.2).
    /// </summary>
    public class SaveMerger
    {
        public PlayerSaveData Merge(PlayerSaveData local, PlayerSaveData server)
        {
            if (local == null) return server;
            if (server == null) return local;

            return new PlayerSaveData
            {
                SaveVersion = Math.Max(local.SaveVersion, server.SaveVersion),
                Version = server.Version + 1,
                LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),

                // Progression — merge toward most advanced state
                CurrentSectorIndex = Math.Max(local.CurrentSectorIndex, server.CurrentSectorIndex),
                LevelProgress = MergeLevelProgress(local.LevelProgress, server.LevelProgress),
                SectorProgress = MergeSectorProgress(local.SectorProgress, server.SectorProgress),

                // Server-authoritative (transactional resources)
                TotalFragments = server.TotalFragments,
                CurrentLives = server.CurrentLives,
                LastLifeRestoreTimestamp = server.LastLifeRestoreTimestamp,
                Consumables = new Dictionary<string, int>(server.Consumables ?? new Dictionary<string, int>()),

                // Union
                OwnedItems = UnionItems(local.OwnedItems, server.OwnedItems),

                // Statistics — take max
                TotalLevelsCompleted = Math.Max(local.TotalLevelsCompleted, server.TotalLevelsCompleted),
                TotalStarsCollected = Math.Max(local.TotalStarsCollected, server.TotalStarsCollected),
                TotalPlayTime = Math.Max(local.TotalPlayTime, server.TotalPlayTime),
            };
        }

        static Dictionary<string, LevelProgress> MergeLevelProgress(
            Dictionary<string, LevelProgress> local,
            Dictionary<string, LevelProgress> server)
        {
            var merged = new Dictionary<string, LevelProgress>();
            var allKeys = new HashSet<string>();

            if (local != null) foreach (var k in local.Keys) allKeys.Add(k);
            if (server != null) foreach (var k in server.Keys) allKeys.Add(k);

            foreach (var key in allKeys)
            {
                LevelProgress l = null, s = null;
                local?.TryGetValue(key, out l);
                server?.TryGetValue(key, out s);

                if (l == null) { merged[key] = s; continue; }
                if (s == null) { merged[key] = l; continue; }

                merged[key] = new LevelProgress
                {
                    IsCompleted = l.IsCompleted || s.IsCompleted,
                    BestStars = Math.Max(l.BestStars, s.BestStars),
                    BestTime = MergeBestTime(l.BestTime, s.BestTime),
                    Attempts = Math.Max(l.Attempts, s.Attempts),
                };
            }

            return merged;
        }

        static float MergeBestTime(float local, float server)
        {
            if (local == 0f) return server;
            if (server == 0f) return local;
            return Math.Min(local, server);
        }

        static Dictionary<string, SectorProgress> MergeSectorProgress(
            Dictionary<string, SectorProgress> local,
            Dictionary<string, SectorProgress> server)
        {
            var merged = new Dictionary<string, SectorProgress>();
            var allKeys = new HashSet<string>();

            if (local != null) foreach (var k in local.Keys) allKeys.Add(k);
            if (server != null) foreach (var k in server.Keys) allKeys.Add(k);

            foreach (var key in allKeys)
            {
                SectorProgress l = null, s = null;
                local?.TryGetValue(key, out l);
                server?.TryGetValue(key, out s);

                if (l == null) { merged[key] = s; continue; }
                if (s == null) { merged[key] = l; continue; }

                merged[key] = new SectorProgress
                {
                    State = (SectorState)Math.Max((int)l.State, (int)s.State),
                    StarsCollected = Math.Max(l.StarsCollected, s.StarsCollected),
                    ControlLevelPassed = l.ControlLevelPassed || s.ControlLevelPassed,
                };
            }

            return merged;
        }

        static List<string> UnionItems(List<string> local, List<string> server)
        {
            var set = new HashSet<string>();
            if (local != null) foreach (var item in local) set.Add(item);
            if (server != null) foreach (var item in server) set.Add(item);
            return set.ToList();
        }
    }
}
