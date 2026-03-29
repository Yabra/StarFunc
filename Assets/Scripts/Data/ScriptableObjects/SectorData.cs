using UnityEngine;

namespace StarFunc.Data
{
    [CreateAssetMenu(menuName = "StarFunc/Data/SectorData")]
    public class SectorData : ScriptableObject
    {
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
        public float[] ConstellationStarAngles;

        [Header("Narrative")]
        public CutsceneData IntroCutscene;
        public CutsceneData OutroCutscene;
    }
}
