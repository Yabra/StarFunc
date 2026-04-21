using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StarFunc.Data;
using UnityEngine;

namespace StarFunc.Infrastructure
{
    /// <summary>
    /// Remote configuration loader (API.md §6.6, §7.3, §11).
    /// Downloads sectors, levels, and balance config from the server with ETag caching.
    /// Falls back to bundled JSON in Assets/Resources/content/ when offline or on first launch.
    /// </summary>
    public class ContentService
    {
        const string CacheDir = "content";
        const string ManifestFile = "manifest.json";
        const string SectorsFile = "sectors.json";
        const string BalanceFile = "balance.json";
        const string ShopCatalogFile = "shop_catalog.json";
        const string LevelsDir = "levels";
        const string ETagsFile = "etags.json";

        readonly ApiClient _apiClient;
        readonly NetworkMonitor _networkMonitor;
        readonly BalanceConfig _balanceConfig;

        string _cachePath;
        CachedManifest _cachedManifest;
        Dictionary<string, string> _etags = new();

        // Loaded content accessible by other services
        SectorDefinitionDto[] _sectors;
        Dictionary<string, LevelDefinitionDto[]> _levelsBySector = new();
        BalanceConfigDto _balance;
        ShopItemDto[] _shopCatalog;

        public SectorDefinitionDto[] Sectors => _sectors;
        public BalanceConfigDto Balance => _balance;
        public ShopItemDto[] ShopCatalog => _shopCatalog;

        public ContentService(ApiClient apiClient, NetworkMonitor networkMonitor,
            BalanceConfig balanceConfig)
        {
            _apiClient = apiClient;
            _networkMonitor = networkMonitor;
            _balanceConfig = balanceConfig;
            _cachePath = Path.Combine(Application.persistentDataPath, CacheDir);
        }

        /// <summary>
        /// Main initialization: check manifest, diff-update changed content, apply balance.
        /// Called once during boot (§10.5 step 6).
        /// </summary>
        public async Task InitializeAsync()
        {
            EnsureCacheDirectory();
            LoadETagsFromDisk();
            LoadCachedManifest();

            if (_networkMonitor.IsOnline)
            {
                await UpdateFromServer();
            }

            // Load content from cache (or bundled fallback)
            LoadSectors();
            LoadBalance();
            LoadShopCatalog();

            // Apply remote balance to the SO used by other services
            ApplyBalanceToConfig();

            Debug.Log($"[ContentService] Initialized — " +
                      $"{_sectors?.Length ?? 0} sectors, " +
                      $"balance v{_balance?.Version ?? 0}, " +
                      $"online={_networkMonitor.IsOnline}");
        }

        /// <summary>
        /// Get all levels for a sector, loading from cache/bundled if not yet loaded.
        /// </summary>
        public LevelDefinitionDto[] GetSectorLevels(string sectorId)
        {
            if (_levelsBySector.TryGetValue(sectorId, out var levels))
                return levels;

            levels = LoadLevelsForSector(sectorId);
            if (levels != null)
                _levelsBySector[sectorId] = levels;

            return levels;
        }

        /// <summary>
        /// Get a single level by ID.
        /// </summary>
        public LevelDefinitionDto GetLevel(string levelId)
        {
            // Determine sector from levelId convention: "sector_N_level_NN"
            string sectorId = ExtractSectorId(levelId);
            if (sectorId == null) return null;

            var levels = GetSectorLevels(sectorId);
            return levels?.FirstOrDefault(l => l.LevelId == levelId);
        }

        #region Server update

        async Task UpdateFromServer()
        {
            var manifest = await FetchManifest();
            if (manifest == null) return;

            // Compare versions and download only what changed
            bool anyUpdate = false;

            // Check sectors
            if (_cachedManifest == null ||
                manifest.ContentVersion != _cachedManifest.ContentVersion)
            {
                var changedSectors = GetChangedSectors(manifest);
                if (changedSectors.Count > 0)
                {
                    await UpdateSectors();
                    foreach (var sectorId in changedSectors)
                    {
                        await UpdateSectorLevels(sectorId);
                    }
                    anyUpdate = true;
                }
            }

            // Check balance
            if (_cachedManifest == null ||
                manifest.BalanceConfigVersion != _cachedManifest.BalanceConfigVersion)
            {
                if (await UpdateBalance())
                    anyUpdate = true;
            }

            // Check shop catalog
            if (_cachedManifest == null ||
                manifest.ShopCatalogVersion != _cachedManifest.ShopCatalogVersion)
            {
                if (await UpdateShopCatalog())
                    anyUpdate = true;
            }

            // Persist updated manifest
            if (anyUpdate || _cachedManifest == null)
            {
                SaveCachedManifest(manifest);
            }
        }

