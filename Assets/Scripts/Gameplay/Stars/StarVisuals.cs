using UnityEngine;
using StarFunc.Core;
using StarFunc.Data;

namespace StarFunc.Gameplay
{
    public class StarVisuals : MonoBehaviour
    {
        [SerializeField] SpriteRenderer _mainSprite;
        [SerializeField] SpriteRenderer _glowSprite;

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
