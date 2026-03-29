using UnityEngine;

namespace StarFunc.Gameplay
{
    public class CoordinatePlane : MonoBehaviour
    {
        [Header("Defaults (used if Initialize() is not called)")]
        [SerializeField] Vector2 _planeMin = new(-5f, -5f);
        [SerializeField] Vector2 _planeMax = new(5f, 5f);
        [SerializeField] float _gridStep = 1f;

        [Header("Sub-components")]
        [SerializeField] GridRenderer _gridRenderer;
        [SerializeField] CoordinateLabeler _labeler;
        [SerializeField] TouchInputHandler _touchInput;
        [SerializeField] PlaneCamera _planeCamera;

        public Vector2 PlaneMin => _planeMin;
        public Vector2 PlaneMax => _planeMax;
        public float GridStep => _gridStep;

        bool _initialized;

        void Start()
        {
            if (!_initialized)
                Initialize(_planeMin, _planeMax, _gridStep);
        }

        public void Initialize(Vector2 planeMin, Vector2 planeMax, float gridStep)
        {
            _planeMin = planeMin;
            _planeMax = planeMax;
            _gridStep = gridStep;
            _initialized = true;

            if (_gridRenderer) _gridRenderer.UpdateGrid(planeMin, planeMax, gridStep);
            if (_labeler) _labeler.Rebuild(planeMin, planeMax, gridStep);
            if (_planeCamera) _planeCamera.SetBounds(planeMin, planeMax);
        }

        /// <summary>
        /// Converts a world-space position to coordinate-plane space.
        /// </summary>
        public Vector2 WorldToPlane(Vector2 worldPos)
        {
            Vector2 origin = transform.position;
            return worldPos - origin;
        }

        /// <summary>
        /// Converts a coordinate-plane position to world-space.
        /// </summary>
        public Vector2 PlaneToWorld(Vector2 planePos)
        {
            Vector2 origin = transform.position;
            return planePos + origin;
        }
    }
}
