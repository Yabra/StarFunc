using StarFunc.Data;
using UnityEngine;

namespace StarFunc.Meta
{
    /// <summary>
    /// Designer-facing VFX config: per-FeedbackType prefab spawned at the
    /// callsite's world position by <see cref="FeedbackService"/>. Each prefab
    /// is expected to be a self-destructing one-shot ParticleSystem
    /// (StopAction = Destroy).
    /// </summary>
    [CreateAssetMenu(menuName = "StarFunc/Config/VfxConfig", fileName = "VfxConfig")]
    public class VfxConfig : ScriptableObject
    {
        [Header("Feedback VFX")]
        public GameObject StarPlaced;
        public GameObject StarError;
        public GameObject LevelComplete;
        public GameObject ConstellationRestored;
        public GameObject SectorUnlock;
        public GameObject ButtonTap;

        public GameObject GetVfxPrefab(FeedbackType type) => type switch
        {
            FeedbackType.StarPlaced => StarPlaced,
            FeedbackType.StarError => StarError,
            FeedbackType.LevelComplete => LevelComplete,
            FeedbackType.ConstellationRestored => ConstellationRestored,
            FeedbackType.SectorUnlock => SectorUnlock,
            FeedbackType.ButtonTap => ButtonTap,
            _ => null
        };
    }
}