        async Task<ContentManifest> FetchManifest()
        {
            string etag = GetETag(ApiEndpoints.ContentManifest);
            var result = await _apiClient.GetConditional<ContentManifest>(
                ApiEndpoints.ContentManifest, etag);

            if (result.NotModified)
                return null;

            if (!result.IsSuccess)
            {
                Debug.LogWarning(
                    $"[ContentService] Manifest fetch failed: {result.Error?.Code}");
                return null;
            }

            SaveETag(ApiEndpoints.ContentManifest, result.ETag);
            return result.Data;
        }

        List<string> GetChangedSectors(ContentManifest manifest)
        {
            if (_cachedManifest?.SectorVersions == null || manifest.Sectors == null)
                return manifest.Sectors?.Select(s => s.SectorId).ToList() ?? new List<string>();

            var cachedVersions = _cachedManifest.SectorVersions
                .ToDictionary(s => s.SectorId, s => s.Version);

            var changed = new List<string>();
            foreach (var sector in manifest.Sectors)
            {
                if (!cachedVersions.TryGetValue(sector.SectorId, out int cachedVersion) ||
                    cachedVersion != sector.Version)
                {
                    changed.Add(sector.SectorId);
                }
            }

            return changed;
        }

        async Task UpdateSectors()
        {
            string etag = GetETag(ApiEndpoints.ContentSectors);
            var result = await _apiClient.GetConditional<SectorsResponse>(
                ApiEndpoints.ContentSectors, etag);

            if (result.NotModified) return;

            if (!result.IsSuccess || result.Data?.Sectors == null)
            {
                Debug.LogWarning(
                    $"[ContentService] Sectors fetch failed: {result.Error?.Code}");
                return;
            }

            SaveETag(ApiEndpoints.ContentSectors, result.ETag);
            string json = JsonConvert.SerializeObject(result.Data.Sectors, Formatting.Indented);
            WriteCacheFile(SectorsFile, json);
        }

        async Task UpdateSectorLevels(string sectorId)
        {
            string endpoint = $"{ApiEndpoints.ContentSectors}/{sectorId}/levels";
            string etag = GetETag(endpoint);
            var result = await _apiClient.GetConditional<LevelsResponse>(endpoint, etag);

            if (result.NotModified) return;

            if (!result.IsSuccess || result.Data?.Levels == null)
            {
                Debug.LogWarning(
                    $"[ContentService] Levels fetch for {sectorId} failed: {result.Error?.Code}");
                return;
            }

            SaveETag(endpoint, result.ETag);
            string json = JsonConvert.SerializeObject(result.Data.Levels, Formatting.Indented);
            string fileName = Path.Combine(LevelsDir, $"{sectorId}.json");
            WriteCacheFile(fileName, json);

            // Update in-memory cache
            _levelsBySector[sectorId] = result.Data.Levels;
        }

        async Task<bool> UpdateBalance()
        {
            string etag = GetETag(ApiEndpoints.ContentBalance);
            var result = await _apiClient.GetConditional<BalanceConfigDto>(
                ApiEndpoints.ContentBalance, etag);

            if (result.NotModified) return false;

            if (!result.IsSuccess || result.Data == null)
            {
                Debug.LogWarning(
                    $"[ContentService] Balance fetch failed: {result.Error?.Code}");
                return false;
            }

            SaveETag(ApiEndpoints.ContentBalance, result.ETag);
            string json = JsonConvert.SerializeObject(result.Data, Formatting.Indented);
            WriteCacheFile(BalanceFile, json);
            return true;
        }

