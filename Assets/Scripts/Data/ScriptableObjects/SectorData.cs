using UnityEngine;

namespace StarFunc.Data
{
    [CreateAssetMenu(menuName = "StarFunc/Data/SectorData")]
    public class SectorData : ScriptableObject
    {
        /// <summary>
        /// The sector the player launched the current level from. Set by
        /// SectorScreen on tap, cleared by SceneFlowManager on Level unload.
        /// Mirrors the <see cref="LevelData.ActiveLevel"/> pattern; lets the
        /// Level scene reach back to its parent sector (e.g. for the result
        /// screen's constellation preview) without a back-pointer on every
        /// LevelData.
        /// </summary>
        public static SectorData ActiveSector { get; set; }

        [Header("Identity")]
        public string SectorId;
        public string DisplayName;
        public int SectorIndex;

        [Header("Levels")]
        public LevelData[] Levels;

        [Header("Unlock Conditions")]
        public SectorData PreviousSector;
        public int RequiredStarsToUnlock;

        [Header("Visual")]
        public Sprite ConstellationSprite;
        public Sprite ConstellationRestoredSprite;
        public Sprite SectorIcon;
        public Color AccentColor;
        public Color StarColor;

        [Header("Narrative")]
        public CutsceneData IntroCutscene;
        public CutsceneData OutroCutscene;
    }
}
