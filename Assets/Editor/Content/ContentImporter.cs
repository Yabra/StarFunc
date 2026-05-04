using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using StarFunc.Data;
using UnityEditor;
using UnityEngine;

namespace StarFunc.Editor.Content
{
    /// <summary>
    /// One-shot importer that reads the bundled JSON in
    /// <c>Assets/Resources/content/</c> and populates the matching
    /// <see cref="LevelData"/>, <see cref="SectorData"/>, and
    /// <see cref="FunctionDefinition"/> assets under
    /// <c>Assets/ScriptableObjects/</c>.
    ///
    /// Existing assets are reused (matched by file path / asset name);
    /// missing FunctionDefinition assets are created fresh next to the
    /// existing ones. Run from <c>StarFunc → Import Content from JSON</c>.
    /// </summary>
    public static class ContentImporter
    {
        const string ResourcesContent = "Assets/Resources/content";
        const string LevelsRoot = "Assets/ScriptableObjects/Levels";
        const string SectorsRoot = "Assets/ScriptableObjects/Sectors";
        const string FunctionsRoot = "Assets/ScriptableObjects/Functions";
        const string CutscenesRoot = "Assets/ScriptableObjects/Cutscenes";

        // Per-sector accent palette — the JSON has no color hint, so we pick
        // distinct hues here. Designers can tweak in the inspector after import.
        static readonly Color[] AccentByIndex =
        {
            new(0.95f, 0.55f, 0.30f, 1f), // sector_1 — orange
            new(0.30f, 0.60f, 0.90f, 1f), // sector_2 — blue
            new(0.85f, 0.40f, 0.60f, 1f), // sector_3 — pink
            new(0.30f, 0.85f, 0.85f, 1f), // sector_4 — cyan
            new(0.65f, 0.40f, 0.85f, 1f), // sector_5 — purple
        };

