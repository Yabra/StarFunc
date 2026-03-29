using UnityEngine;

namespace StarFunc.Data
{
    [CreateAssetMenu(menuName = "StarFunc/Data/CutsceneData")]
    public class CutsceneData : ScriptableObject
    {
        [Header("Identity")]
        public string CutsceneId;

        [Header("Frames")]
        public CutsceneFrame[] Frames;
    }
}
