// Small, engine-independent gate for the tutorial key/cabinet sequence.
public sealed class KeyEchoSequence
{
    public enum Phase { Idle, Playing, ReadyToCapture, Captured }
    public Phase Current { get; private set; }
    public bool Begin()
    {
        if (Current != Phase.Idle) return false;
        Current = Phase.Playing;
        return true;
    }
    public void FinishPreview() { if (Current == Phase.Playing) Current = Phase.ReadyToCapture; }
    public bool Capture()
    {
        if (Current != Phase.ReadyToCapture) return false;
        Current = Phase.Captured;
        return true;
    }
    public void Reset() => Current = Phase.Idle;
    public static bool CanUnlock(bool storedKey, bool nearCabinet, bool uiBlocked, bool alreadyOpen) =>
        storedKey && nearCabinet && !uiBlocked && !alreadyOpen;
}

public sealed class CabinetEchoReplay
{
    public bool Playing { get; private set; }
    private bool complete;
    public bool Begin(bool storedKey, bool nearCabinet, bool uiBlocked, bool alreadyOpen)
    {
        if (Playing || complete || !KeyEchoSequence.CanUnlock(storedKey, nearCabinet, uiBlocked, alreadyOpen))
            return false;
        Playing = true;
        return true;
    }
    public bool Finish()
    {
        if (!Playing) return false;
        Playing = false;
        complete = true;
        return true;
    }
    public void Reset() { Playing = false; complete = false; }
}
