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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        characterImage.enabled = false;

        dialogueRunner.onDialogueComplete.AddListener(HidePortrait);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDestroy()
    {
        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueComplete.RemoveListener(HidePortrait);
        }
    }

    void HidePortrait()
    {
        characterImage.enabled = false;
    }

    [YarnCommand("portrait")]
    public void ChangePortrait(string spriteName)
    {
        Sprite targetSprite = null;

        foreach (Sprite sprite in characterImages)
        {
            if (sprite.name.ToLower() == spriteName.ToLower())
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
        else if (spriteName.ToLower() == "none" ||
                 spriteName.ToLower() == "hide")
        {
            characterImage.enabled = false;
        }
        else
        {
            Debug.LogWarning(
                $"Portrait sprite '{spriteName}' not found in the Expressions list!"
            );
        }
    }

}
