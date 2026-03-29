using UnityEngine;

namespace StarFunc.Data
{
    [CreateAssetMenu(menuName = "StarFunc/Data/FunctionDefinition")]
    public class FunctionDefinition : ScriptableObject
    {
        [Header("Function")]
        public FunctionType Type;

        [Header("Formula")]
        public float[] Coefficients;

        [Header("Domain")]
        public Vector2 DomainRange;
    }
}
