using System;
using System.Collections.Generic;

// Pure prototype logic: replace independently of the Manual and Marcus's visuals.
public sealed class SentenceReconstruction
{
    private readonly int[] targetOrder;
    private readonly int[] slots;
    public IReadOnlyList<int> Slots => slots;
    public bool Complete { get; private set; }
    public bool AllSlotsFilled => Array.TrueForAll(slots, value => value >= 0);
    public bool HasAnyPlaced => Array.Exists(slots, value => value >= 0);
    public SentenceReconstruction(int[] order)
    {
        targetOrder = (int[])order.Clone();
        slots = new int[order.Length];
        Clear();
    }
    private void Clear() { for (int i = 0; i < slots.Length; i++) slots[i] = -1; }
    public bool Place(int fragment, Func<int, bool> collected)
    {
        if (Array.IndexOf(slots, fragment) >= 0) return false;
        int empty = Array.IndexOf(slots, -1);
        return empty >= 0 && PlaceAt(fragment, empty, collected);
    }
    public bool PlaceAt(int fragment, int slot, Func<int, bool> collected)
    {
        if (Complete || fragment < 0 || slot < 0 || slot >= slots.Length || !collected(fragment)) return false;
        int previous = Array.IndexOf(slots, fragment);
        if (previous == slot) return false;
        int displaced = slots[slot];
        if (previous >= 0) slots[previous] = displaced;
        slots[slot] = fragment;
        return true;
    }
    public int SlotOf(int fragment) => Array.IndexOf(slots, fragment);
    public void ResetArrangement() { if (!Complete) Clear(); }
    public bool TryComplete()
    {
        if (Complete || !AllSlotsFilled) return false;
        for (int i = 0; i < targetOrder.Length; i++)
            if (slots[i] != targetOrder[i]) return false;
        Complete = true;
        return true;
    }
}
