using TMPro;
using UnityEngine;

namespace StarFunc.UI
{
    public class FragmentsDisplay : MonoBehaviour
    {
        [SerializeField] TMP_Text _fragmentsText;
        [SerializeField] string _format = "◆ {0}";

        public void SetFragments(int count)
        {
            if (_fragmentsText)
                _fragmentsText.text = string.Format(_format, count);
        }
    }
}
