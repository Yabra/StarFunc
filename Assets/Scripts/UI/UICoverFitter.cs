using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    /// <summary>
    /// Sizes this RectTransform so that its <see cref="Image.sprite"/> covers
    /// the parent rect with its aspect preserved — analogous to CSS
    /// <c>background-size: cover</c>. Used by the cutscene Background image so
    /// a square sprite on a portrait phone fills the height fully and lets
    /// the width spill past the screen edges, the same way
    /// <c>PersistentBackground</c> handles the world-space main background.
    /// <para>
    /// Recomputed in <see cref="OnRectTransformDimensionsChange"/> (which
    /// fires when the parent rect changes) and <see cref="OnEnable"/> so a
    /// sprite swap or aspect-ratio change is picked up automatically.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UICoverFitter : MonoBehaviour
    {
        [Tooltip("Image whose sprite drives the aspect ratio. Defaults to an " +
                 "Image on this GameObject.")]
        [SerializeField] Image _image;

        RectTransform _rect;
        Sprite _lastSprite;

        void Awake()
        {
            _rect = (RectTransform)transform;
            if (_image == null) _image = GetComponent<Image>();
        }

        void OnEnable() => Apply();

        // Fires when the parent rect (and therefore our resolvable size)
        // changes — covers screen rotation, dynamic safe-area updates, etc.
        void OnRectTransformDimensionsChange() => Apply();

        void LateUpdate()
        {
            // Catch sprite swaps mid-frame (e.g. cutscene frame change).
            if (_image != null && _image.sprite != _lastSprite) Apply();
        }

        void Apply()
        {
            if (_rect == null) _rect = (RectTransform)transform;
            if (_image == null) return;

            var sprite = _image.sprite;
            _lastSprite = sprite;
            if (sprite == null) return;

            var parent = _rect.parent as RectTransform;
            if (parent == null) return;

            float parentW = parent.rect.width;
            float parentH = parent.rect.height;
            if (parentW <= 0f || parentH <= 0f) return;

            float spriteW = sprite.rect.width;
            float spriteH = sprite.rect.height;
            if (spriteW <= 0f || spriteH <= 0f) return;

            float spriteAspect = spriteW / spriteH;
            float parentAspect = parentW / parentH;

            float targetW, targetH;
            if (spriteAspect > parentAspect)
            {
                // Sprite is wider than parent → match height, overflow width.
                targetH = parentH;
                targetW = parentH * spriteAspect;
            }
            else
            {
                // Sprite is taller (or equally wide) → match width, overflow height.
                targetW = parentW;
                targetH = parentW / spriteAspect;
            }

            // Anchor at parent center so the overflow is symmetric.
            _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.anchoredPosition = Vector2.zero;
            _rect.sizeDelta = new Vector2(targetW, targetH);
        }
    }
}
