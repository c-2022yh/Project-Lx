internal sealed class AwakeningReadyAudioState
{
    private bool initialized, wasReady;

    public void Reset() { initialized = wasReady = false; }

    public bool Observe(bool ready, bool allowSound)
    {
        bool play = initialized && allowSound && ready && !wasReady;
        initialized = true;
        wasReady = ready;
        return play;
    }
}
