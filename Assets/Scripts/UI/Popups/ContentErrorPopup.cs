using StarFunc.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    /// <summary>
    /// Alpha-test placeholder shown when the player taps a sector that hasn't
    /// shipped yet. Displays a localised "content unavailable" message with the
    /// app version (Player Settings → Bundle Version) and a single Close
    /// button. The widget self-builds its UI hierarchy on first Awake so the
    /// scene only needs an empty GameObject + this component.
    /// </summary>
    public class ContentErrorPopup : UIPopup
    {
        [Header("Wired (auto-built if left null)")]
        [SerializeField] TMP_Text _messageText;
        [SerializeField] Button _closeButton;
        [SerializeField] TMP_Text _closeButtonLabel;

        [Header("Copy")]
        [TextArea(2, 6)]
        [SerializeField] string _messageTemplate =
            "Извините, этот контент пока недоступен.\nВерсия {0} не поддерживается.";
        [SerializeField] string _closeLabel = "Закрыть";

        void Awake()
        {
            EnsureBuilt();

            if (_closeButton) _closeButton.onClick.AddListener(Hide);
        }

        void OnDestroy()
        {
            if (_closeButton) _closeButton.onClick.RemoveListener(Hide);
        }

        public override void Show(PopupData data)
        {
            base.Show(data);

            if (_messageText)
                _messageText.text = string.Format(_messageTemplate, Application.version);
            if (_closeButtonLabel)
                _closeButtonLabel.text = _closeLabel;
        }

        // ----- Self-construction ---------------------------------------------------
        // Build a minimal popup hierarchy at runtime so the Hub scene only
        // needs an empty GameObject + this component. If an artist later wires
        // the SerializeFields manually (or hands us a designed prefab), the
        // null-checks below skip the auto-build and reuse what's there.

        void EnsureBuilt()
        {
            var rt = transform as RectTransform;
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();
            StretchToParent(rt);

            // CanvasGroup on root for UIPopup base to drive alpha / interactable.
            var cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            BindCanvasGroup(cg);

            // Dim background — full-canvas semi-transparent black, swallows taps.
            var dim = transform.Find("DimBackground");
            if (dim == null) dim = BuildDim(rt).transform;
            BindDim(dim.gameObject);

            // Centred panel.
            var panel = transform.Find("Panel") as RectTransform;
            if (panel == null) panel = BuildPanel(rt);

            // Message text inside the panel.
            if (_messageText == null)
            {
                var existing = panel.Find("Message")?.GetComponent<TMP_Text>();
                _messageText = existing != null ? existing : BuildMessage(panel);
            }

            // Close button + label.
            if (_closeButton == null)
            {
                var existing = panel.Find("CloseButton")?.GetComponent<Button>();
                _closeButton = existing != null ? existing : BuildCloseButton(panel, out _closeButtonLabel);
            }
            else if (_closeButtonLabel == null)
            {
                _closeButtonLabel = _closeButton.GetComponentInChildren<TMP_Text>();
            }
        }

        static void StretchToParent(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        void BindCanvasGroup(CanvasGroup cg)
        {
            // UIPopup base reads _canvasGroup via SerializeField. Use reflection-
            // free shadow-set by writing through the field directly — no other
            // instance owns the reference, so this is safe.
            var f = typeof(UIPopup).GetField("_canvasGroup",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (f != null && f.GetValue(this) == null) f.SetValue(this, cg);
        }

        void BindDim(GameObject dim)
        {
            var f = typeof(UIPopup).GetField("_dimBackground",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (f != null && f.GetValue(this) == null) f.SetValue(this, dim);
        }

        static GameObject BuildDim(RectTransform parent)
        {
            var go = new GameObject("DimBackground", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, worldPositionStays: false);
            StretchToParent(rt);
            var img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.7f);
            img.raycastTarget = true;
            return go;
        }

        static RectTransform BuildPanel(RectTransform parent)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, worldPositionStays: false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(820f, 0f);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.07f, 0.09f, 0.15f, 0.97f);
            img.raycastTarget = true;

            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 48, 48);
            layout.spacing = 32f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = go.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return rt;
        }

        static TMP_Text BuildMessage(RectTransform panel)
        {
            var go = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(panel, worldPositionStays: false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 36f;
            text.color = new Color(0.95f, 0.95f, 0.97f, 1f);
            text.text = "Content unavailable";
            return text;
        }

        static Button BuildCloseButton(RectTransform panel, out TMP_Text label)
        {
            var go = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            var rt = (RectTransform)go.transform;
            rt.SetParent(panel, worldPositionStays: false);
            rt.sizeDelta = new Vector2(0f, 100f);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.95f, 0.55f, 0.3f, 1f);
            img.raycastTarget = true;

            var layoutEl = go.GetComponent<LayoutElement>();
            layoutEl.preferredHeight = 100f;
            layoutEl.flexibleWidth = 1f;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(rt, worldPositionStays: false);
            var labelRt = (RectTransform)labelGo.transform;
            StretchToParent(labelRt);
            label = labelGo.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 32f;
            label.color = Color.white;
            label.text = "Close";

            var button = go.GetComponent<Button>();
            button.targetGraphic = img;

            return button;
        }
    }
}
