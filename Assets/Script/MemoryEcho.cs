using System.ComponentModel;
using UnityEngine;

// Click Echo once, show animation
// Animation waits for player to press again to collect
// After collect, add into LevelInventory.

// This script mainly controls what happens when the echo is activated.
public class MemoryEcho : Collectible
{
    [Header("Memory Echo Settings")]
    [Tooltip("Already existing object on scene. Set active when triggered. Should self set active false.")]
    [SerializeField] private GameObject echoReplay;

    public void TriggerEchoFunction()
    {
        if (echoReplay == null)
        {
            Debug.Log($"[MemoryEcho] Echo {displayName} is triggered, but nothing happened.");
            return;
        }

        echoReplay.SetActive(false);
        echoReplay.SetActive(true);
        Animation animation = echoReplay.GetComponent<Animation>();
        if (animation != null) animation.Rewind(); 
    }
}