using UnityEngine;

namespace StarFunc.Gameplay
{
    public class GhostPositioner : MonoBehaviour
    {
        [SerializeField] CoordinatePlane _coordinatePlane;
        [SerializeField] Vector2 _levelOffset = new(3f, 0f);

        void Start()
        {
            if (_coordinatePlane)
                SetLevelPosition();
        }

        public void SetLevelPosition()
        {
            if (!_coordinatePlane) return;

            Vector2 planeMax = _coordinatePlane.PlaneMax;
            Vector2 anchor = new(planeMax.x, (planeMax.y + _coordinatePlane.PlaneMin.y) * 0.5f);
            Vector2 worldPos = _coordinatePlane.PlaneToWorld(anchor) + _levelOffset;
            transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
        }

        public void SetHubPosition(Vector2 position)
        {
            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }
    }
}
