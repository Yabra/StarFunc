using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarFunc.UI
{
    public class FragmentsDisplay : MonoBehaviour
    {
        [SerializeField] TMP_Text _fragmentsText;
        [Tooltip("Optional fragment/diamond icon Image — designer-assigned " +
                 "sprite, sits next to the count.")]
        [SerializeField] Image _icon;
        [Tooltip("string.Format pattern for the count. Default is '{0}'; the " +
                 "old default '◆ {0}' baked the diamond glyph into the text " +
                 "and most project fonts don't ship it.")]
        [SerializeField] string _format = "{0}";

        public void SetFragments(int count)
        {
            if (_fragmentsText)
                _fragmentsText.text = string.Format(_format, count);
            _ = _icon; // reserved for future tinting / pulse on increase
        }
    }
}
