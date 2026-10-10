using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ReconstructionData", menuName = "Scriptable Objects/ReconstructionData")]
public class ReconstructionData : ScriptableObject
{
    public string reconstructionId;
    [TextArea(3, 8)]
    public string storyText;

    public List<ReconstructionSlotData> slots;
    public List<ReconstructionFragmentData> fragments;
    
}

[Serializable]
public class ReconstructionSlotData
{
    public string slotID;
    public string correctFragementID;
}

[Serializable]
public class ReconstructionFragmentData
{
    public string fragmentId;

    [TextArea]
    public string fragmentText;
}
