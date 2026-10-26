using System;
using UnityEngine;

// Tutorial checkpoint plus the post-tutorial shop economy. v1 saves migrate on load.
public static class TutorialProgressSave
{
    private const string SaveKey = "MemoryMerchant.TutorialProgress.v1";
    [Serializable]
    public sealed class Record
    {
        public int version;
        public bool tutorialCompleted;
        public int memoryPoints;
        public int coins;
        public int echoCapacity = 1;
        public bool dualEchoReplay;
        public int[] items = new int[4];
    }
    public static bool HasValidSave => TryLoad(out _);
    public static bool TryLoad(out Record record)
    {
        record = null;
        if (!PlayerPrefs.HasKey(SaveKey)) return false;
        try
        {
            Record loaded = JsonUtility.FromJson<Record>(PlayerPrefs.GetString(SaveKey));
            if (loaded == null || (loaded.version != 1 && loaded.version != 2) || !loaded.tutorialCompleted ||
                loaded.memoryPoints < 0 || loaded.coins < 0) return false;
            if (loaded.version == 1)
            {
                loaded.echoCapacity = 1;
                loaded.dualEchoReplay = false;
                loaded.items = new int[4];
            }
            if (loaded.echoCapacity < 1 || loaded.echoCapacity > 3 ||
                loaded.items == null || loaded.items.Length != 4 ||
                (loaded.dualEchoReplay && loaded.echoCapacity < 2)) return false;
            foreach (int quantity in loaded.items)
                if (quantity < 0 || quantity > ShopCatalogue.ItemLimit) return false;
            loaded.version = 2;
            record = loaded;
            return true;
        }
        catch (ArgumentException) { return false; }
    }
    public static void SaveCompletedTutorial(int memoryPoints, int coins)
    {
        // Preserve purchases if an existing checkpoint's currency is updated.
        if (!TryLoad(out Record record)) record = new Record();
        record.memoryPoints = Mathf.Max(0, memoryPoints);
        record.coins = Mathf.Max(0, coins);
        Save(record);
    }
    public static void Save(Record record)
    {
        record.version = 2;
        record.tutorialCompleted = true;
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(record));
        PlayerPrefs.Save();
    }
    public static void DeleteCheckpoint()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }
}
