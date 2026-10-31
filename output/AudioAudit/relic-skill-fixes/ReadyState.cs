internal sealed class AwakeningReadyAudioState
{
    private bool initialized, wasReady;

    // Full/Dark Moon still announce a full gauge even though awakening is blocked.
    public static bool IsEnergyReady(float current, float maximum, float required, bool awakeningBlocked)
    {
        return maximum > 0f && (awakeningBlocked
            ? current >= maximum
            : maximum >= required && current >= required);
    }

    public void Reset() { initialized = wasReady = false; }

    public bool Observe(bool ready, bool allowSound)
    {
        bool play = initialized && allowSound && ready && !wasReady;
        initialized = true;
        wasReady = ready;
        return play;
    }
}
