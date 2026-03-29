using TMPro;
using UnityEngine;

namespace StarFunc.UI
{
    public class LivesDisplay : MonoBehaviour
    {
        [SerializeField] TMP_Text _livesText;

        public void SetLives(int count)
        {
            _livesText.text = $"\u2665 {count}";
        }
    }
}
