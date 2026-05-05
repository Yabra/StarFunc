using UnityEngine;

namespace StarFunc.Core
{
    /// <summary>
    /// Disables a target Behaviour (or this whole GameObject) on Awake if
    /// another active instance of the same component type already exists in
    /// any loaded scene. Lets us keep an EventSystem / AudioListener in every
    /// scene for direct-scene testing while silencing the duplicate-spam
    /// warnings when scenes are loaded additively (Hub → Level).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class DuplicateAvoider : MonoBehaviour
    {
        [Tooltip("The component to silence if a duplicate is detected.")]
        [SerializeField] Behaviour _target;

        [Tooltip("If true, deactivates the entire GameObject instead of just " +
                 "_target — pick this for things like EventSystem where the " +
                 "InputModule sibling should also stop.")]
        [SerializeField] bool _disableGameObject;

        void Awake()
        {
            if (_target == null) return;
            var matches = FindObjectsByType(
                _target.GetType(),
                FindObjectsInactive.Exclude);

            // Anything other than us means a duplicate already exists.
            for (int i = 0; i < matches.Length; i++)
            {
                if (ReferenceEquals(matches[i], _target)) continue;

                if (_disableGameObject) gameObject.SetActive(false);
                else _target.enabled = false;
                return;
            }
        }
    }
}
