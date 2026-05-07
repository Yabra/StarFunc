using UnityEngine;
using StarFunc.Core;
using StarFunc.Data;

namespace StarFunc.Gameplay
{
    public class StarVisuals : MonoBehaviour
    {
        [SerializeField] SpriteRenderer _mainSprite;
        [SerializeField] SpriteRenderer _glowSprite;

        Vector3 _baseScale = Vector3.one;
        bool _baseScaleCaptured;

        void Awake()
        {
            // Capture authored localScale once. SetScale tweens are expressed
            // as a scalar multiplier of this base — without it, callers that
            // pass 1f at the end of an animation would snap the star to
            // (1,1,1) regardless of the prefab's authored scale.
            _baseScale = transform.localScale;
            _baseScaleCaptured = true;
        }

        public void ApplyState(StarState state)
        {
            switch (state)
            {
                case StarState.Hidden:
                    _mainSprite.enabled = false;
                    SetGlow(false);
                    break;

                case StarState.Active:
                    _mainSprite.enabled = true;
                    SetColor(ColorTokens.POINT_PRIMARY);
                    SetGlow(true);
                    break;

                case StarState.Placed:
                    _mainSprite.enabled = true;
                    SetColor(ColorTokens.SUCCESS);
                    SetGlow(true);
                    break;

                case StarState.Incorrect:
                    _mainSprite.enabled = true;
                    SetColor(ColorTokens.ERROR);
                    SetGlow(false);
                    break;

                case StarState.Restored:
                    _mainSprite.enabled = true;
                    SetColor(ColorTokens.SUCCESS);
                    SetGlow(true);
                    break;
            }
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

        /// <summary>Current main-sprite color (for save/restore around flashes).</summary>
        public Color GetColor() => _mainSprite ? _mainSprite.color : Color.white;

        /// <summary>Override only the main sprite color (does not touch glow). Used for flashes.</summary>
        public void SetMainColor(Color color)
        {
            if (_mainSprite) _mainSprite.color = color;
        }

        /// <summary>Set glow opacity in [0..1]. Used by glow-pulse animations.</summary>
        public void SetGlowAlpha(float alpha)
        {
            if (!_glowSprite) return;
            var c = _glowSprite.color;
            _glowSprite.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(alpha));
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
            if (!_baseScaleCaptured) _baseScale = transform.localScale;
            transform.localScale = _baseScale * scale;
        }
    }
}
