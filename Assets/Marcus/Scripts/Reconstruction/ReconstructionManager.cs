using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ReconstructionManager : MonoBehaviour
{
    public List<ReconstructionSlot> slots;
    public TMP_Text resultText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CheckAnswer()
    {
        int correctCount = 0;

        foreach (ReconstructionSlot slot in slots)
        {
            if (slot.IsCorrect())
            {
                correctCount++;
            }
        }

        if (correctCount == slots.Count)
        {
            resultText.text = "Memory successfully reconstructed!";
        }
        else
        {
            resultText.text = "There's still something missing.";
        }
    }
}
