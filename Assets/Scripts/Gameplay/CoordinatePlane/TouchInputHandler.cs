using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace StarFunc.Gameplay
{
    public class TouchInputHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] CoordinatePlane _plane;
        [SerializeField] Camera _camera;

        [Header("Options")]
        [SerializeField] bool _snapToGrid;
        [Tooltip("Maximum pixel distance the pointer can travel between " +
                 "press and release before we treat the gesture as a drag " +
                 "(camera pan) instead of a tap.")]
        [SerializeField] float _tapMaxMovePixels = 12f;

        Vector2 _pressScreenPos;
        bool _pressInProgress;
        bool _pressOverUI;

        /// <summary>
        /// Fired when the player taps/clicks the coordinate plane.
        /// The Vector2 is in plane-coordinate space.
        /// </summary>
        public event Action<Vector2> OnPlaneClicked;

        void Update()
        {
            var mouse = Mouse.current;
            var touch = Touchscreen.current;

            // We fire on RELEASE rather than press so PlaneCamera's drag
            // gesture (which translates the camera while the pointer is
            // held) doesn't also drop a star at the press location. Tap is
            // defined as press → release with very little movement.
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                    BeginPress(mouse.position.ReadValue());
                else if (mouse.leftButton.wasReleasedThisFrame)
                    ResolvePress(mouse.position.ReadValue());
                return;
            }

            if (touch != null)
            {
                if (touch.primaryTouch.press.wasPressedThisFrame)
                    BeginPress(touch.primaryTouch.position.ReadValue());
                else if (touch.primaryTouch.press.wasReleasedThisFrame)
                    ResolvePress(touch.primaryTouch.position.ReadValue());
            }
        }

        void BeginPress(Vector2 screenPos)
        {
            _pressInProgress = true;
            _pressScreenPos = screenPos;
            _pressOverUI = EventSystem.current != null
                           && EventSystem.current.IsPointerOverGameObject();
        }

        void ResolvePress(Vector2 screenPos)
        {
            if (!_pressInProgress) return;
            _pressInProgress = false;

            // UI-press → no plane interaction. Drag (moved beyond the tap
            // threshold) → camera-pan only, no plane click.
            if (_pressOverUI) return;
            if (Vector2.Distance(screenPos, _pressScreenPos) > _tapMaxMovePixels) return;

            HandlePointer(screenPos);
        }

        void HandlePointer(Vector2 screenPos)
        {
            // Don't react to taps on UI elements
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var cam = _camera ? _camera : Camera.main;
            if (!cam) return;

            Vector3 worldPos3 = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
            Vector2 worldPos = new(worldPos3.x, worldPos3.y);
            Vector2 planePos = _plane.WorldToPlane(worldPos);

            if (_snapToGrid)
            {
                float step = _plane.GridStep;
                planePos.x = Mathf.Round(planePos.x / step) * step;
                planePos.y = Mathf.Round(planePos.y / step) * step;
            }

            Debug.Log($"[CoordinatePlane] Tap → plane ({planePos.x:F2}, {planePos.y:F2})");
            OnPlaneClicked?.Invoke(planePos);
        }
    }
}
