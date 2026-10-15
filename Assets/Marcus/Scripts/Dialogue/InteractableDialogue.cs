using UnityEngine;

public class InteractableDialogue : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
 

    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private string nodeName;

    private bool playerInRange;

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            dialogueManager.StartDialogue(nodeName);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("Press E to interact");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}
