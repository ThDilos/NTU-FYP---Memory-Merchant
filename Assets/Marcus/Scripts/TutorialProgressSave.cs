using System;
using UnityEngine;

// One tutorial checkpoint. This can be replaced by a shared save service later.
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
    }
    public static bool HasValidSave => TryLoad(out _);
    public static bool TryLoad(out Record record)
    {
        record = null;
        if (!PlayerPrefs.HasKey(SaveKey)) return false;
        try
        {
            Record loaded = JsonUtility.FromJson<Record>(PlayerPrefs.GetString(SaveKey));
            if (loaded == null || loaded.version != 1 || !loaded.tutorialCompleted ||
                loaded.memoryPoints < 0 || loaded.coins < 0) return false;
            record = loaded;
            return true;
        }
        catch (ArgumentException) { return false; }
    }
    public static void SaveCompletedTutorial(int memoryPoints, int coins)
    {
        var record = new Record { version = 1, tutorialCompleted = true, memoryPoints = Mathf.Max(0, memoryPoints), coins = Mathf.Max(0, coins) };
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(record));
        PlayerPrefs.Save();
    }
    public static void DeleteCheckpoint()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }
}
