using UnityEngine;

namespace StarFunc.UI
{
    /// <summary>
    /// Single shared background sprite for Boot and Hub. The first instance
    /// to <c>Awake</c> wins and persists via <c>DontDestroyOnLoad</c>;
    /// subsequent instances (e.g. an editor-time copy left in the Hub scene
    /// for direct-scene testing) destroy themselves so designers can't end up
    /// with two backgrounds drifting apart.
    /// <para>
    /// Each frame the sprite is rescaled so its world height matches the
    /// active orthographic camera's vertical view — the texture is a 4096²
    /// square, so the camera always sees a full-height slice plus whatever
    /// horizontal extent the screen aspect ratio reveals (the game is
    /// portrait, so the sprite is wider than the visible window in normal
    /// aspect ratios).
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class PersistentBackground : MonoBehaviour
    {
        [SerializeField] SpriteRenderer _renderer;
        [SerializeField] bool _followCamera = true;

        static PersistentBackground _instance;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void LateUpdate()
        {
            if (_renderer == null || _renderer.sprite == null) return;

            var cam = Camera.main;
            if (cam == null || !cam.orthographic) return;

            float worldHeight = cam.orthographicSize * 2f;
            float spriteHeight = _renderer.sprite.bounds.size.y;
            if (spriteHeight <= 0f) return;

            float scale = worldHeight / spriteHeight;
            // Uniform scale preserves the texture's aspect — height fits the
            // viewport, width follows along (the camera shows the central
            // strip).
            transform.localScale = new Vector3(scale, scale, 1f);

            if (_followCamera)
            {
                var camPos = cam.transform.position;
                var pos = transform.position;
                transform.position = new Vector3(camPos.x, camPos.y, pos.z);
            }
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