        async Task<bool> UpdateShopCatalog()
        {
            string etag = GetETag(ApiEndpoints.ShopItems);
            var result = await _apiClient.GetConditional<ShopItemDto[]>(
                ApiEndpoints.ShopItems, etag);

            if (result.NotModified) return false;

            if (!result.IsSuccess || result.Data == null)
            {
                Debug.LogWarning(
                    $"[ContentService] Shop catalog fetch failed: {result.Error?.Code}");
                return false;
            }

            SaveETag(ApiEndpoints.ShopItems, result.ETag);
            string json = JsonConvert.SerializeObject(result.Data, Formatting.Indented);
            WriteCacheFile(ShopCatalogFile, json);
            return true;
        }

        #endregion

        #region Loading (cache → bundled fallback)

        void LoadSectors()
        {
            string json = ReadCacheFile(SectorsFile) ?? ReadBundledFile(SectorsFile);
            if (json == null)
            {
                Debug.LogError("[ContentService] No sectors data found (cache or bundled).");
                _sectors = Array.Empty<SectorDefinitionDto>();
                return;
            }

            _sectors = JsonConvert.DeserializeObject<SectorDefinitionDto[]>(json);
        }

        LevelDefinitionDto[] LoadLevelsForSector(string sectorId)
        {
            string fileName = Path.Combine(LevelsDir, $"{sectorId}.json");
            string json = ReadCacheFile(fileName) ?? ReadBundledFile(fileName);
            if (json == null)
            {
                Debug.LogWarning($"[ContentService] No levels data for {sectorId}.");
                return null;
            }

            return JsonConvert.DeserializeObject<LevelDefinitionDto[]>(json);
        }

        void LoadBalance()
        {
            string json = ReadCacheFile(BalanceFile) ?? ReadBundledFile(BalanceFile);
            if (json == null)
            {
                Debug.LogWarning("[ContentService] No balance data found — using SO defaults.");
                return;
            }

            _balance = JsonConvert.DeserializeObject<BalanceConfigDto>(json);
        }

        void LoadShopCatalog()
        {
            string json = ReadCacheFile(ShopCatalogFile) ?? ReadBundledFile(ShopCatalogFile);
            if (json == null)
            {
                Debug.LogWarning("[ContentService] No shop catalog found.");
                _shopCatalog = Array.Empty<ShopItemDto>();
                return;
            }

            _shopCatalog = JsonConvert.DeserializeObject<ShopItemDto[]>(json);
        }

        void ApplyBalanceToConfig()
        {
            if (_balance == null) return;

            _balanceConfig.MaxLives = _balance.LivesConfig.MaxLives;
            _balanceConfig.RestoreIntervalSeconds = _balance.LivesConfig.RestoreIntervalSeconds;
            _balanceConfig.RestoreCostFragments = _balance.LivesConfig.RestoreCostFragments;
            _balanceConfig.SkipLevelCostFragments = _balance.SkipLevelCostFragments;
            _balanceConfig.ImprovementBonusPerStar = _balance.ImprovementBonusPerStar;
            _balanceConfig.HintCostFragments = _balance.HintCostFragments;

            Debug.Log($"[ContentService] Balance config applied (v{_balance.Version}).");
        }

        #endregion

        #region LevelDefinition → LevelData conversion

