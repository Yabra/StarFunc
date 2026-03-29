using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarFunc.Core
{
    [CreateAssetMenu(menuName = "StarFunc/Events/GameEvent", fileName = "NewGameEvent")]
    public class GameEvent : ScriptableObject
    {
        readonly List<GameEventListener> _listeners = new();
        event Action OnRaised;

        public void Raise()
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i].OnEventRaised();

            OnRaised?.Invoke();
        }

        public void RegisterListener(GameEventListener listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void UnregisterListener(GameEventListener listener)
        {
            _listeners.Remove(listener);
        }

        public void AddListener(Action callback) => OnRaised += callback;
        public void RemoveListener(Action callback) => OnRaised -= callback;
    }
}
