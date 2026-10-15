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
    public bool IsDiscovered { get; private set; }
    public bool IsStored => LevelInventory.Instance != null && LevelInventory.Instance.memoryEchoes.Contains(this);
    public void ResetDiscovery() => IsDiscovered = false;
    public void MarkDiscovered() => IsDiscovered = true;
    public override void OnCollect()
    {
        IsDiscovered = true;
    }

    public void TriggerEchoFunction()
    {
        
    }
}
