using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Yarn.Unity;

public class CharacterImage : MonoBehaviour
{
    [Header("UI Reference")]
    public Image characterImage;

    [Header("Character Sprites")]
    public List<Sprite> characterImages;

    [Header("Yarn")]
    public DialogueRunner dialogueRunner;
    void Awake()
    {
        HidePortrait();
    }

    void Start()
    {
        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueStart.AddListener(HidePortrait);
            dialogueRunner.onDialogueComplete.AddListener(HidePortrait);
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (dialogueRunner == null || !dialogueRunner.IsDialogueRunning) HidePortrait();
    }

    void OnDestroy()
    {
        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueStart.RemoveListener(HidePortrait);
            dialogueRunner.onDialogueComplete.RemoveListener(HidePortrait);
        }
    }

    void HidePortrait()
    {
        if (characterImage != null) characterImage.enabled = false;
    }

    [YarnCommand("portrait")]
    public void ChangePortrait(string spriteName)
    {
        if (characterImage == null) return;
        if (dialogueRunner == null || !dialogueRunner.IsDialogueRunning ||
            string.Equals(spriteName, "none", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(spriteName, "hide", System.StringComparison.OrdinalIgnoreCase))
        {
            HidePortrait();
            return;
        }
        Sprite targetSprite = null;

        if (characterImages != null) foreach (Sprite sprite in characterImages)
        {
            if (sprite != null && string.Equals(sprite.name, spriteName, System.StringComparison.OrdinalIgnoreCase))
            {
                targetSprite = sprite;
                break;
            }
        }

        if (targetSprite != null)
        {
            characterImage.enabled = true;
            characterImage.sprite = targetSprite;
        }
        else
        {
            Debug.LogWarning(
                $"Portrait sprite '{spriteName}' not found in the Portrait list!"
            );
        }
    }

    public void TestPortrait()
    {
        Debug.Log("Testing character portrait");

        gameObject.SetActive(true);

        if (characterImage != null)
        {
            characterImage.gameObject.SetActive(true);
            characterImage.enabled = true;
            characterImage.color = Color.white;
        }
        else
        {
            Debug.LogError("Character Portrait Image is not assigned!");
        }
    }

}
