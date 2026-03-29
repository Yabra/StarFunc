using StarFunc.Core;
using UnityEngine;

namespace StarFunc.Gameplay
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GridRenderer : MonoBehaviour
    {
        [Header("Material")]
        [SerializeField] Material _gridMaterial;

        [Header("Shader Defaults")]
        [SerializeField] float _gridThickness = 0.03f;
        [SerializeField] float _axisThickness = 0.06f;

        MeshFilter _meshFilter;
        MeshRenderer _meshRenderer;
        MaterialPropertyBlock _props;

        static readonly int PropGridStep = Shader.PropertyToID("_GridStep");
        static readonly int PropGridColor = Shader.PropertyToID("_GridColor");
        static readonly int PropAxisColorX = Shader.PropertyToID("_AxisColorX");
        static readonly int PropAxisColorY = Shader.PropertyToID("_AxisColorY");
        static readonly int PropGridThickness = Shader.PropertyToID("_GridThickness");
        static readonly int PropAxisThickness = Shader.PropertyToID("_AxisThickness");

        void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _props = new MaterialPropertyBlock();

            if (_gridMaterial)
                _meshRenderer.sharedMaterial = _gridMaterial;
        }

        public void UpdateGrid(Vector2 planeMin, Vector2 planeMax, float gridStep)
        {
            RebuildQuad(planeMin, planeMax);
            ApplyProperties(gridStep);
        }

        /// <summary>
        /// Called by PlaneCamera when zoom changes.
        /// Adjusts grid thickness so lines stay pixel-consistent.
        /// </summary>
        public void UpdateZoom(float orthoSize)
        {
            // Scale thickness proportionally to keep ~constant screen-pixel width.
            // Reference: at orthoSize 6, use default thickness values.
            const float referenceOrthoSize = 6f;
            float scale = orthoSize / referenceOrthoSize;

            _props.SetFloat(PropGridThickness, _gridThickness * scale);
            _props.SetFloat(PropAxisThickness, _axisThickness * scale);
            _meshRenderer.SetPropertyBlock(_props);
        }

        void RebuildQuad(Vector2 min, Vector2 max)
        {
            var mesh = new Mesh
            {
                name = "CoordinateGridQuad",
                vertices = new[]
                { 
                    (Vector3)min,
                    new Vector3(max.x, min.y, 0f),
                    (Vector3)max,
                    new Vector3(min.x, max.y, 0f)
                },
                triangles = new[] { 0, 2, 1, 0, 3, 2 }
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            _meshFilter.mesh = mesh;
        }

        void ApplyProperties(float gridStep)
        {
            Color gridColor = ColorTokens.BG_SECOND;
            gridColor.a = 0.4f;

            _props.SetFloat(PropGridStep, gridStep);
            _props.SetColor(PropGridColor, gridColor);
            _props.SetColor(PropAxisColorX, ColorTokens.AXIS_X);
            _props.SetColor(PropAxisColorY, ColorTokens.AXIS_Y);
            _props.SetFloat(PropGridThickness, _gridThickness);
            _props.SetFloat(PropAxisThickness, _axisThickness);
            _meshRenderer.SetPropertyBlock(_props);
        }
    }
}
