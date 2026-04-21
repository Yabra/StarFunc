using System;
using Newtonsoft.Json;

namespace StarFunc.Infrastructure
{
    #region Manifest

    [Serializable]
    public class ContentManifest
    {
        [JsonProperty("contentVersion")] public int ContentVersion;
        [JsonProperty("sectors")] public SectorVersionEntry[] Sectors;
        [JsonProperty("shopCatalogVersion")] public int ShopCatalogVersion;
        [JsonProperty("balanceConfigVersion")] public int BalanceConfigVersion;
    }

    [Serializable]
    public class SectorVersionEntry
    {
        [JsonProperty("sectorId")] public string SectorId;
        [JsonProperty("version")] public int Version;
        [JsonProperty("levelCount")] public int LevelCount;
    }

    #endregion

    #region Sector

    [Serializable]
    public class SectorsResponse
    {
        [JsonProperty("sectors")] public SectorDefinitionDto[] Sectors;
    }

    [Serializable]
    public class SectorResponse
    {
        [JsonProperty("sector")] public SectorDefinitionDto Sector;
    }

    [Serializable]
    public class SectorDefinitionDto
    {
        [JsonProperty("sectorId")] public string SectorId;
        [JsonProperty("displayName")] public string DisplayName;
        [JsonProperty("sectorIndex")] public int SectorIndex;
        [JsonProperty("levelIds")] public string[] LevelIds;
        [JsonProperty("previousSectorId")] public string PreviousSectorId;
        [JsonProperty("requiredStarsToUnlock")] public int RequiredStarsToUnlock;
        [JsonProperty("visual")] public SectorVisualDto Visual;
        [JsonProperty("introCutsceneId")] public string IntroCutsceneId;
        [JsonProperty("outroCutsceneId")] public string OutroCutsceneId;
    }

    [Serializable]
    public class SectorVisualDto
    {
        [JsonProperty("accentColor")] public string AccentColor;
        [JsonProperty("starColor")] public string StarColor;
    }

    #endregion

    #region Level

    [Serializable]
    public class LevelsResponse
    {
        [JsonProperty("levels")] public LevelDefinitionDto[] Levels;
    }

    [Serializable]
    public class LevelResponse
    {
        [JsonProperty("level")] public LevelDefinitionDto Level;
    }

    [Serializable]
    public class LevelDefinitionDto
    {
        [JsonProperty("levelId")] public string LevelId;
        [JsonProperty("levelIndex")] public int LevelIndex;
        [JsonProperty("sectorId")] public string SectorId;
        [JsonProperty("type")] public string Type;
        [JsonProperty("taskType")] public string TaskType;
        [JsonProperty("coordinatePlane")] public CoordinatePlaneDto CoordinatePlane;
        [JsonProperty("stars")] public StarDto[] Stars;
        [JsonProperty("referenceFunctions")] public FunctionDto[] ReferenceFunctions;
        [JsonProperty("answerOptions")] public AnswerOptionDto[] AnswerOptions;
        [JsonProperty("accuracyThreshold")] public float AccuracyThreshold;
        [JsonProperty("starRating")] public StarRatingDto StarRating;
        [JsonProperty("constraints")] public ConstraintsDto Constraints;
        [JsonProperty("visibility")] public VisibilityDto Visibility;
        [JsonProperty("tutorial")] public TutorialDto Tutorial;
        [JsonProperty("fragmentReward")] public int FragmentReward;
    }

    [Serializable]
    public class CoordinatePlaneDto
    {
        [JsonProperty("planeMin")] public Vec2Dto PlaneMin;
        [JsonProperty("planeMax")] public Vec2Dto PlaneMax;
        [JsonProperty("gridStep")] public float GridStep;
    }

    [Serializable]
    public class Vec2Dto
    {
        [JsonProperty("x")] public float X;
        [JsonProperty("y")] public float Y;
    }

    [Serializable]
    public class StarDto
    {
        [JsonProperty("starId")] public string StarId;
        [JsonProperty("coordinate")] public Vec2Dto Coordinate;
        [JsonProperty("initialState")] public string InitialState;
        [JsonProperty("isControlPoint")] public bool IsControlPoint;
        [JsonProperty("isDistractor")] public bool IsDistractor;
        [JsonProperty("belongsToSolution")] public bool BelongsToSolution;
        [JsonProperty("revealAfterAction")] public int RevealAfterAction;
    }

