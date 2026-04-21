using UnityEngine;

namespace StarFunc.Data
{
    [CreateAssetMenu(menuName = "StarFunc/Config/BalanceConfig", fileName = "BalanceConfig")]
    public class BalanceConfig : ScriptableObject
    {
        [Header("Lives")]
        public int MaxLives = 5;
        public int RestoreIntervalSeconds = 1800;
        public int RestoreCostFragments = 20;

        [Header("Economy")]
        public int SkipLevelCostFragments = 100;
        public int ImprovementBonusPerStar = 5;
        public int HintCostFragments = 10;
    }
}
