using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class AutoFade : MonoBehaviour
{
    [Header("refs")]
    SpriteRenderer[] sprites;
    Image[] images;
    TextMeshProUGUI[] texts;
    Coroutine currentFade;

    float[] spriteAlphas;
    float[] imageAlphas;
    float[] textAlphas;

    [Header("fade settings")]
    [SerializeField] float fadeInTime;
    [SerializeField] float stayTime;
    [SerializeField] float fadeOutTime;
    [SerializeField] bool allowSkipToFadeOut;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        GetAllComponents();
    }

    void GetAllComponents()
    {
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        float[] spritesA = new float[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            spritesA[i] = sprites[i].color.a;
        }
        spriteAlphas = spritesA;

        images = GetComponentsInChildren<Image>(true);
        float[] imagesA = new float[images.Length];
        for (int i = 0; i < images.Length; i++)
        {
            imagesA[i] = images[i].color.a;
        }
        imageAlphas = imagesA;

        texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        float[] textsA = new float[texts.Length];
        for (int i = 0; i < texts.Length; i++)
        {
            textsA[i] = texts[i].color.a;
        }
        textAlphas = textsA;
    }

    private void OnEnable()
    {
        currentFade = StartCoroutine(FadeEffect());
    }

    public IEnumerator FadeEffect(float newFadeInTime = 0f, float newFadeOutTime = 0f)
    {
        // stopping prev fade
        if (currentFade != null) StopCoroutine(currentFade);

        // choosing between base and new fade times
        float currentFadeInTime = (newFadeInTime == 0) ? fadeInTime : newFadeInTime;
        float currentFadeOutTine = (newFadeOutTime == 0) ? fadeOutTime : newFadeOutTime; 

        float t = 0;

        // fade in
        if (fadeInTime > 0)
        {
            while (t < fadeInTime)
            {
                t += Time.deltaTime;
                float clampedT = t / fadeInTime;
                SetObjectsAlpha(clampedT);
                yield return null;
            }

            t = 0;
        }

        // stay
        DefaultObjectAlphas();
        if (allowSkipToFadeOut) yield return Animations.instance.SkippablePause(stayTime);
        else yield return new WaitForSeconds(stayTime);

        // fade out
        FadeOut(fadeOutTime);
    }

    /// <summary>
    /// Call to fade out the object
    /// </summary>
    /// <param name="fadeOutTime"></param>
    public void FadeOut(float fadeOutT)
    {
        StartCoroutine(OnlyFadeOut(fadeOutT));
    }

    IEnumerator OnlyFadeOut(float fadeOutT)
    {
        float t = 0;

        if (fadeOutT > 0)
        {
            while (t < fadeOutT)
            {
                t += Time.deltaTime;
                float clampedT = t / fadeOutT;
                SetObjectsAlpha(1 - clampedT);
                yield return null;
            }

            gameObject.SetActive(false);
        }
    }

    void DefaultObjectAlphas()
    {
        // changing alpha in Sprite Renderers
        for (int i = 0; i < sprites.Length; i++)
        {
            Color newColor = sprites[i].color;
            newColor.a = spriteAlphas[i];
            sprites[i].color = newColor;
        }

        // changing alpha in Images
        for (int i = 0; i < images.Length; i++)
        {
            Color newColor = images[i].color;
            newColor.a = imageAlphas[i];
            images[i].color = newColor;
        }

        // changing alpha in Texts
        for (int i = 0; i < texts.Length; i++)
        {
            Color newColor = texts[i].color;
            newColor.a = textAlphas[i];
            texts[i].color = newColor;
        }
    }

    void SetObjectsAlpha(float alpha)
    {
        // changing alpha in Sprite Renderers
        for (int i = 0; i < sprites.Length; i++)
        {
            Color newColor = sprites[i].color;
            if (alpha <= spriteAlphas[i]) newColor.a = alpha;
            sprites[i].color = newColor;
        }

        // changing alpha in Images
        for (int i = 0; i < images.Length; i++)
        {
            Color newColor = images[i].color;
            if (alpha <= imageAlphas[i]) newColor.a = alpha;
            images[i].color = newColor;
        }

        // changing alpha in Texts
        for (int i = 0; i < texts.Length; i++)
        {
            Color newColor = texts[i].color;
            if (alpha <= textAlphas[i]) newColor.a = alpha;
            texts[i].color = newColor;
        }
    }
}
