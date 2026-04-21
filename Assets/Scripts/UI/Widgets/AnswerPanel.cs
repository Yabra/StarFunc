using System.Collections.Generic;
using StarFunc.Data;
using StarFunc.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    public class AnswerPanel : MonoBehaviour
    {
        [SerializeField] Transform _buttonContainer;
        [SerializeField] Button _answerButtonPrefab;

        [Header("Formula Style")]
        [SerializeField] float _formulaFontSize = 28f;
        [SerializeField] FontStyles _formulaFontStyle = FontStyles.Italic;

        void Awake()
        {
            if (_buttonContainer && !_buttonContainer.GetComponent<LayoutGroup>())
            {
                var layout = _buttonContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 12f;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
            }
        }

        [Header("Colors")]
        [SerializeField] Color _normalColor = Color.white;
        [SerializeField] Color _selectedColor = new(1f, 0.6f, 0.2f, 1f);

        AnswerSystem _answerSystem;
        readonly List<Button> _spawnedButtons = new();
        int _selectedIndex = -1;

        public void Initialize(AnswerSystem answerSystem)
        {
            _answerSystem = answerSystem;
            _answerSystem.OnOptionsChanged += OnOptionsChanged;
        }

        void OnDestroy()
        {
            if (_answerSystem != null)
                _answerSystem.OnOptionsChanged -= OnOptionsChanged;
        }

        void OnOptionsChanged(AnswerOption[] options, TaskType taskType)
        {
            ClearButtons();
            _selectedIndex = -1;

            bool isChooseFunction = taskType == TaskType.ChooseFunction;

            for (int i = 0; i < options.Length; i++)
            {
                var button = Instantiate(_answerButtonPrefab, _buttonContainer);
                var label = button.GetComponentInChildren<TMP_Text>();
                if (label)
                {
                    if (isChooseFunction)
                    {
                        label.text = !string.IsNullOrEmpty(options[i].Text)
                            ? options[i].Text
                            : FormatFunctionFormula(options[i].Function);
                        label.fontSize = _formulaFontSize;
                        label.fontStyle = _formulaFontStyle;
                    }
                    else
                    {
                        label.text = options[i].Text;
                    }
                }

                int index = i;
                button.onClick.AddListener(() => OnButtonClicked(index));
                _spawnedButtons.Add(button);
            }
        }

        static string FormatFunctionFormula(FunctionDefinition function)
        {
            if (!function || function.Coefficients == null)
                return "???";

            if (function.Type != FunctionType.Linear)
                return function.name;

            float a = function.Coefficients.Length > 0 ? function.Coefficients[0] : 0f;
            float b = function.Coefficients.Length > 1 ? function.Coefficients[1] : 0f;

            string slope = FormatSlope(a);
            if (Mathf.Approximately(b, 0f))
                return $"y = {slope}";

            string sign = b > 0 ? "+" : "−";
            return $"y = {slope} {sign} {Mathf.Abs(b)}";
        }

        static string FormatSlope(float a)
        {
            if (Mathf.Approximately(a, 1f)) return "x";
            if (Mathf.Approximately(a, -1f)) return "-x";
            return $"{a}x";
        }

        void OnButtonClicked(int index)
        {
            if (_answerSystem == null) return;

            _selectedIndex = index;
            _answerSystem.SelectOption(index);
            UpdateButtonVisuals();
        }

        void UpdateButtonVisuals()
        {
            for (int i = 0; i < _spawnedButtons.Count; i++)
            {
                var colors = _spawnedButtons[i].colors;
                colors.normalColor = i == _selectedIndex ? _selectedColor : _normalColor;
                colors.selectedColor = i == _selectedIndex ? _selectedColor : _normalColor;
                _spawnedButtons[i].colors = colors;
            }
        }

        void ClearButtons()
        {
            foreach (var button in _spawnedButtons)
            {
                if (button)
                    Destroy(button.gameObject);
            }

            _spawnedButtons.Clear();
        }
    }
}
