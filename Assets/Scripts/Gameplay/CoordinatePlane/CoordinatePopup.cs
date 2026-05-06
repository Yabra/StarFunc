using System.Collections;
using StarFunc.Core;
using TMPro;
using UnityEngine;

namespace StarFunc.Gameplay
{
    /// <summary>
    /// Non-interactable world-space label that pops up at the player's tap
    /// location showing the plane coordinates (e.g. <c>(2, 3)</c>). Lives in
    /// the Level scene; subscribes to <see cref="TouchInputHandler.OnPlaneClicked"/>
    /// and auto-hides after <see cref="_displayDuration"/> seconds.
    /// </summary>
    public class CoordinatePopup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] TouchInputHandler _input;
        [SerializeField] CoordinatePlane _plane;
        [Tooltip("World-space TextMeshPro that displays the coordinate text.")]
        [SerializeField] TMP_Text _text;
        [Tooltip("Optional background sprite (panel behind the text). Pass " +
                 "null if the popup is text-only.")]
        [SerializeField] GameObject _background;

        [Header("Display")]
        [Tooltip("string.Format pattern for the coordinate text. {0} is x, " +
                 "{1} is y — already pre-formatted as strings.")]
        [SerializeField] string _format = "({0}, {1})";
        [Tooltip("Seconds the popup stays visible after a click. Each new " +
                 "click resets the timer.")]
        [SerializeField] float _displayDuration = 1.5f;
        [Tooltip("Plane-space offset added to the click position so the " +
                 "popup sits above (not on top of) the player's finger / cursor.")]
        [SerializeField] Vector2 _planeOffset = new(0f, 0.4f);

        [Header("Zoom Compensation")]
        [Tooltip("Camera used to read orthographicSize for zoom-aware " +
                 "scaling. Falls back to Camera.main if left blank.")]
        [SerializeField] Camera _camera;
        [Tooltip("Orthographic size at which the popup renders at its " +
                 "authored (1×) world scale. Smaller orthoSize means the " +
                 "camera is zoomed in, so we shrink the popup proportionally " +
                 "to keep its on-screen size constant.")]
        [SerializeField] float _referenceOrthoSize = 1f;

        Coroutine _hideRoutine;

        void Awake()
        {
            SetVisible(false);
        }

        void LateUpdate()
        {
            // Compensate for camera zoom each frame so the popup keeps a
            // constant on-screen size whether the player has zoomed in
            // close or pulled the camera back. LateUpdate so we run after
            // any zoom tween for the current frame.
            var cam = _camera != null ? _camera : Camera.main;
            if (cam == null || !cam.orthographic || _referenceOrthoSize <= 0f) return;

            float s = cam.orthographicSize / _referenceOrthoSize;
            transform.localScale = new Vector3(s, s, 1f);
        }

        void OnEnable()
        {
            if (_input != null) _input.OnPlaneClicked += HandleClick;
        }

        void OnDisable()
        {
            if (_input != null) _input.OnPlaneClicked -= HandleClick;
            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
                _hideRoutine = null;
            }
        }

        void HandleClick(Vector2 planePos)
        {
            if (_text == null || _plane == null) return;

            // Format integer-ish coords without a trailing ".0" for cleaner
            // reading; fractional coords show a single decimal.
            _text.text = string.Format(_format,
                FormatCoord(planePos.x), FormatCoord(planePos.y));

            // Position in world space at the click + small offset so the
            // popup doesn't sit directly under the finger.
            Vector2 displayPlanePos = planePos + _planeOffset;
            Vector2 worldPos = _plane.PlaneToWorld(displayPlanePos);
            var t = transform;
            var p = t.position;
            t.position = new Vector3(worldPos.x, worldPos.y, p.z);

            SetVisible(true);

            if (_hideRoutine != null) StopCoroutine(_hideRoutine);
            _hideRoutine = StartCoroutine(HideAfter(_displayDuration));
        }

        IEnumerator HideAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            SetVisible(false);
            _hideRoutine = null;
        }

        void SetVisible(bool visible)
        {
            if (_text != null) _text.gameObject.SetActive(visible);
            if (_background != null) _background.SetActive(visible);
        }

        static string FormatCoord(float v)
        {
            int rounded = Mathf.RoundToInt(v);
            return Mathf.Approximately(v, rounded)
                ? rounded.ToString()
                : v.ToString("F1");
        }
    }
}
