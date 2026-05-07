namespace StarFunc.Data
{
    public enum HintTrigger
    {
        OnLevelStart,
        AfterErrors,
        OnFirstInteraction,
        /// <summary>
        /// Fires when the player selects an answer option (taps an answer
        /// button, sets a function coefficient, etc.) for the first time —
        /// before they confirm with the Done button.
        /// </summary>
        OnAnswerSelected,
        /// <summary>
        /// Never fires automatically. Reserved for paid hints surfaced via
        /// <c>HintSystem.UseHint</c> — listed in <c>LevelData.Hints[]</c> so
        /// the player can spend an inventory hint to reveal them.
        /// </summary>
        Manual
    }
}