        /// <summary>
        /// Convert a server LevelDefinition DTO into a runtime LevelData ScriptableObject.
        /// </summary>
        public static LevelData ToLevelData(LevelDefinitionDto dto)
        {
            var ld = ScriptableObject.CreateInstance<LevelData>();

            ld.LevelId = dto.LevelId;
            ld.LevelIndex = dto.LevelIndex;
            ld.Type = ParseEnum<LevelType>(dto.Type);
            ld.TaskType = ParseEnum<TaskType>(dto.TaskType);

            // Coordinate plane
            if (dto.CoordinatePlane != null)
            {
                ld.PlaneMin = ToVector2(dto.CoordinatePlane.PlaneMin);
                ld.PlaneMax = ToVector2(dto.CoordinatePlane.PlaneMax);
                ld.GridStep = dto.CoordinatePlane.GridStep;
            }

            // Stars
            if (dto.Stars != null)
            {
                ld.Stars = new StarConfig[dto.Stars.Length];
                for (int i = 0; i < dto.Stars.Length; i++)
                {
                    var s = dto.Stars[i];
                    ld.Stars[i] = new StarConfig
                    {
                        StarId = s.StarId,
                        Coordinate = ToVector2(s.Coordinate),
                        InitialState = ParseEnum<StarState>(s.InitialState),
                        IsControlPoint = s.IsControlPoint,
                        IsDistractor = s.IsDistractor,
                        BelongsToSolution = s.BelongsToSolution,
                        RevealAfterAction = s.RevealAfterAction
                    };
                }
            }

            // Reference functions
            if (dto.ReferenceFunctions != null)
            {
                ld.ReferenceFunctions = new FunctionDefinition[dto.ReferenceFunctions.Length];
                for (int i = 0; i < dto.ReferenceFunctions.Length; i++)
                {
                    var f = dto.ReferenceFunctions[i];
                    var fd = ScriptableObject.CreateInstance<FunctionDefinition>();
                    fd.Type = ParseEnum<FunctionType>(f.Type);
                    fd.Coefficients = f.Coefficients;
                    fd.DomainRange = ToVector2(f.DomainRange);
                    ld.ReferenceFunctions[i] = fd;
                }
            }

            // Answer options
            if (dto.AnswerOptions != null)
            {
                ld.AnswerOptions = new AnswerOption[dto.AnswerOptions.Length];
                for (int i = 0; i < dto.AnswerOptions.Length; i++)
                {
                    var o = dto.AnswerOptions[i];
                    ld.AnswerOptions[i] = new AnswerOption
                    {
                        OptionId = o.OptionId,
                        Text = o.Text,
                        IsCorrect = o.IsCorrect
                    };
                }
            }

            // Star rating
            if (dto.StarRating != null)
            {
                ld.StarRating = new StarRatingConfig
                {
                    ThreeStarMaxErrors = dto.StarRating.ThreeStarMaxErrors,
                    TwoStarMaxErrors = dto.StarRating.TwoStarMaxErrors,
                    OneStarMaxErrors = dto.StarRating.OneStarMaxErrors,
                    TimerAffectsRating = dto.StarRating.TimerAffectsRating,
                    ThreeStarMaxTime = dto.StarRating.ThreeStarMaxTime
                };
            }

            ld.AccuracyThreshold = dto.AccuracyThreshold;
            ld.FragmentReward = dto.FragmentReward;

            // Constraints
            if (dto.Constraints != null)
            {
                ld.MaxAttempts = dto.Constraints.MaxAttempts;
                ld.MaxAdjustments = dto.Constraints.MaxAdjustments;
            }

            // Visibility
            if (dto.Visibility != null)
            {
                ld.UseMemoryMode = dto.Visibility.UseMemoryMode;
                ld.MemoryDisplayDuration = dto.Visibility.MemoryDisplayDuration;
                if (dto.Visibility.GraphVisibility != null)
                {
                    ld.GraphVisibility = new GraphVisibilityConfig
                    {
                        PartialReveal = dto.Visibility.GraphVisibility.PartialReveal,
                        InitialVisibleSegments =
                            dto.Visibility.GraphVisibility.InitialVisibleSegments,
                        RevealPerCorrectAction =
                            dto.Visibility.GraphVisibility.RevealPerCorrectAction
                    };
                }
            }

            // Tutorial / hints
            if (dto.Tutorial != null)
            {
                ld.ShowHints = dto.Tutorial.ShowHints;
                if (dto.Tutorial.Hints != null)
                {
                    ld.Hints = new HintConfig[dto.Tutorial.Hints.Length];
                    for (int i = 0; i < dto.Tutorial.Hints.Length; i++)
                    {
                        var h = dto.Tutorial.Hints[i];
                        ld.Hints[i] = new HintConfig
                        {
                            Trigger = ParseEnum<HintTrigger>(h.Trigger),
                            HintText = h.HintText,
                            HighlightPosition = ToVector2(h.HighlightPosition),
                            TriggerAfterErrors = h.TriggerAfterErrors
                        };
                    }
                }
            }

            return ld;
        }

        #endregion

        #region File I/O helpers

