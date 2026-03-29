using UnityEngine;

namespace StarFunc.Gameplay
{
    /// <summary>
    /// Autonomous level timer. Tracks elapsed play-time with pause support.
    /// Uses Time.realtimeSinceStartup to survive timeScale = 0.
    /// Later phases will delegate to TimerService.
    /// Plain C# class — not a MonoBehaviour.
    /// </summary>
    public class LevelTimer
    {
        float _startTime;
        float _pausedElapsed;
        bool _isRunning;
        bool _isPaused;

        public bool IsRunning => _isRunning;
        public bool IsPaused => _isPaused;

        /// <summary>Start (or restart) the timer from zero.</summary>
        public void Start()
        {
            _startTime = Time.realtimeSinceStartup;
            _pausedElapsed = 0f;
            _isRunning = true;
            _isPaused = false;
        }

        /// <summary>Pause the timer, preserving elapsed time.</summary>
        public void Pause()
        {
            if (!_isRunning || _isPaused) return;

            _pausedElapsed = GetElapsedTime();
            _isPaused = true;
        }

        /// <summary>Resume a paused timer.</summary>
        public void Resume()
        {
            if (!_isRunning || !_isPaused) return;

            _startTime = Time.realtimeSinceStartup - _pausedElapsed;
            _isPaused = false;
        }

        /// <summary>Stop the timer. GetElapsedTime() will return the final value.</summary>
        public void Stop()
        {
            if (!_isRunning) return;

            _pausedElapsed = GetElapsedTime();
            _isRunning = false;
            _isPaused = false;
        }

        /// <summary>Elapsed seconds since Start(), accounting for pauses.</summary>
        public float GetElapsedTime()
        {
            if (!_isRunning) return _pausedElapsed;
            if (_isPaused) return _pausedElapsed;

            return Time.realtimeSinceStartup - _startTime;
        }
    }
}
