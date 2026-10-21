
using System;
using System.Collections.Generic;
using UnityEngine;

public class InteractableDialogue : Interactible
{
    [Serializable]
    public class StateDialogue
    {
        public string stateName;
        public string nodeName;
    }

    [Header("Dialogue")]
    [SerializeField] private DialogueManager dialogueManager;

    [Header("Object State")]
    [SerializeField] private string currentState = "Default";

    [Header("State Dialogue Mapping")]
    [SerializeField] private List<StateDialogue> stateDialogues = new();

    public override void Interact()
    {
        if (Time.timeScale <= 0f || dialogueManager == null)
            return;

        foreach (StateDialogue entry in stateDialogues)
        {
            if (entry.stateName == currentState)
            {
                if (!string.IsNullOrEmpty(entry.nodeName))
                    dialogueManager.StartDialogue(entry.nodeName);

                return;
            }
        }

        Debug.LogWarning(
            $"No dialogue configured for state '{currentState}' on {gameObject.name}."
        );
    }

    public void SetState(string newState)
    {
        currentState = newState;
    }

    public string GetState()
    {
        return currentState;
    }

    public bool IsInState(string stateName)
    {
        return currentState == stateName;
    }
}
