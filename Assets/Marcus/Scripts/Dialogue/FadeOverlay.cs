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
        SetAlpha(0f);
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
        SetAlpha(startAlpha);

        while(elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed/fadeDuration);

            SetAlpha(alpha);

            yield return null;
        }

        SetAlpha(endAlpha);
    }

    void SetAlpha(float alpha)
    {
        if (fadeImage == null) return;
        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
        // A transparent fade must let pointer events reach the shop's draggable manual.
        fadeImage.raycastTarget = alpha > 0f;
        CanvasGroup group = fadeImage.GetComponent<CanvasGroup>();
        if (group != null) group.blocksRaycasts = alpha > 0f;
    }
}
