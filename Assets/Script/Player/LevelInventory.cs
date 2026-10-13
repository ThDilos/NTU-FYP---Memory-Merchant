using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.InputSystem;

public class LevelInventory : MonoBehaviour
{
    [Description("This inventory only exists in a level")]
    [SerializeField] private int echoSlotCount = 1;

    [Header("Using Memory Echo")]
    [SerializeField] InputActionAsset inputActionAsset;
    [SerializeField] private string inputActionMapName = "Player";
    [SerializeField] private string inputActionName = "Memory Echo Shortcut";

    // Runtime vars
    public List<MemoryEcho> memoryEchoes = new List<MemoryEcho>(); 
    public List<MemoryFragment> memoryFragments = new List<MemoryFragment>();

    public static LevelInventory Instance { get; private set; }

    private InputAction usingEcho;
    void OnEnable()
    {
        usingEcho.Enable();
    }
    void OnDisable()
    {
        usingEcho.Disable();
    }

    void Awake()
    {
        Instance = this;
        InputActionMap map = inputActionAsset.FindActionMap(inputActionMapName);
        usingEcho = map.FindAction(inputActionName);
    }

    public void Initialize() { Initialize(echoSlotCount); }
    public void Initialize(int echoSlotCount)
    {
        this.echoSlotCount = echoSlotCount;

        if (EchoInventoryDisplay.Instance == null)
        {
            Debug.LogWarning("[LevelInventory] Missing EchoInventoryDisplay Instance.");
        } 
        else
        {
            EchoInventoryDisplay.Instance.Initialize(echoSlotCount);    
        }
    }

    public void Collect(Collectible collectible)
    {
        Debug.Log($"[LevelInventory] Player collected {collectible.DisplayName}");

        if (collectible is MemoryEcho)
        {
            if (memoryEchoes.Contains((MemoryEcho)collectible))
            {
                memoryEchoes.Remove((MemoryEcho)collectible);
            }
            if (memoryEchoes.Count >= echoSlotCount)
            {
                // At this stage, remove the first echo. Later can introduce replace function
                memoryEchoes.RemoveAt(memoryEchoes.Count - 1);
            }
            memoryEchoes.Insert(0, (MemoryEcho)collectible);
            if (EchoInventoryDisplay.Instance != null) EchoInventoryDisplay.Instance.UpdateInventorySlot();
        }
        else if (collectible is MemoryFragment)
        {
            if (memoryFragments.Contains((MemoryFragment)collectible)) return;
            memoryFragments.Add((MemoryFragment)collectible);
        }

        collectible.OnCollect();
    }

    void Update()
    {
        HandleMemoryEchoUsage();
    }
    private void HandleMemoryEchoUsage()
    {
        if (
            memoryEchoes.Count == 0 ||
            !usingEcho.WasPerformedThisFrame()
            ) return;

        int pressedKey = int.Parse(usingEcho.activeControl.name);
        if (pressedKey == 0) pressedKey = 10;
        UseMemoryEchoIndex(pressedKey);
    }

    public void UseMemoryEchoIndex(int index)
    {
        if (memoryEchoes.Count < index) return;
        memoryEchoes[index - 1].TriggerEchoFunction();
    }
}