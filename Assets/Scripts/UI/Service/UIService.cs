using System;
using System.Collections.Generic;
using System.Linq;
using StarFunc.Core;
using StarFunc.Data;
using UnityEngine;

namespace StarFunc.UI
{
    public class UIService : MonoBehaviour, IUIService
    {
        [SerializeField] Transform _screenContainer;
        [SerializeField] Transform _popupContainer;

        readonly Dictionary<Type, UIScreen> _screens = new();
        readonly List<UIPopup> _activePopups = new();
        readonly Stack<UIScreen> _screenStack = new();

        void Awake()
        {
            CollectScreens();
            CollectPopups();
            ServiceLocator.Register<IUIService>(this);
        }

        void CollectScreens()
        {
            var root = _screenContainer ? _screenContainer : transform;
            foreach (var screen in root.GetComponentsInChildren<UIScreen>(true))
            {
                var type = screen.GetType();
                if (_screens.ContainsKey(type))
                {
                    Debug.LogWarning($"[UIService] Duplicate screen type: {type.Name}");
                    continue;
                }

                _screens[type] = screen;
                screen.Hide();
            }
        }

        void CollectPopups()
        {
            var root = _popupContainer ? _popupContainer : transform;
            foreach (var popup in root.GetComponentsInChildren<UIPopup>(true))
                popup.Hide();
        }

        public void ShowScreen<T>() where T : UIScreen
        {
            var screen = GetScreen<T>();
            if (screen == null) return;

            // Hide current top screen
            if (_screenStack.Count > 0)
            {
                var current = _screenStack.Peek();
                if (current != screen)
                    current.Hide();
            }

            _screenStack.Push(screen);
            screen.Show();
        }

        public void HideScreen<T>() where T : UIScreen
        {
            var screen = GetScreen<T>();
            if (screen == null) return;

            screen.Hide();

            // Rebuild stack without the hidden screen
            var items = _screenStack.ToArray();
            _screenStack.Clear();
            for (int i = items.Length - 1; i >= 0; i--)
            {
                if (items[i] != screen)
                    _screenStack.Push(items[i]);
            }

            // Show the new top if any
            if (_screenStack.Count > 0)
                _screenStack.Peek().Show();
        }

        public T GetScreen<T>() where T : UIScreen
        {
            if (_screens.TryGetValue(typeof(T), out var screen))
                return (T)screen;

            Debug.LogWarning($"[UIService] Screen not found: {typeof(T).Name}");
            return null;
        }

        public void ShowPopup<T>(PopupData data) where T : UIPopup
        {
            var root = _popupContainer ? _popupContainer : transform;
            var popup = root.GetComponentInChildren<T>(true);

            if (popup == null)
            {
                Debug.LogWarning($"[UIService] Popup not found: {typeof(T).Name}");
                return;
            }

            popup.Show(data);
            _activePopups.Add(popup);
        }

        public void HideAllPopups()
        {
            foreach (var popup in _activePopups)
            {
                if (popup && popup.IsVisible)
                    popup.Hide();
            }

            _activePopups.Clear();
        }
    }
}
