using UnityEngine;
using StarFunc.Data;

namespace StarFunc.Gameplay
{
    public class GhostEntity : MonoBehaviour
    {
        [SerializeField] GhostVisuals _visuals;
        [SerializeField] GhostAnimator _animator;
        [SerializeField] GhostEmotionController _emotionController;
        [SerializeField] GhostPositioner _positioner;

        GhostEmotion _currentEmotion = GhostEmotion.Idle;

        public GhostEmotion CurrentEmotion => _currentEmotion;

        public void SetEmotion(GhostEmotion emotion)
        {
            _currentEmotion = emotion;
            _visuals.ApplyEmotion(emotion);
            _animator.PlayEmotionReaction(emotion);
        }
    }
}