        void EnsureCacheDirectory()
        {
            if (!Directory.Exists(_cachePath))
                Directory.CreateDirectory(_cachePath);

            string levelsPath = Path.Combine(_cachePath, LevelsDir);
            if (!Directory.Exists(levelsPath))
                Directory.CreateDirectory(levelsPath);
        }

        string ReadCacheFile(string relativePath)
        {
            string fullPath = Path.Combine(_cachePath, relativePath);
            if (!File.Exists(fullPath)) return null;

            try
            {
                return File.ReadAllText(fullPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ContentService] Failed to read cache {relativePath}: {e.Message}");
                return null;
            }
        }

        void WriteCacheFile(string relativePath, string content)
        {
            string fullPath = Path.Combine(_cachePath, relativePath);
            string dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            try
            {
                File.WriteAllText(fullPath, content);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[ContentService] Failed to write cache {relativePath}: {e.Message}");
            }
        }

        /// <summary>
        /// Read from Assets/Resources/content/ (bundled fallback).
        /// Resources.Load requires path without extension, relative to Resources/.
        /// </summary>
        string ReadBundledFile(string relativePath)
        {
            // Resources path: "content/sectors" (no .json extension)
            string resourcePath = Path.Combine(CacheDir, relativePath);
            // Remove .json extension for Resources.Load
            if (resourcePath.EndsWith(".json"))
                resourcePath = resourcePath.Substring(0, resourcePath.Length - 5);

            // Normalize to forward slashes
            resourcePath = resourcePath.Replace('\\', '/');

            var textAsset = Resources.Load<TextAsset>(resourcePath);
            if (textAsset == null) return null;

            string text = textAsset.text;
            Resources.UnloadAsset(textAsset);
            return text;
        }

        #endregion

        #region Manifest persistence

        void LoadCachedManifest()
        {
            string json = ReadCacheFile(ManifestFile);
            if (json == null) return;

            try
            {
                _cachedManifest = JsonConvert.DeserializeObject<CachedManifest>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ContentService] Failed to parse cached manifest: {e.Message}");
                _cachedManifest = null;
            }
        }

        void SaveCachedManifest(ContentManifest manifest)
        {
            _cachedManifest = new CachedManifest
            {
                ContentVersion = manifest.ContentVersion,
                BalanceConfigVersion = manifest.BalanceConfigVersion,
                ShopCatalogVersion = manifest.ShopCatalogVersion,
                SectorVersions = manifest.Sectors?.Select(s => new CachedSectorVersion
                {
                    SectorId = s.SectorId,
                    Version = s.Version
                }).ToArray()
            };

            string json = JsonConvert.SerializeObject(_cachedManifest, Formatting.Indented);
            WriteCacheFile(ManifestFile, json);
        }

        #endregion

        #region ETag persistence

        string GetETag(string endpoint) =>
            _etags.TryGetValue(endpoint, out string etag) ? etag : null;

        void SaveETag(string endpoint, string etag)
        {
            if (string.IsNullOrEmpty(etag)) return;
            _etags[endpoint] = etag;
            PersistETags();
        }

        void LoadETagsFromDisk()
        {
            string json = ReadCacheFile(ETagsFile);
            if (json == null) return;

            try
            {
                _etags = JsonConvert.DeserializeObject<Dictionary<string, string>>(json)
                         ?? new Dictionary<string, string>();
            }
            catch
            {
                _etags = new Dictionary<string, string>();
            }
        }

        void PersistETags()
        {
            string json = JsonConvert.SerializeObject(_etags, Formatting.Indented);
            WriteCacheFile(ETagsFile, json);
        }

        #endregion

        #region Utility

        static string ExtractSectorId(string levelId)
        {
            // "sector_1_level_05" → "sector_1"
            if (string.IsNullOrEmpty(levelId)) return null;
            int idx = levelId.IndexOf("_level_", StringComparison.Ordinal);
            return idx > 0 ? levelId.Substring(0, idx) : null;
        }

        static Vector2 ToVector2(Vec2Dto dto)
        {
            if (dto == null) return Vector2.zero;
            return new Vector2(dto.X, dto.Y);
        }

        static T ParseEnum<T>(string value) where T : struct
        {
            if (string.IsNullOrEmpty(value)) return default;
            return Enum.TryParse<T>(value, true, out var result) ? result : default;
        }

        #endregion
    }
}
