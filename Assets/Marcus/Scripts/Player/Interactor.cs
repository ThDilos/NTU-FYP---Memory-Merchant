using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Interactor : MonoBehaviour
{
    [Header("Put in Input System that contains interaction key")]
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField] private string inputActionMapName = "Player";
    [SerializeField] private string inputActionName = "Interact";

    [Header("Interaction Marker")]
    [Tooltip("The marker that floats above interactible object")]
    [SerializeField] private bool disableMarker = false;
    [SerializeField] private GameObject marker;
    [SerializeField] private TMP_Text markerText;
    [Tooltip("Draw interaction hints above foreground scenery.")]
    [SerializeField] private int markerSortingOrder = 30;
    [SerializeField] private string markerAnimationOnInteract = "";

    [Header("Interaction Setting")]
    [Tooltip("Radius of the circle, from the player's transform.position")]
    [SerializeField] private float interactionRange = 2.0f;
    [Tooltip("Interactible needs to be in a united layer mask")]
    [SerializeField] private LayerMask interactibleLayerMask;

    // Runtime vars
    private InputAction interact;

    void OnEnable()
    {
        interact?.Enable();
    }

    void OnDisable()
    {
        interact?.Disable();
        if (marker != null) marker.SetActive(false);
    }

    void Awake()
    {
        if (marker != null)
        {
            Canvas markerCanvas = marker.GetComponent<Canvas>();
            if (markerCanvas != null)
            {
                markerCanvas.overrideSorting = true;
                markerCanvas.sortingOrder = markerSortingOrder;
            }
        }

        if (inputActionAsset == null )
        {
            Debug.LogError("[Interactor] InputActionAsset is not set.");
            return;
        }

        InputActionMap map = inputActionAsset.FindActionMap(inputActionMapName);
        interact = map.FindAction(inputActionName);
    }

    void Update()
    {
        if (interact == null) return;

        Interactible selectedInteractible = CheckInteractibleInRange();

        HandleMarker(selectedInteractible);
        if (selectedInteractible == null) return;

        if (interact.WasPerformedThisFrame()) {
            HandleInteraction(selectedInteractible);
            TriggerMarkerAnimation();
        }
    }

    private void HandleInteraction(Interactible interactible)
    {
        if (interactible.IsInteractable)
        {
            AudioManager.Instance?.PlayInteraction();
            interactible.Interact();
        }
        else
        {
            Debug.Log($"[Interactor] {interactible.name} is not interactible.");
        }
    }

    private Interactible CheckInteractibleInRange()
    {
        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, interactionRange, interactibleLayerMask);
        float closestDistance = Mathf.Infinity;
        Interactible highlightedInteractible = null;
        foreach (Collider2D col in cols)
        {
            Interactible interactible = col.GetComponent<Interactible>();
            if (interactible == null || !interactible.IsInteractable) continue;

            float distance = Vector2.Distance(transform.position, col.ClosestPoint(transform.position));
            if (distance < closestDistance)
            {
                highlightedInteractible = interactible;
                closestDistance = distance;
            }
        }

        return highlightedInteractible;
    }

    private void HandleMarker(Interactible interactible)
    {
        if (disableMarker || marker == null) return;
        if (interactible == null)
        {
            marker.SetActive(false);
            marker.transform.position = transform.position;
            return;
        } 
        else
        {
            marker.SetActive(true);
        }

        Vector2 markerPosition = interactible.InteractionMarkerPosition;
        
        marker.transform.position = markerPosition;

        if (markerText != null)
        {
            markerText.text = interactible.InteractionHint;
        }
    }

    private void TriggerMarkerAnimation()
    {
        if (disableMarker || marker == null || markerAnimationOnInteract.Length <= 0) return;
        Animation animation = marker.GetComponent<Animation>();

        if (animation == null)
        {
            Debug.LogWarning("[Interactor] marker does not contain an animation component but the markerAnimationOnInteract is set.\nNo animation is performed.");
            return;
        }

        animation.Play(markerAnimationOnInteract);
    }
}
