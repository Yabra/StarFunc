using UnityEngine;

namespace StarFunc.Meta
{
    public class TimerService : ITimerService
    {
        float _startTime;
        float _accumulatedTime;
        bool _running;
        bool _paused;

        public void StartTimer()
        {
            _startTime = Time.realtimeSinceStartup;
            _accumulatedTime = 0f;
            _running = true;
            _paused = false;
        }

        public void StopTimer()
        {
            if (_running && !_paused)
                _accumulatedTime += Time.realtimeSinceStartup - _startTime;

            _running = false;
            _paused = false;
        }

        public void PauseTimer()
        {
            if (!_running || _paused) return;

            _accumulatedTime += Time.realtimeSinceStartup - _startTime;
            _paused = true;
        }

        public void ResumeTimer()
        {
            if (!_running || !_paused) return;

            _startTime = Time.realtimeSinceStartup;
            _paused = false;
        }

        public float GetElapsedTime()
        {
            if (!_running) return _accumulatedTime;
            if (_paused) return _accumulatedTime;

            return _accumulatedTime + (Time.realtimeSinceStartup - _startTime);
        }
    }
}
