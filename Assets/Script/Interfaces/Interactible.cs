

using System.ComponentModel;
using UnityEngine;

public class Interactible : MonoBehaviour
{
    [Header("Basic interactible settings")]
    [SerializeField] private bool alwaysInteractible = true;
    [Description("Offsetting the marker from object center. By default directly up.")]
    public Vector2 interactionMarkerOffset = Vector2.up * 1.0f;
    [Description("The words on the interaction marker")]
    public string interactionPrompt = "Interact";

    [Header("Highlight Effect")]
    [Description("Glowing effect that's always visible? Add the glowing effect into highLightSFX. Will be spawned once [isHighlighted] becomes true.\nAnd destroyed if it becomes false.")]
    public bool isHighlighted = false;
    [SerializeField] private GameObject highLightSFX;

    // Runtime vars
    private GameObject spawnedHighLightSFX;
    protected void Update()
    {
        if (spawnedHighLightSFX == null)
        {
            if (isHighlighted && highLightSFX != null)
            {
                spawnedHighLightSFX = Instantiate(highLightSFX, transform);
            }
        }
        else
        {
            if (!isHighlighted) Destroy(spawnedHighLightSFX);
        }
    }

    public virtual bool IsInteractable { get
        {
            if (!alwaysInteractible) 
            {
                Debug.LogError("[Interactible] Method \"Interactable\" needs to be overriden!");
                return false;
            }
            return true;
        }
    }

    public virtual void Interact()
    {
        Debug.Log($"[Interactible] {gameObject.name} was interacted.\nBut nothing happened since \"Interact\" method needs to be overriden.");
        return;
    }
}