    [Serializable]
    public class FunctionDto
    {
        [JsonProperty("functionId")] public string FunctionId;
        [JsonProperty("type")] public string Type;
        [JsonProperty("coefficients")] public float[] Coefficients;
        [JsonProperty("domainRange")] public Vec2Dto DomainRange;
    }

    [Serializable]
    public class AnswerOptionDto
    {
        [JsonProperty("optionId")] public string OptionId;
        [JsonProperty("text")] public string Text;
        [JsonProperty("value")] public string Value;
        [JsonProperty("isCorrect")] public bool IsCorrect;
    }

    [Serializable]
    public class StarRatingDto
    {
        [JsonProperty("threeStarMaxErrors")] public int ThreeStarMaxErrors;
        [JsonProperty("twoStarMaxErrors")] public int TwoStarMaxErrors;
        [JsonProperty("oneStarMaxErrors")] public int OneStarMaxErrors;
        [JsonProperty("timerAffectsRating")] public bool TimerAffectsRating;
        [JsonProperty("threeStarMaxTime")] public float ThreeStarMaxTime;
    }

    [Serializable]
    public class ConstraintsDto
    {
        [JsonProperty("maxAttempts")] public int MaxAttempts;
        [JsonProperty("maxAdjustments")] public int MaxAdjustments;
    }

    [Serializable]
    public class VisibilityDto
    {
        [JsonProperty("useMemoryMode")] public bool UseMemoryMode;
        [JsonProperty("memoryDisplayDuration")] public float MemoryDisplayDuration;
        [JsonProperty("graphVisibility")] public GraphVisibilityDto GraphVisibility;
    }

    [Serializable]
    public class GraphVisibilityDto
    {
        [JsonProperty("partialReveal")] public bool PartialReveal;
        [JsonProperty("initialVisibleSegments")] public int InitialVisibleSegments;
        [JsonProperty("revealPerCorrectAction")] public int RevealPerCorrectAction;
    }

    [Serializable]
    public class TutorialDto
    {
        [JsonProperty("showHints")] public bool ShowHints;
        [JsonProperty("hints")] public HintDto[] Hints;
    }

    [Serializable]
    public class HintDto
    {
        [JsonProperty("trigger")] public string Trigger;
        [JsonProperty("hintText")] public string HintText;
        [JsonProperty("highlightPosition")] public Vec2Dto HighlightPosition;
        [JsonProperty("triggerAfterErrors")] public int TriggerAfterErrors;
    }

    #endregion

    #region Balance

    [Serializable]
    public class BalanceConfigDto
    {
        [JsonProperty("version")] public int Version;
        [JsonProperty("livesConfig")] public LivesConfigDto LivesConfig;
        [JsonProperty("skipLevelCostFragments")] public int SkipLevelCostFragments;
        [JsonProperty("improvementBonusPerStar")] public int ImprovementBonusPerStar;
        [JsonProperty("hintCostFragments")] public int HintCostFragments;
    }

    [Serializable]
    public class LivesConfigDto
    {
        [JsonProperty("maxLives")] public int MaxLives;
        [JsonProperty("restoreIntervalSeconds")] public int RestoreIntervalSeconds;
        [JsonProperty("restoreCostFragments")] public int RestoreCostFragments;
    }

    #endregion

    #region Shop

    [Serializable]
    public class ShopItemDto
    {
        [JsonProperty("itemId")] public string ItemId;
        [JsonProperty("displayName")] public string DisplayName;
        [JsonProperty("description")] public string Description;
        [JsonProperty("category")] public string Category;
        [JsonProperty("price")] public int Price;
        [JsonProperty("isConsumable")] public bool IsConsumable;
        [JsonProperty("quantity")] public int? Quantity;
        [JsonProperty("iconId")] public string IconId;
        [JsonProperty("isAvailable")] public bool IsAvailable;
    }

    #endregion

    #region Cached manifest (local persistence)

    [Serializable]
    public class CachedManifest
    {
        [JsonProperty("contentVersion")] public int ContentVersion;
        [JsonProperty("balanceConfigVersion")] public int BalanceConfigVersion;
        [JsonProperty("shopCatalogVersion")] public int ShopCatalogVersion;
        [JsonProperty("sectorVersions")] public CachedSectorVersion[] SectorVersions;
    }

    [Serializable]
    public class CachedSectorVersion
    {
        [JsonProperty("sectorId")] public string SectorId;
        [JsonProperty("version")] public int Version;
    }

    #endregion
}
