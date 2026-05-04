using System;
using System.Collections.Generic;
using StarFunc.Data;
using UnityEngine;

namespace StarFunc.Gameplay
{
    /// <summary>
    /// Button row for picking the function type in BuildFunction mode.
    /// Hidden when only one type is allowed (or none configured).
    /// </summary>
    public class TypeSelector : MonoBehaviour
    {
        [SerializeField] RectTransform _container;
        [SerializeField] TypeSelectorButton _buttonPrefab;

        readonly List<TypeSelectorButton> _buttons = new();
        FunctionType _current;

        public event Action<FunctionType> OnTypeChanged;
        public FunctionType Current => _current;

        /// <summary>
        /// Build the button row. Returns the type that ended up selected
        /// (callers seed the editor with this).
        /// </summary>
        public FunctionType Setup(IReadOnlyList<FunctionType> allowed, FunctionType initial)
        {
            ClearButtons();
            if (allowed == null || allowed.Count == 0) return initial;

            _current = Contains(allowed, initial) ? initial : allowed[0];

            foreach (var type in allowed)
            {
                var btn = Instantiate(_buttonPrefab, _container);
                btn.gameObject.SetActive(true);
                btn.Initialize(type, type == _current);
                btn.OnClicked += HandleClicked;
                _buttons.Add(btn);
            }

            return _current;
        }

        void HandleClicked(TypeSelectorButton clicked)
        {
            if (clicked == null || clicked.Type == _current) return;

            _current = clicked.Type;
            foreach (var btn in _buttons)
                if (btn) btn.SetSelected(btn == clicked);

            OnTypeChanged?.Invoke(_current);
        }

        void ClearButtons()
        {
            foreach (var btn in _buttons)
            {
                if (!btn) continue;
                btn.OnClicked -= HandleClicked;
                Destroy(btn.gameObject);
            }
            _buttons.Clear();
        }

        static bool Contains(IReadOnlyList<FunctionType> list, FunctionType type)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] == type) return true;
            return false;
        }
    }
}
