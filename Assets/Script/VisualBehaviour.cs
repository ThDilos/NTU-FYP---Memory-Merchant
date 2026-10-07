using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Yarn.Unity;

public class VisualBehaviour : MonoBehaviour
{
    private DialogueRunner DialogueRunner;
    //private FadeOverlay fadeOverlay;

    private void Awake()
    {
        DialogueRunner = FindObjectOfType<Yarn.Unity.DialogueRunner>();
        //fadeOverlay = FindObjectOfType<FadeOverlay>();

        //DialogueRunner.AddCommandHandler<float>("fadeIn", FadeIn);
        //DialogueRunner.AddCommandHandler<float>("fadeOut", FadeOut);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /*private Coroutine FadeIn(float time = 1f)
    {
        return StartCoroutine(fadeOverlay.FadeIn(time));
    }
    private Coroutine FadeOut(float time = 1f)
    {
        return StartCoroutine(fadeOverlay.FadeOut(time));
    }*/
}
