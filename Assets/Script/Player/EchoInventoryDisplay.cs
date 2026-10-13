using System.Collections.Generic;
using UnityEngine;


// Handle the isplays of the Echos on screen
// Empty Slots will be empty
public class EchoInventoryDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] private GameObject echoSlotPrefab; // Should contain SlotDisplay class
    [SerializeField] private float widthPerSlot = 100.0f;

    [Header("Debug Tests on Unity Editor, does not affect game")]
    [SerializeField] private int testSlotCount = 3;
    [SerializeField] private float debugCircleRadius = 10f;

    // Runtime vars
    private int totalSlotCount = 0;
    private List<GameObject> echoSlots = new List<GameObject>();

    public static EchoInventoryDisplay Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
    }

    public void Initialize(int totalSlotCount)
    {
        if (echoSlotPrefab == null)
        {
            Debug.LogError("[EchoInventoryDisplay] Echo Slot prefab is not set!");
            return;
        }

        this.totalSlotCount = totalSlotCount;

        GenerateBaseSlots();
    }

    // Spawn the echo slots
    private void GenerateBaseSlots()
    {
        for (int x = 0; x < totalSlotCount; x++)
        {
            GameObject spawnedSlot = Instantiate(echoSlotPrefab, transform.position + Vector3.left * x * widthPerSlot, Quaternion.identity, transform);
            echoSlots.Add(spawnedSlot);
        }

        UpdateInventorySlot();
    }

    // This should be called by Level Inventory whenever it's updated.
    // Does not update by itself (for performance)
    // Update all slots
    public void UpdateInventorySlot()
    {
        for (int i = 0; i < LevelInventory.Instance.memoryEchoes.Count; ++i)
        {
            UpdateInventorySlot(i);
        }
    }

    // Update a single slot
    public void UpdateInventorySlot(int slotIndex)
    {
        // Collectible Interface: public Sprite Sprite and string DisplayName
        if (LevelInventory.Instance == null) { 
            Debug.LogError("[EchoInventoryDisplay] LevelInventory Instance is null!");
            return;
        }

        if (slotIndex >= echoSlots.Count)
        {
            Debug.LogError($"[EchoInventoryDisplay] SlotIndex {slotIndex} exceed total slot count of {echoSlots.Count}");
            return;
        }

        SlotDisplay slotDisplay = echoSlots[slotIndex].GetComponent<SlotDisplay>();
        if (slotDisplay == null)
        {
            Debug.LogError("[EchoInventoryDisplay] No SlotDisplay script attached on the EchoSlotPrefab!");
            return;
        }

        MemoryEcho echo = LevelInventory.Instance.memoryEchoes[slotIndex];

        slotDisplay.SetDisplay(echo.Sprite, echo.DisplayName, (slotIndex + 1 % 10).ToString());
    }

    void OnDrawGizmos()
    {
        if (testSlotCount == 0) return;

        for (int x = 0; x < testSlotCount; x++)
        {
            Gizmos.DrawWireSphere(transform.position + Vector3.left * x * widthPerSlot, debugCircleRadius);
        }
    }
}