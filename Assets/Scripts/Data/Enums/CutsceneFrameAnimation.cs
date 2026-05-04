namespace StarFunc.Data
{
    /// <summary>
    /// Per-frame ambient motion played on the cutscene character image.
    /// Loops while the frame is on screen and is killed/reset between frames
    /// by <c>CutscenePopup</c>. Pick the simplest verb that matches the line.
    /// </summary>
    public enum CutsceneFrameAnimation
    {
        None,
        Pulse,     // gentle scale yoyo — calm idle
        Bounce,    // sharper scale yoyo — excitement
        Sway,      // slow horizontal drift — neutral
        Shake,     // quick horizontal jitter — surprise / alarm
        FloatUp,   // vertical bob — dreamy / curious
        Tilt,      // slow z-rotation yoyo — questioning
        Wiggle     // fast z-rotation yoyo — silly / nervous
    }
}
