namespace StarFunc.Core
{
    public interface ILoadingOverlay
    {
        /// <summary>Show immediately.</summary>
        void Show();

        /// <summary>Hide immediately. Cancels a pending <see cref="ShowDelayed"/> if present.</summary>
        void Hide();

        /// <summary>
        /// Show only if <see cref="Hide"/> isn't called within <paramref name="thresholdSeconds"/>.
        /// Use this for "safety screen" behavior on additive scene loads — fast loads get no
        /// flash, slow loads get the spinner.
        /// </summary>
        void ShowDelayed(float thresholdSeconds);

        /// <summary>Update the progress bar / readout. <paramref name="progress"/> is clamped to [0..1].</summary>
        void SetProgress(float progress);
    }
}
