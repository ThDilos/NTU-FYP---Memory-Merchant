using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Yarn.Unity;

public class FadeOverlay : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Image fadeImage;

    private static FadeOverlay instance;

    private void Awake()
    {
        instance = this;
    }

    [YarnCommand("fadeOut")]
    public void FadeOut()
    {
        StartCoroutine(Fade(0f, 1f, 1f));
    }

    [YarnCommand("fadeIn")]
    public void FadeIn()
    {
        StartCoroutine(Fade(1f, 0f, 1f));
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        float time = 0f;
        Color color = fadeImage.color;

        while(time < duration)
        {
            time += Time.deltaTime;

            float alpha = Mathf.Lerp(startAlpha, endAlpha, time/duration);

            color.a = alpha;
            fadeImage.color = color;

            yield return null;
        }

        color.a = endAlpha;
        fadeImage.color = color;
    }
}
