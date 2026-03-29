using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarFunc.Core
{
    public class GameEvent<T> : ScriptableObject
    {
        readonly List<GameEventListener<T>> _listeners = new();
        event Action<T> _onRaised;

        public void Raise(T value)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i].OnEventRaised(value);

            _onRaised?.Invoke(value);
        }

        public void RegisterListener(GameEventListener<T> listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void UnregisterListener(GameEventListener<T> listener)
        {
            _listeners.Remove(listener);
        }

        public void AddListener(Action<T> callback) => _onRaised += callback;
        public void RemoveListener(Action<T> callback) => _onRaised -= callback;
    }
}
