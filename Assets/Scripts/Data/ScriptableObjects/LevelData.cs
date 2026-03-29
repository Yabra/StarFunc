using UnityEngine;

namespace StarFunc.Data
{
    [CreateAssetMenu(menuName = "StarFunc/Data/LevelData")]
    public class LevelData : ScriptableObject
    {
        /// <summary>
        /// Set by SceneFlowManager before additive Level scene load.
        /// Read by LevelController on Start() to auto-initialize.
        /// </summary>
        public static LevelData ActiveLevel { get; set; }

        [Header("Identity")]
        public string LevelId;
        public int LevelIndex;

        [Header("Type")]
        public LevelType Type;

        [Header("Coordinate Plane")]
        public Vector2 PlaneMin;
        public Vector2 PlaneMax;
        public float GridStep = 1f;

        [Header("Stars")]
        public StarConfig[] Stars;

        [Header("Task")]
        public TaskType TaskType;
        public FunctionDefinition[] ReferenceFunctions;
        public AnswerOption[] AnswerOptions;

        [Header("Validation")]
        public float AccuracyThreshold;
        public StarRatingConfig StarRating;

        [Header("Constraints")]
        public int MaxAttempts;
        public int MaxAdjustments;

        [Header("Visibility")]
        public bool UseMemoryMode;
        public float MemoryDisplayDuration;
        public GraphVisibilityConfig GraphVisibility;

        [Header("Tutorial")]
        public bool ShowHints;
        public HintConfig[] Hints;

        [Header("Rewards")]
        public int FragmentReward;
    }
}
