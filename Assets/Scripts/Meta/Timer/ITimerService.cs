namespace StarFunc.Meta
{
    public interface ITimerService
    {
        void StartTimer();
        void StopTimer();
        void PauseTimer();
        void ResumeTimer();
        float GetElapsedTime();
    }
}
