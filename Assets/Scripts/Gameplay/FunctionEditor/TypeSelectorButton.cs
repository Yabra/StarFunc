using System;
using StarFunc.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.Gameplay
{
    /// <summary>
    /// One button inside <see cref="TypeSelector"/>. Bound to a single
    /// <see cref="FunctionType"/>; toggles its visual state when selected.
    /// </summary>
    public class TypeSelectorButton : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] TMP_Text _label;
        [SerializeField] Image _background;
        [SerializeField] Color _selectedColor = new(0.95f, 0.55f, 0.30f, 1f);
        [SerializeField] Color _unselectedColor = new(0.25f, 0.25f, 0.30f, 1f);

        public event Action<TypeSelectorButton> OnClicked;
        public FunctionType Type { get; private set; }

        void Awake()
        {
            if (_button) _button.onClick.AddListener(HandleClicked);
        }

        void OnDestroy()
        {
            if (_button) _button.onClick.RemoveListener(HandleClicked);
        }

        public void Initialize(FunctionType type, bool selected)
        {
            Type = type;
            if (_label) _label.text = LabelFor(type);
            SetSelected(selected);
        }

        public void SetSelected(bool selected)
        {
            if (_background) _background.color = selected ? _selectedColor : _unselectedColor;
        }

        void HandleClicked() => OnClicked?.Invoke(this);

        static string LabelFor(FunctionType type) => type switch
        {
            FunctionType.Linear => "Линейная",
            FunctionType.Quadratic => "Квадратичная",
            FunctionType.Sinusoidal => "Синусоида",
            FunctionType.Mixed => "Смешанная",
            _ => type.ToString()
        };
    }
}
