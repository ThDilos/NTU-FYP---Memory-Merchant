using System.ComponentModel;
using UnityEngine;

// Click Echo once, show animation
// Animation waits for player to press again to collect
// After collect, add into LevelInventory.
public class MemoryEcho : Collectible
{
    [Header("Memory Echo Settings")]
    [Description("A series of animation packed into a Prefab, to be played whenever the echo is used.")]
    [SerializeField] private GameObject echoReplay;
    public override void OnCollect()
    {
        
    }

    public void TriggerEchoFunction()
    {
        
    }
}