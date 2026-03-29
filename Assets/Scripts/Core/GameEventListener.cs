using UnityEngine;
using UnityEngine.Events;

namespace StarFunc.Core
{
    public class GameEventListener : MonoBehaviour
    {
        [SerializeField] GameEvent _event;
        [SerializeField] UnityEvent _response;

        void OnEnable()
        {
            if (_event != null)
                _event.RegisterListener(this);
        }

        void OnDisable()
        {
            if (_event != null)
                _event.UnregisterListener(this);
        }

        public void OnEventRaised()
        {
            _response?.Invoke();
        }
    }
}
