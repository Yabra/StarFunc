using StarFunc.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace StarFunc.Gameplay
{
    public class PlaneCamera : MonoBehaviour, DefaultInputSystemActions.IGameActions
    {
        [Header("References")]
        [SerializeField] Camera _camera;
        [SerializeField] GridRenderer _gridRenderer;
        [SerializeField] CoordinateLabeler _labeler;

        [Header("Zoom Settings")]
        [SerializeField] float _minOrthoSize = 1f;
        [SerializeField] float _maxOrthoSize = 15f;
        [Tooltip("Orthographic size the camera lands on when a level loads. " +
                 "Auto-fit-the-whole-plane was the old behaviour (made sense " +
                 "when the grid was ±5); with the ±20 grid it ends up zoomed " +
                 "out too far to be comfortable. Designer tunes the entry " +
                 "framing here.")]
        [SerializeField] float _initialOrthoSize = 6f;
        [SerializeField] float _scrollSensitivity = 0.5f;
        [SerializeField] float _pinchSensitivity = 0.01f;
        [SerializeField] float _zoomSmoothing = 10f;

        [Header("Pan Settings")]
        [Tooltip("Skip drag when the pointer started over a UI element so " +
                 "popups and HUD buttons don't double as pan handles.")]
        [SerializeField] bool _ignorePanWhenOverUI = true;
        [Tooltip("Multiplier on the pan delta. 1 = world point under cursor " +
                 "stays under cursor (true map-pan). >1 = camera moves " +
                 "faster than the cursor, useful when the plane is much " +
                 "larger than the viewport (±20 grid).")]
        [SerializeField] float _panSensitivity = 2.5f;

        float _targetOrthoSize;
        float _pendingScroll;

        Vector2 _planeMin;
        Vector2 _planeMax;
        bool _hasBounds;

        bool _isPanning;
        Vector2 _lastPanScreenPos;

        DefaultInputSystemActions _input;

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();

            _input = new DefaultInputSystemActions();
            _input.Game.AddCallbacks(this);
            _input.Game.Enable();
        }

        void OnDisable()
        {
            _input.Game.RemoveCallbacks(this);
            _input.Game.Disable();
            _input.Dispose();
            _input = null;

            EnhancedTouchSupport.Disable();
        }

        public void OnZoom(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
                _pendingScroll += ctx.ReadValue<Vector2>().y;
        }

        public void OnPan(InputAction.CallbackContext ctx)
        {
            // Pan support will be added later
        }

        public void SetBounds(Vector2 planeMin, Vector2 planeMax)
        {
            _planeMin = planeMin;
            _planeMax = planeMax;
            _hasBounds = true;

            var cam = GetCam();
            if (!cam) return;

            // Land on the designer-chosen entry zoom rather than auto-fitting
            // the whole plane (which is too zoomed-out for the ±20 grid).
            _targetOrthoSize = Mathf.Clamp(_initialOrthoSize, _minOrthoSize, _maxOrthoSize);
            cam.orthographicSize = _targetOrthoSize;

            // Center camera on the plane.
            Vector2 center = (planeMin + planeMax) * 0.5f;
            var camTransform = cam.transform;
            camTransform.position = new Vector3(center.x, center.y, camTransform.position.z);

            NotifyZoomChanged(_targetOrthoSize);
        }

        void Update()
        {
            HandleScrollZoom();
            HandlePinchZoom();
            HandlePan();
            ApplySmoothing();
        }

        void HandlePan()
        {
            // Pan with single-finger drag (mobile) or left-click drag
            // (editor / desktop). Two-finger pinch is handled separately
            // and shouldn't ALSO pan the camera, so we bail when there's
            // more than one active touch.
            var mouse = Mouse.current;
            var touch = Touchscreen.current;

            bool pressed = false;
            bool justPressed = false;
            bool justReleased = false;
            Vector2 screenPos = Vector2.zero;

            if (touch != null && Touch.activeTouches.Count > 0)
            {
                if (Touch.activeTouches.Count > 1)
                {
                    // Pinch in progress — abort any active pan.
                    _isPanning = false;
                    return;
                }
                var t = Touch.activeTouches[0];
                screenPos = t.screenPosition;
                pressed = t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled;
                justPressed = t.phase == TouchPhase.Began;
                justReleased = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
            }
            else if (mouse != null)
            {
                screenPos = mouse.position.ReadValue();
                pressed = mouse.leftButton.isPressed;
                justPressed = mouse.leftButton.wasPressedThisFrame;
                justReleased = mouse.leftButton.wasReleasedThisFrame;
            }

            if (justReleased) _isPanning = false;

            if (justPressed)
            {
                if (_ignorePanWhenOverUI && IsPointerOverUI(screenPos))
                    return;
                _isPanning = true;
                _lastPanScreenPos = screenPos;
                return;
            }

            if (!_isPanning || !pressed) return;

            var cam = GetCam();
            if (!cam) return;

            // Translate camera by the world-space delta between the
            // previous and current pointer positions, in the OPPOSITE
            // direction so the world feels grabbed by the cursor.
            Vector3 prevWorld = cam.ScreenToWorldPoint(
                new Vector3(_lastPanScreenPos.x, _lastPanScreenPos.y, 0f));
            Vector3 currWorld = cam.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, 0f));
            Vector3 worldDelta = currWorld - prevWorld;

            var camPos = cam.transform.position;
            camPos.x -= worldDelta.x * _panSensitivity;
            camPos.y -= worldDelta.y * _panSensitivity;
            cam.transform.position = ClampToBounds(camPos, cam);

            _lastPanScreenPos = screenPos;
        }

        Vector3 ClampToBounds(Vector3 desired, Camera cam)
        {
            if (!_hasBounds) return desired;

            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            // If the plane is smaller than the viewport in either axis, snap
            // to the plane centre on that axis instead of clamping (which
            // would pin the camera to a corner).
            float planeW = _planeMax.x - _planeMin.x;
            float planeH = _planeMax.y - _planeMin.y;
            float centerX = (_planeMin.x + _planeMax.x) * 0.5f;
            float centerY = (_planeMin.y + _planeMax.y) * 0.5f;

            desired.x = halfW * 2f >= planeW
                ? centerX
                : Mathf.Clamp(desired.x, _planeMin.x + halfW, _planeMax.x - halfW);
            desired.y = halfH * 2f >= planeH
                ? centerY
                : Mathf.Clamp(desired.y, _planeMin.y + halfH, _planeMax.y - halfH);
            return desired;
        }

        static bool IsPointerOverUI(Vector2 screenPos)
        {
            if (EventSystem.current == null) return false;
            // Old-style is fine here — ScreenToWorldPoint integrations
            // already use mouse.position; this matches.
            return EventSystem.current.IsPointerOverGameObject();
        }

        void HandleScrollZoom()
        {
            if (Mathf.Approximately(_pendingScroll, 0f)) return;

            _targetOrthoSize -= Mathf.Sign(_pendingScroll) * _scrollSensitivity;
            _targetOrthoSize = Mathf.Clamp(_targetOrthoSize, _minOrthoSize, _maxOrthoSize);
            _pendingScroll = 0f;
        }

        void HandlePinchZoom()
        {
            if (Touch.activeTouches.Count < 2) return;

            var touch0 = Touch.activeTouches[0];
            var touch1 = Touch.activeTouches[1];

            float prevDist = Vector2.Distance(
                touch0.screenPosition - touch0.delta,
                touch1.screenPosition - touch1.delta);
            float currDist = Vector2.Distance(
                touch0.screenPosition,
                touch1.screenPosition);

            float delta = prevDist - currDist;
            _targetOrthoSize += delta * _pinchSensitivity;
            _targetOrthoSize = Mathf.Clamp(_targetOrthoSize, _minOrthoSize, _maxOrthoSize);
        }

        void ApplySmoothing()
        {
            var cam = GetCam();
            if (!cam) return;

            float prev = cam.orthographicSize;
            cam.orthographicSize = Mathf.Lerp(prev, _targetOrthoSize, Time.deltaTime * _zoomSmoothing);

            if (!Mathf.Approximately(prev, cam.orthographicSize))
                NotifyZoomChanged(cam.orthographicSize);
        }

        void NotifyZoomChanged(float orthoSize)
        {
            if (_gridRenderer) _gridRenderer.UpdateZoom(orthoSize);
            if (_labeler) _labeler.UpdateForZoom(orthoSize);
        }

        Camera GetCam()
        {
            if (_camera) return _camera;
            _camera = Camera.main;
            return _camera;
        }
    }
}
