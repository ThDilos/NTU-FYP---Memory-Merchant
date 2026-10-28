using UnityEngine;
using System;

// Active tutorial progress; the checkpoint is saved separately by TutorialProgressSave.
public static class TutorialRewardBridge
{
    public const int TutorialMemoryPoints = 3;
    public const int TutorialCoins = 0;
    public static int MemoryPoints { get; private set; }
    public static int Coins { get; private set; }
    public static bool TutorialCompleted { get; private set; }
    public static bool IsArchiveReplay { get; private set; }
    public static bool MemoryWorldIntroSeen { get; private set; }
    public static int EchoCapacity { get; private set; } = 1;
    public static bool DualEchoReplayUnlocked { get; private set; }
    public static int PlayableEchoCapacity => TutorialCompleted && !IsArchiveReplay ? EchoCapacity : 1;
    // Owning Slot II completes the shop lesson, including after loading a save.
    public static bool NeedsFirstEchoUpgrade => TutorialCompleted && !IsArchiveReplay && EchoCapacity < 2;
    private static int[] items = new int[4];
    public static event Action Changed;
    public static void MarkMemoryWorldIntroSeen() => MemoryWorldIntroSeen = true;
    private static bool returnToArchives;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        MemoryPoints = 0; Coins = 0; TutorialCompleted = false;
        IsArchiveReplay = false; returnToArchives = false;
        MemoryWorldIntroSeen = false;
        EchoCapacity = 1; DualEchoReplayUnlocked = false; items = new int[4];
        activePuzzleRound = 0;
    }
    public static void BeginNewGame()
    {
        ResetSession();
        TutorialProgressSave.DeleteCheckpoint();
        Changed?.Invoke();
    }
    public static bool ResumeSavedGame()
    {
        if (!TutorialProgressSave.TryLoad(out TutorialProgressSave.Record record)) return false;
        TutorialCompleted = record.tutorialCompleted;
        MemoryWorldIntroSeen = record.tutorialCompleted;
        MemoryPoints = record.memoryPoints;
        Coins = record.coins;
        EchoCapacity = record.echoCapacity;
        DualEchoReplayUnlocked = record.dualEchoReplay;
        items = (int[])record.items.Clone();
        activePuzzleRound = 0;
        IsArchiveReplay = false; returnToArchives = false;
        Changed?.Invoke();
        return true;
    }
    public static bool BeginArchiveReplay()
    {
        if (!ResumeSavedGame()) return false;
        IsArchiveReplay = true;
        Changed?.Invoke();
        return true;
    }
    public static void FinishArchiveReplay()
    {
        if (!IsArchiveReplay) return;
        IsArchiveReplay = false;
        returnToArchives = true;
        Changed?.Invoke();
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
        SaveProgress();
        Changed?.Invoke();
        return true;
    }

    private static void SaveProgress()
    {
        if (!TutorialCompleted || IsArchiveReplay) return;
        TutorialProgressSave.Save(new TutorialProgressSave.Record
        {
            memoryPoints = MemoryPoints, coins = Coins, echoCapacity = EchoCapacity,
            dualEchoReplay = DualEchoReplayUnlocked, items = (int[])items.Clone()
        });
    }

    public static int OwnedQuantity(ShopProduct product)
    {
        if (!ShopCatalogue.IsValid(product)) return 0;
        switch (product)
        {
            case ShopProduct.EchoSlotII: return EchoCapacity >= 2 ? 1 : 0;
            case ShopProduct.EchoSlotIII: return EchoCapacity >= 3 ? 1 : 0;
            case ShopProduct.DualEchoReplay: return DualEchoReplayUnlocked ? 1 : 0;
            default: return items[(int)product - 3];
        }
    }

    public static bool CanPurchase(ShopProduct product, out string reason)
    {
        if (!ShopCatalogue.IsValid(product)) { reason = "Unknown item."; return false; }
        if (!TutorialCompleted || IsArchiveReplay) { reason = "Complete the tutorial first."; return false; }
        if (ShopCatalogue.IsUpgrade(product) && OwnedQuantity(product) > 0) { reason = "Already owned"; return false; }
        if ((product == ShopProduct.EchoSlotIII || product == ShopProduct.DualEchoReplay) && EchoCapacity < 2)
        { reason = "Requires Echo Slot II"; return false; }
        if (!ShopCatalogue.IsUpgrade(product) && OwnedQuantity(product) >= ShopCatalogue.ItemLimit)
        { reason = "Inventory full"; return false; }
        int available = ShopCatalogue.IsUpgrade(product) ? MemoryPoints : Coins;
        int missing = ShopCatalogue.Price(product) - available;
        if (missing > 0) { reason = "Need " + missing + " more " + ShopCatalogue.CurrencyName(product); return false; }
        reason = "";
        return true;
    }

    public static bool TryPurchase(ShopProduct product, out string message)
    {
        if (!CanPurchase(product, out message)) return false;
        if (ShopCatalogue.IsUpgrade(product)) MemoryPoints -= ShopCatalogue.Price(product);
        else Coins -= ShopCatalogue.Price(product);
        switch (product)
        {
            case ShopProduct.EchoSlotII: EchoCapacity = 2; break;
            case ShopProduct.EchoSlotIII: EchoCapacity = 3; break;
            case ShopProduct.DualEchoReplay: DualEchoReplayUnlocked = true; break;
            default: items[(int)product - 3]++; break;
        }
        SaveProgress();
        message = ShopCatalogue.IsUpgrade(product) ? "Upgrade purchased and saved." : "Item added to your inventory.";
        AudioManager.Instance?.PlayPurchase();
        Changed?.Invoke();
        return true;
    }

    // A round token is consumed before granting currency, so duplicate completion callbacks
    // cannot pay twice. Abandoning/reopening the puzzle starts a different round.
    private static long nextPuzzleRound;
    private static long activePuzzleRound;
    public static long BeginPuzzleRound()
    {
        activePuzzleRound = TutorialCompleted && !IsArchiveReplay ? ++nextPuzzleRound : 0;
        return activePuzzleRound;
    }
    public static void CancelPuzzleRound(long round)
    {
        if (round == activePuzzleRound) activePuzzleRound = 0;
    }
    public static bool CompletePuzzleRound(long round)
    {
        if (round == 0 || round != activePuzzleRound || !TutorialCompleted || IsArchiveReplay) return false;
        activePuzzleRound = 0;
        Coins = (int)Math.Min(int.MaxValue, (long)Coins + ShopCatalogue.PuzzleReward);
        AudioManager.Instance?.PlayPurchase();
        SaveProgress();
        Changed?.Invoke();
        return true;
    }
}
