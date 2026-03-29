using StarFunc.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace StarFunc.Gameplay
{
    public class PlaneCamera : MonoBehaviour, DefaultInputSystemActions.IGameActions
    {
        [Header("References")]
        [SerializeField] Camera _camera;
        [SerializeField] GridRenderer _gridRenderer;
        [SerializeField] CoordinateLabeler _labeler;

        [Header("Zoom Settings")]
        [SerializeField] float _minOrthoSize = 2f;
        [SerializeField] float _maxOrthoSize = 15f;
        [SerializeField] float _scrollSensitivity = 0.5f;
        [SerializeField] float _pinchSensitivity = 0.01f;
        [SerializeField] float _zoomSmoothing = 10f;

        float _targetOrthoSize;
        float _pendingScroll;

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
            // Fit the full plane with some padding
            float planeWidth = planeMax.x - planeMin.x;
            float planeHeight = planeMax.y - planeMin.y;

            var cam = GetCam();
            if (!cam) return;

            float aspect = cam.aspect;
            float sizeForHeight = planeHeight * 0.55f;
            float sizeForWidth = (planeWidth * 0.55f) / aspect;
            float fitSize = Mathf.Max(sizeForHeight, sizeForWidth);

            _targetOrthoSize = Mathf.Clamp(fitSize, _minOrthoSize, _maxOrthoSize);
            cam.orthographicSize = _targetOrthoSize;

            // Center camera on the plane
            Vector2 center = (planeMin + planeMax) * 0.5f;
            var camTransform = cam.transform;
            camTransform.position = new Vector3(center.x, center.y, camTransform.position.z);

            NotifyZoomChanged(_targetOrthoSize);
        }

        void Update()
        {
            HandleScrollZoom();
            HandlePinchZoom();
            ApplySmoothing();
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
