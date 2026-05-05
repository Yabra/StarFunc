using System;
using System.Collections.Generic;
using System.Linq;
using StarFunc.Core;
using StarFunc.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            // Each scene ships its own UIService — Hub's stays alive while
            // Level loads additively, so Level's Awake would otherwise throw
            // "already registered". Replace the existing registration so the
            // most-recently-loaded scene owns the slot; we restore the prior
            // owner on scene-unload (see OnSceneUnloaded below).
            AcquireRegistration();
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            ServiceLocator.Unregister<IUIService>(this);
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

        void AcquireRegistration()
        {
            if (ServiceLocator.Contains<IUIService>())
            {
                // Take over from whoever's currently holding the slot.
                var current = ServiceLocator.Get<IUIService>();
                ServiceLocator.Unregister<IUIService>(current);
            }
            ServiceLocator.Register<IUIService>(this);
        }

        void OnSceneUnloaded(Scene scene)
        {
            // If our registration was stolen by a now-unloaded scene's
            // UIService, reclaim the slot. The displaced (still-alive) Hub
            // service typically does this when Level unloads.
            if (this == null) return;
            if (!ServiceLocator.Contains<IUIService>())
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
            {
                var type = popup.GetType();
                if (!_popups.ContainsKey(type)) _popups[type] = popup;
                popup.Hide();
            }
        }

        public void ShowScreen<T>() where T : UIScreen => ShowScreen<T>(true);

        public void ShowScreen<T>(bool useTransition) where T : UIScreen
        {
            var screen = GetScreen<T>();
            if (screen == null) return;

            if (!useTransition)
            {
                ShowScreenImmediate(screen);
                return;
            }

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
