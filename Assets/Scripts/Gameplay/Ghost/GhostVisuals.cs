using UnityEngine;
using StarFunc.Core;
using StarFunc.Data;

namespace StarFunc.Gameplay
{
    public class GhostVisuals : MonoBehaviour
    {
        [SerializeField] SpriteRenderer _mainSprite;
        [SerializeField] SpriteRenderer _glowSprite;

        public void ApplyEmotion(GhostEmotion emotion)
        {
            Color color = emotion switch
            {
                GhostEmotion.Happy => ColorTokens.SUCCESS,
                GhostEmotion.Sad => ColorTokens.ERROR,
                GhostEmotion.Excited => ColorTokens.ACCENT_PINK,
                GhostEmotion.Determined => ColorTokens.LINE_PRIMARY,
                _ => ColorTokens.UI_NEUTRAL
            };

            SetColor(color);
        }

        public void SetColor(Color color)
        {
            _mainSprite.color = color;
            if (_glowSprite)
                _glowSprite.color = new Color(color.r, color.g, color.b, _glowSprite.color.a);
        }

        public void SetGlow(bool enabled)
        {
            if (_glowSprite) _glowSprite.gameObject.SetActive(enabled);
        }

        public void SetAlpha(float alpha)
        {
            var c = _mainSprite.color;
            _mainSprite.color = new Color(c.r, c.g, c.b, alpha);

            if (_glowSprite)
            {
                var g = _glowSprite.color;
                _glowSprite.color = new Color(g.r, g.g, g.b, alpha * 0.4f);
            }
        }

        public void SetScale(float scale)
        {
            transform.localScale = Vector3.one * scale;
        }
    }
}
