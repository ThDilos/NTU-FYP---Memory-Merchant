using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Level Settings")]
    [Description("The name will be used by Unlocker later.\nThe fog gameObjects that already exist in scene will be activated on Start, and deactivated when the room is unlocked.")]
    [SerializeField] private Dictionary<string, List<GameObject>> foggedRooms = new Dictionary<string, List<GameObject>>();

    public static LevelManager Instance { get; private set; }
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Activate all fogs on start
        foggedRooms.ToList().ForEach(foggedRoom => foggedRoom.Value.ForEach(fog => fog.SetActive(true)));
    }
    public void UnlockRoom(string roomName)
    {
        if (foggedRooms.ContainsKey(roomName))
        {
            foggedRooms[roomName].ForEach(fog => fog.SetActive(false));
        }
        else
        {
            Debug.LogError($"[LevelManager] Room {roomName} doesn't exist in the Level Manager!");
        }
    }
}