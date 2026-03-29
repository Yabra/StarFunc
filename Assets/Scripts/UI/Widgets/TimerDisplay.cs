using StarFunc.Gameplay;
using TMPro;
using UnityEngine;

namespace StarFunc.UI
{
    public class TimerDisplay : MonoBehaviour
    {
        [SerializeField] TMP_Text _timerText;

        LevelController _controller;

        public void Initialize(LevelController controller)
        {
            _controller = controller;
        }

        void Update()
        {
            var timer = _controller != null ? _controller.Timer : null;
            if (timer == null || !timer.IsRunning)
                return;

            float elapsed = timer.GetElapsedTime();
            int minutes = (int)(elapsed / 60f);
            int seconds = (int)(elapsed % 60f);
            _timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }
}
