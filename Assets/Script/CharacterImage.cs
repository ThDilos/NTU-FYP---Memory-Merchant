using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Yarn.Unity;

public class CharacterImage : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        characterImage.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [Header("UI Reference")]
    public Image characterImage;

    [Header("Character Sprites")]
    public List<Sprite> characterImages;

    [YarnCommand("portrait")]
    public void ChangePortrait(string spriteName)
    {
        Sprite targetSprite = characterImages.Find(s => s.name.ToLower() == spriteName.ToLower());

        if(targetSprite != null )
        {
            characterImage.gameObject.SetActive(true);
            characterImage.sprite = targetSprite;
        }else if(spriteName.ToLower() == "none" ||  spriteName.ToLower() == "hide")
        {
            characterImage.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"Portrait sprite '{spriteName}' not found in the characterImage list!");
        }
    }

}
