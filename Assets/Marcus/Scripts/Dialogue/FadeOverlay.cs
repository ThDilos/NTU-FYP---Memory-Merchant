using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Yarn.Unity;

public class FadeOverlay : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Image fadeImage;

    //[SerializeField] private GameObject fadeObject;
    public float fadeDuration = 2f;

    private static FadeOverlay instance;

    private void Awake()
    {
        instance = this;
    }

    [YarnCommand("fadeOut")]
    public void FadeOut()
    {
        StartCoroutine(Fade(0f, 1f));
    }

    [YarnCommand("fadeIn")]
    public void FadeIn()
    {
        StartCoroutine(Fade(1f, 0f));
    }
    void Start()
    {
        SetAlpha(0f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float elapsed = 0f;

        while(elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed/fadeDuration);

            SetAlpha(alpha);

            yield return null;
        }
    }

    void SetAlpha(float alpha)
    {
        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;

    }
}
