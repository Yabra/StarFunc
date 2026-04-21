using StarFunc.Data;

namespace StarFunc.Meta
{
    public interface IFeedbackService
    {
        void PlayFeedback(FeedbackType type);
        void SetHapticsEnabled(bool enabled);
    }
}
