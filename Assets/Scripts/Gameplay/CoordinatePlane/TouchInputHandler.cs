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

        /// <summary>
        /// Fired when the player taps/clicks the coordinate plane.
        /// The Vector2 is in plane-coordinate space.
        /// </summary>
        public event Action<Vector2> OnPlaneClicked;

        void Update()
        {
            var mouse = Mouse.current;
            var touch = Touchscreen.current;

            // Mouse click (editor / desktop)
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                HandlePointer(mouse.position.ReadValue());
                return;
            }

            // Single-finger tap (mobile)
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            {
                HandlePointer(touch.primaryTouch.position.ReadValue());
            }
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
