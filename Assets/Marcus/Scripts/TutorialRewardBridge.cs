using UnityEngine;

// Active tutorial progress; the checkpoint is saved separately by TutorialProgressSave.
public static class TutorialRewardBridge
{
    public const int TutorialMemoryPoints = 3;
    public const int TutorialCoins = 0;
    public static int MemoryPoints { get; private set; }
    public static int Coins { get; private set; }
    public static bool TutorialCompleted { get; private set; }
    public static bool IsArchiveReplay { get; private set; }
    private static bool returnToArchives;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        MemoryPoints = 0; Coins = 0; TutorialCompleted = false;
        IsArchiveReplay = false; returnToArchives = false;
    }
    public static void BeginNewGame()
    {
        ResetSession();
        TutorialProgressSave.DeleteCheckpoint();
    }
    public static bool ResumeSavedGame()
    {
        if (!TutorialProgressSave.TryLoad(out TutorialProgressSave.Record record)) return false;
        TutorialCompleted = record.tutorialCompleted;
        MemoryPoints = record.memoryPoints;
        Coins = record.coins;
        IsArchiveReplay = false; returnToArchives = false;
        return true;
    }
    public static bool BeginArchiveReplay()
    {
        if (!ResumeSavedGame()) return false;
        IsArchiveReplay = true;
        return true;
    }
    public static void FinishArchiveReplay()
    {
        if (!IsArchiveReplay) return;
        IsArchiveReplay = false;
        returnToArchives = true;
    }
    public static bool ConsumeArchiveReturn()
    {
        bool pending = returnToArchives;
        returnToArchives = false;
        return pending;
    }
    public static bool AwardTutorialOnce()
    {
        if (IsArchiveReplay || TutorialCompleted) return false;
        TutorialCompleted = true;
        MemoryPoints += TutorialMemoryPoints;
        Coins += TutorialCoins;
        TutorialProgressSave.SaveCompletedTutorial(MemoryPoints, Coins);
        return true;
    }
}