        [MenuItem("StarFunc/Import Content from JSON")]
        public static void ImportAll()
        {
            try
            {
                AssetDatabase.StartAssetEditing();

                var sectors = LoadSectorDefs();
                var levelsBySector = LoadLevelDefs();

                int populatedLevels = 0;
                int createdFunctions = 0;

                for (int s = 1; s <= 5; s++)
                {
                    if (!levelsBySector.TryGetValue(s, out var defs)) continue;
                    foreach (var def in defs)
                    {
                        if (TryPopulateLevel(def, s, ref createdFunctions))
                            populatedLevels++;
                    }
                }

                int updatedSectors = PopulateSectors(sectors);

                Debug.Log($"[ContentImporter] Populated {populatedLevels} levels, " +
                          $"created {createdFunctions} FunctionDefinition assets, " +
                          $"updated {updatedSectors} sectors.");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        // =========================================================================
        // Levels
        // =========================================================================

        static bool TryPopulateLevel(LevelDef def, int sectorNumber, ref int createdFunctions)
        {
            string assetPath = FindLevelAssetPath(def, sectorNumber);
            if (assetPath == null)
            {
                Debug.LogWarning($"[ContentImporter] No LevelData asset found for {def.levelId} " +
                                 $"in {LevelsRoot}/Sector_{sectorNumber}/");
                return false;
            }

            var data = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
            if (data == null)
            {
                Debug.LogWarning($"[ContentImporter] Failed to load LevelData at {assetPath}");
                return false;
            }

            data.LevelId = def.levelId;
            data.LevelIndex = def.levelIndex;
            data.Type = ParseEnum(def.type, LevelType.Normal);
            data.TaskType = ParseEnum(def.taskType, TaskType.ChooseCoordinate);

            if (def.coordinatePlane != null)
            {
                data.PlaneMin = ToVec(def.coordinatePlane.planeMin);
                data.PlaneMax = ToVec(def.coordinatePlane.planeMax);
                data.GridStep = def.coordinatePlane.gridStep > 0 ? def.coordinatePlane.gridStep : 1f;
            }

            data.Stars = (def.stars ?? new List<StarDef>())
                .Select(ToStarConfig)
                .ToArray();

            data.AnswerOptions = (def.answerOptions ?? new List<AnswerOptionDef>())
                .Select(ToAnswerOption)
                .ToArray();

            data.AccuracyThreshold = def.accuracyThreshold;
            data.StarRating = ToStarRating(def.starRating);
            data.FragmentReward = def.fragmentReward;

            ApplyTutorial(data, def.tutorial);

            var resolvedFns = new List<FunctionDefinition>();
            foreach (var f in def.referenceFunctions ?? new List<FunctionDef>())
            {
                var fn = ResolveOrCreateFunction(f, sectorNumber, def.levelId, ref createdFunctions);
                if (fn != null) resolvedFns.Add(fn);
            }
            data.ReferenceFunctions = resolvedFns.ToArray();

            EditorUtility.SetDirty(data);
            return true;
        }

        static string FindLevelAssetPath(LevelDef def, int sectorNumber)
        {
            // Existing assets follow the pattern S{n}_L{idx:D2}_{Type}.asset.
            string name = $"S{sectorNumber}_L{def.levelIndex:D2}_{def.type}.asset";
            string fullPath = $"{LevelsRoot}/Sector_{sectorNumber}/{name}";
            if (AssetDatabase.LoadAssetAtPath<LevelData>(fullPath) != null) return fullPath;

            // Fall back to GUID search by LevelId in case the file was renamed.
            var guids = AssetDatabase.FindAssets($"t:LevelData", new[] { $"{LevelsRoot}/Sector_{sectorNumber}" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (asset != null && asset.LevelId == def.levelId) return path;
            }

            return null;
        }

        // =========================================================================
        // FunctionDefinitions
        // =========================================================================

        static FunctionDefinition ResolveOrCreateFunction(
            FunctionDef def, int sectorNumber, string levelId, ref int createdCount)
        {
            if (def == null) return null;

            string name = SanitizeAssetName(def.functionId);
            string assetPath = $"{FunctionsRoot}/{name}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<FunctionDefinition>(assetPath);
            if (existing != null)
            {
                ApplyFunctionFields(existing, def);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var fn = ScriptableObject.CreateInstance<FunctionDefinition>();
            ApplyFunctionFields(fn, def);
            EnsureFolder(FunctionsRoot);
            AssetDatabase.CreateAsset(fn, assetPath);
            createdCount++;
            return fn;
        }

        static void ApplyFunctionFields(FunctionDefinition fn, FunctionDef def)
        {
            fn.Type = ParseEnum(def.type, FunctionType.Linear);
            fn.Coefficients = (def.coefficients ?? new List<float>()).ToArray();
            fn.DomainRange = ToVec(def.domainRange);
        }

        // =========================================================================
        // Sectors
        // =========================================================================

        static int PopulateSectors(List<SectorDef> defs)
        {
            int updated = 0;
            var byId = new Dictionary<string, SectorData>();

            // First pass: load assets, populate scalar fields.
            for (int s = 1; s <= defs.Count && s <= 5; s++)
            {
                var def = defs[s - 1];
                string assetPath = $"{SectorsRoot}/Sector {s}.asset";
                var data = AssetDatabase.LoadAssetAtPath<SectorData>(assetPath);
                if (data == null)
                {
                    Debug.LogWarning($"[ContentImporter] Sector asset missing: {assetPath}");
                    continue;
                }

                data.SectorId = def.sectorId;
                data.DisplayName = def.displayName;
                data.SectorIndex = def.sectorIndex;
                data.RequiredStarsToUnlock = def.requiredStarsToUnlock;

                int idx = Mathf.Clamp(def.sectorIndex, 0, AccentByIndex.Length - 1);
                if (data.AccentColor.a == 0f)
                    data.AccentColor = AccentByIndex[idx];
                if (data.StarColor.a == 0f)
                    data.StarColor = Color.white;

                // Resolve cutscene refs by id from Assets/ScriptableObjects/Cutscenes/.
                // Designers create one CutsceneData asset per id; the importer
                // wires it onto the sector. Missing assets are left null and
                // logged so the warning surfaces during import.
                if (!string.IsNullOrEmpty(def.introCutsceneId))
                    data.IntroCutscene = ResolveCutscene(def.introCutsceneId);
                if (!string.IsNullOrEmpty(def.outroCutsceneId))
                    data.OutroCutscene = ResolveCutscene(def.outroCutsceneId);

                byId[def.sectorId] = data;
                EditorUtility.SetDirty(data);
                updated++;
            }

            // Second pass: wire previous-sector references now that everything is loaded.
            for (int s = 1; s <= defs.Count && s <= 5; s++)
            {
                var def = defs[s - 1];
                if (!byId.TryGetValue(def.sectorId, out var data)) continue;
                data.PreviousSector = !string.IsNullOrEmpty(def.previousSectorId)
                                      && byId.TryGetValue(def.previousSectorId, out var prev)
                    ? prev
                    : null;
                EditorUtility.SetDirty(data);
            }

            return updated;
        }

        static CutsceneData ResolveCutscene(string cutsceneId)
        {
            // Match by either CutsceneId field or asset filename so designers
            // can choose either convention.
            var guids = AssetDatabase.FindAssets($"t:CutsceneData", new[] { CutscenesRoot });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<CutsceneData>(path);
                if (asset == null) continue;
                if (asset.CutsceneId == cutsceneId) return asset;
                if (Path.GetFileNameWithoutExtension(path) == cutsceneId) return asset;
            }

            Debug.LogWarning($"[ContentImporter] CutsceneData '{cutsceneId}' not found under {CutscenesRoot}/.");
            return null;
        }

        // =========================================================================
        // JSON loading
        // =========================================================================

        static List<SectorDef> LoadSectorDefs()
        {
            string path = $"{ResourcesContent}/sectors.json";
            if (!File.Exists(path))
            {
                Debug.LogError($"[ContentImporter] {path} not found.");
                return new List<SectorDef>();
            }
            return JsonConvert.DeserializeObject<List<SectorDef>>(File.ReadAllText(path));
        }

        static Dictionary<int, List<LevelDef>> LoadLevelDefs()
        {
            var result = new Dictionary<int, List<LevelDef>>();
            for (int s = 1; s <= 5; s++)
            {
                string path = $"{ResourcesContent}/levels/sector_{s}.json";
                if (!File.Exists(path)) continue;
                var defs = JsonConvert.DeserializeObject<List<LevelDef>>(File.ReadAllText(path));
                if (defs != null) result[s] = defs;
            }
            return result;
        }

        // =========================================================================
        // Conversion helpers
        // =========================================================================

        static StarConfig ToStarConfig(StarDef d) => new()
        {
            StarId = d.starId,
            Coordinate = ToVec(d.coordinate),
            InitialState = ParseEnum(d.initialState, StarState.Active),
            IsControlPoint = d.isControlPoint,
            IsDistractor = d.isDistractor,
            BelongsToSolution = d.belongsToSolution,
            RevealAfterAction = d.revealAfterAction,
        };

        static AnswerOption ToAnswerOption(AnswerOptionDef d)
        {
            // JSON stores `value` as a string (sometimes "x,y" for coords);
            // AnswerOption.Value is float. Parse what we can, leave 0 otherwise.
            float value = 0f;
            if (!string.IsNullOrEmpty(d.value))
                float.TryParse(d.value, System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture, out value);

            return new AnswerOption
            {
                OptionId = d.optionId,
                Text = d.text,
                Value = value,
                IsCorrect = d.isCorrect,
            };
        }

        static void ApplyTutorial(LevelData data, TutorialDef def)
        {
            if (def == null)
            {
                data.ShowHints = false;
                data.Hints = Array.Empty<HintConfig>();
                return;
            }

            data.ShowHints = def.showHints;
            data.Hints = (def.hints ?? new List<HintDef>())
                .Select(ToHintConfig)
                .ToArray();
        }

        static HintConfig ToHintConfig(HintDef d) => new()
        {
            Trigger = ParseEnum(d.trigger, HintTrigger.OnLevelStart),
            HintText = d.hintText ?? string.Empty,
            HighlightPosition = ToVec(d.highlightPosition),
            TriggerAfterErrors = d.triggerAfterErrors,
        };

        static StarRatingConfig ToStarRating(StarRatingDef d)
        {
            if (d == null) return default;
            return new StarRatingConfig
            {
                ThreeStarMaxErrors = d.threeStarMaxErrors,
                TwoStarMaxErrors = d.twoStarMaxErrors,
                OneStarMaxErrors = d.oneStarMaxErrors,
                TimerAffectsRating = d.timerAffectsRating,
                ThreeStarMaxTime = d.threeStarMaxTime,
            };
        }

        static Vector2 ToVec(Vec2Def d) => d == null ? Vector2.zero : new Vector2(d.x, d.y);

        static T ParseEnum<T>(string value, T fallback) where T : struct
        {
            if (string.IsNullOrEmpty(value)) return fallback;
            return Enum.TryParse<T>(value, ignoreCase: true, out var parsed) ? parsed : fallback;
        }

        static string SanitizeAssetName(string s)
        {
            if (string.IsNullOrEmpty(s)) return "Function";
            var invalid = Path.GetInvalidFileNameChars();
            return new string(s.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        // =========================================================================
        // JSON DTOs (mirror Backend/seed/data/* schemas)
        // =========================================================================

        [Serializable] class SectorDef
        {
            public string sectorId;
            public string displayName;
            public int sectorIndex;
            public List<string> levelIds;
            public string previousSectorId;
            public int requiredStarsToUnlock;
            public string introCutsceneId;
            public string outroCutsceneId;
        }

        [Serializable] class LevelDef
        {
            public string levelId;
            public int levelIndex;
            public string sectorId;
            public string type;
            public string taskType;
            public CoordinatePlaneDef coordinatePlane;
            public List<StarDef> stars;
            public List<FunctionDef> referenceFunctions;
            public List<AnswerOptionDef> answerOptions;
            public float accuracyThreshold;
            public StarRatingDef starRating;
            public int fragmentReward;
            public TutorialDef tutorial;
        }

        [Serializable] class TutorialDef
        {
            public bool showHints;
            public List<HintDef> hints;
        }

        [Serializable] class HintDef
        {
            public string trigger;
            public string hintText;
            public Vec2Def highlightPosition;
            public int triggerAfterErrors;
        }

        [Serializable] class CoordinatePlaneDef
        {
            public Vec2Def planeMin;
            public Vec2Def planeMax;
            public float gridStep;
        }

        [Serializable] class Vec2Def { public float x, y; }

        [Serializable] class StarDef
        {
            public string starId;
            public Vec2Def coordinate;
            public string initialState;
            public bool isControlPoint;
            public bool isDistractor;
            public bool belongsToSolution;
            public int revealAfterAction;
        }

        [Serializable] class FunctionDef
        {
            public string functionId;
            public string type;
            public List<float> coefficients;
            public Vec2Def domainRange;
        }

        [Serializable] class AnswerOptionDef
        {
            public string optionId;
            public string text;
            public string value;
            public bool isCorrect;
        }

        [Serializable] class StarRatingDef
        {
            public int threeStarMaxErrors;
            public int twoStarMaxErrors;
            public int oneStarMaxErrors;
            public bool timerAffectsRating;
            public float threeStarMaxTime;
        }
    }
}
