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

        [Tooltip("Screen to show after Awake hides every collected screen. " +
                 "Wire this in each scene (e.g. HubScreen for Hub.unity) so the " +
                 "scene comes up with something visible. Leave null to start blank.")]
        [SerializeField] UIScreen _defaultScreen;

        readonly Dictionary<Type, UIScreen> _screens = new();
        readonly Dictionary<Type, UIPopup> _popups = new();
        readonly List<UIPopup> _activePopups = new();
        readonly Stack<UIScreen> _screenStack = new();

        void Awake()
        {
            CollectScreens();
            CollectPopups();
            ServiceLocator.Register<IUIService>(this);
        }

        void Start()
        {
            // CollectScreens hid every screen — bring the designer-chosen
            // default back up so the scene doesn't start blank. Without this,
            // boot loads Hub but HubScreen stays inactive (it was hidden in
            // Awake and nothing ever calls ShowScreen for it).
            if (_defaultScreen != null)
                ShowScreenImmediate(_defaultScreen);
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
            {
                var type = popup.GetType();
                if (!_popups.ContainsKey(type)) _popups[type] = popup;
                popup.Hide();
            }
        }

        public void ShowScreen<T>() where T : UIScreen
        {
            var screen = GetScreen<T>();
            if (screen == null) return;

            var transition = ServiceLocator.Contains<ITransitionOverlay>()
                ? ServiceLocator.Get<ITransitionOverlay>()
                : null;

            if (transition == null)
            {
                ShowScreenImmediate(screen);
                return;
            }

            // Cover → swap → reveal.
            transition.TransitionIn(() =>
            {
                ShowScreenImmediate(screen);
                transition.TransitionOut(null);
            });
        }

        void ShowScreenImmediate(UIScreen screen)
        {
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
            var popup = GetPopup<T>();
            if (popup == null) return;
            popup.Show(data);
            _activePopups.Add(popup);
        }

        public T GetPopup<T>() where T : UIPopup
        {
            if (_popups.TryGetValue(typeof(T), out var popup))
                return (T)popup;

            // Fallback to a dynamic search in case the popup wasn't present
            // when CollectPopups ran (e.g. spawned at runtime).
            var root = _popupContainer ? _popupContainer : transform;
            var found = root.GetComponentInChildren<T>(true);
            if (found != null) _popups[typeof(T)] = found;
            else Debug.LogWarning($"[UIService] Popup not found: {typeof(T).Name}");
            return found;
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
