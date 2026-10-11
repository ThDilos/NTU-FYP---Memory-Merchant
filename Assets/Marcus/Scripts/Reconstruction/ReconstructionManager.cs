```csharp
using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ReconstructionManager : MonoBehaviour
{
    [Header("Reconstruction Data")]
    public ReconstructionData reconstructionData;

    [Header("Prefabs")]
    public GameObject slotPrefab;
    public GameObject fragmentPrefab;

    [Header("UI References")]
    public Transform slotArea;
    public Transform fragmentArea;
    public TMP_Text storyText;
    public TMP_Text resultText;

    private List<ReconstructionSlot> slots =
        new List<ReconstructionSlot>();

    private List<FragmentInput> fragments =
        new List<FragmentInput>();

    private void Start()
    {
        LoadReconstruction();
    }

    public void LoadReconstruction()
    {
        // Clear previous puzzle objects.
        foreach (Transform child in slotArea)
            Destroy(child.gameObject);

        foreach (Transform child in fragmentArea)
            Destroy(child.gameObject);

        slots.Clear();
        fragments.Clear();

        if (reconstructionData == null)
        {
            Debug.LogError("Reconstruction data is missing!");
            return;
        }

        // Display the story.
        if (storyText != null)
            storyText.text = reconstructionData.storyText;

        if (resultText != null)
            resultText.text = "";

        // Generate slots.
        foreach (ReconstructionSlotData data
                 in reconstructionData.slots)
        {
            GameObject obj = Instantiate(
                slotPrefab, slotArea);

            ReconstructionSlot slot =
                obj.GetComponent<ReconstructionSlot>();

            slot.Setup(data);
            slots.Add(slot);
        }

        // Generate image fragments.
        foreach (ReconstructionFragmentData data
                 in reconstructionData.fragments)
        {
            GameObject obj = Instantiate(
                fragmentPrefab, fragmentArea);

            FragmentInput fragment =
                obj.GetComponent<FragmentInput>();

            fragment.Setup(
                data.fragmentId,
                data.fragmentImage
            );

            fragments.Add(fragment);
        }
    }

    public void CheckAnswer()
    {
        if (slots.Count == 0)
        {
            resultText.text = "No reconstruction slots found.";
            return;
        }

        int correctCount = 0;

        foreach (ReconstructionSlot slot in slots)
        {
            if (slot.IsCorrect())
                correctCount++;
        }

        if (correctCount == slots.Count)
        {
            resultText.text =
                "Memory successfully reconstructed!";
        }
        else
        {
            resultText.text =
                $"Correct fragments: {correctCount}/{slots.Count}. " +
                "Keep reconstructing!";
        }
    }

    public void ResetReconstruction()
    {
        // Reload the same puzzle from its data asset.
        LoadReconstruction();
    }
}
```