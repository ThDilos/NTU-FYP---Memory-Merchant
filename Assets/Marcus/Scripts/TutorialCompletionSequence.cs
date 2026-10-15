// Engine-independent gates: entry, final Yarn command, actual dialogue completion, Continue.
public sealed class TutorialCompletionSequence
{
    public enum Phase { WaitingForStudy, MotherDialogue, CompletionRequested, Results, Returning }
    public Phase Current { get; private set; }
    public bool TryBegin(bool studyRestored)
    {
        if (!studyRestored || Current != Phase.WaitingForStudy) return false;
        Current = Phase.MotherDialogue;
        return true;
    }
    public bool RequestCompletion()
    {
        if (Current != Phase.MotherDialogue) return false;
        Current = Phase.CompletionRequested;
        return true;
    }
    public bool FinishDialogue()
    {
        if (Current != Phase.CompletionRequested) return false;
        Current = Phase.Results;
        return true;
    }
    public bool TryContinue()
    {
        if (Current != Phase.Results) return false;
        Current = Phase.Returning;
        return true;
    }
    public void AbortDialogue()
    {
        if (Current == Phase.MotherDialogue || Current == Phase.CompletionRequested)
            Current = Phase.WaitingForStudy;
    }
}
