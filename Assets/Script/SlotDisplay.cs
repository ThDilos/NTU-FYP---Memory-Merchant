using TMPro;
using UnityEngine;
using UnityEngine.UI;

// This class is used as a generic display component.
public class SlotDisplay : MonoBehaviour
{
    [Header("Display Setting")]
    [SerializeField] private Image imageRenderer;
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private TMP_Text keybindDisplayText; // Used for Keybind?

    private void Start()
    {
        keybindDisplayText.text = "";
        displayText.text = "";
    }

    public void SetDisplay(Sprite sprite, string displayName, string keyBindString = "")
    {
        keybindDisplayText.text = keyBindString;
        imageRenderer.sprite = sprite;
        displayText.text = displayName;
    }

    public void UseMemoryEcho()
    {
        if (LevelInventory.Instance == null || keybindDisplayText.text == "") return;
        Debug.Log(keybindDisplayText.text);
        int realIndex = int.Parse(keybindDisplayText.text) == 0 ? 10 : int.Parse(keybindDisplayText.text); 
        LevelInventory.Instance.UseMemoryEchoIndex(realIndex);
    }
}