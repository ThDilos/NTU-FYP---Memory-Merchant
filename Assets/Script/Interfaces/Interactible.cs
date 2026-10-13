

using System.ComponentModel;
using UnityEngine;

public class Interactible : MonoBehaviour
{
    [Header("Basic interactible settings")]
    [SerializeField] protected bool initiallyInteractible = true;
    [Tooltip("Offsetting the marker from object center. By default directly up.")]
    public Vector2 interactionMarkerOffset = Vector2.up * 1.0f;
    [Tooltip("The words on the interaction marker")]
    public string interactionPrompt = "Interact";
    [Tooltip("Text if it's not interactible")]
    public string unabletoInteractPrompt = "Nuh uh";

    [Header("Highlight Effect")]
    [Tooltip("Glowing effect that's always visible? Add the glowing effect into highLightSFX. Will be spawned once [isHighlighted] becomes true.\nAnd destroyed if it becomes false.")]
    public bool isHighlighted = false;
    [SerializeField] private GameObject highLightSFX;

    // Runtime vars
    private GameObject spawnedHighLightSFX;
    private bool interactible;
    public bool IsInteractable { get { return interactible; } }

    protected void Start()
    {
        interactible = initiallyInteractible;
    }

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

    public virtual void Interact()
    {
        Debug.Log($"[Interactible] {gameObject.name} was interacted.\nBut nothing happened since \"Interact\" method needs to be overriden.");
        return;
    }

    public void SetInteractable(bool input)
    {
        interactible = input;
    }
}