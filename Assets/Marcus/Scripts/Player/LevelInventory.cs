using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class LevelInventory : MonoBehaviour
{
    [Description("This inventory only exists in a level")]
    [SerializeField] private int echoSlotCount = 1;

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
        this.echoSlotCount = echoSlotCount;
    }

    public void Collect(Collectible collectible)
    {
        if (collectible is MemoryEcho)
        {
            if (memoryEchoes.Count >= echoSlotCount)
            {
                // At this stage, remove the first echo. Later can introduce replace function
                memoryEchoes.RemoveAt(0);
            }
            memoryEchoes.Add((MemoryEcho)collectible);
        }
        else if (collectible is MemoryFragment)
        {
            memoryFragments.Add((MemoryFragment)collectible);
        }

        collectible.OnCollect();
    }
}