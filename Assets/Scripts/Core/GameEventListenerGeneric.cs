using UnityEngine;
using UnityEngine.Events;

namespace StarFunc.Core
{
    public class GameEventListener<T> : MonoBehaviour
    {
        [SerializeField] GameEvent<T> _event;
        [SerializeField] UnityEvent<T> _response;

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

        public void OnEventRaised(T value)
        {
            _response?.Invoke(value);
        }
    }
}
