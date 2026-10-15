using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class LevelInventory : MonoBehaviour
{
    [Description("This inventory only exists in a level")]
    [SerializeField] private int echoSlotCount = 1;
    public int EchoSlotCount => Mathf.Clamp(echoSlotCount, 1, 3);
    public event Action EchoCapacityChanged;

    // Runtime vars
    public List<MemoryEcho> memoryEchoes = new List<MemoryEcho>(); 
    public List<MemoryFragment> memoryFragments = new List<MemoryFragment>();

    public static LevelInventory Instance { get; private set; }
    void Awake()
    {
        Instance = this;
    }

    public void Initialize(int echoSlotCount)
    {
        int capacity = Mathf.Clamp(echoSlotCount, 1, 3);
        if (this.echoSlotCount == capacity) return;
        this.echoSlotCount = capacity;
        EchoCapacityChanged?.Invoke();
    }

    // Call this from the machine's successful upgrade event when upgrades are added.
    public void UnlockNextEchoSlot() => Initialize(EchoSlotCount + 1);

    public void Collect(Collectible collectible)
    {
        if (collectible == null) return;
        if (collectible is MemoryEcho echo)
        {
            if (memoryEchoes.Contains(echo)) return;
            if (memoryEchoes.Count >= EchoSlotCount)
            {
                // At this stage, remove the first echo. Later can introduce replace function
                memoryEchoes.RemoveAt(0);
            }
            memoryEchoes.Add(echo);
        }
        else if (collectible is MemoryFragment)
        {
            if (memoryFragments.Contains((MemoryFragment)collectible)) return;
            memoryFragments.Add((MemoryFragment)collectible);
        }

        collectible.OnCollect();
    }
}
