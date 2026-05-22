using UnityEngine;
using System.Collections;
using TMPro;
using UnityEditor.ShaderGraph.Internal;

public class PowerBonus : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] TextMeshProUGUI text;
    Coroutine currentBonus;

    [Header("anim params")]
    [SerializeField] float fadeTime;
    [SerializeField] float timeBeforeFade;

    public void StartShowingBonus(int bonus)
    {
        if (currentBonus != null) StopCoroutine(currentBonus);
        currentBonus = StartCoroutine(ShowBonus(bonus));
    }

    /// <summary>
    /// Making a number appear next to power when power is added/reduced
    /// </summary>
    /// <param name="bonus"></param>
    /// <returns></returns>
    public IEnumerator ShowBonus(int bonus)
    {
        // enabling text
        text.text = (bonus > 0) ? "+" + bonus.ToString() : bonus.ToString();
        text.gameObject.SetActive(true);
        text.color = new Color(text.color.r, text.color.g, text.color.b, 1f);

        yield return new WaitForSeconds(timeBeforeFade);

        // fading text away
        float t = 0;
        
        while (t < fadeTime)
        {
            // timer
            t += Time.deltaTime;
            float clampedT = t / fadeTime;

            // fading text
            float alpha = 1 - clampedT;
            text.color = new Color(text.color.r, text.color.g, text.color.b, alpha);

            yield return null;
        }

        // deactivating text
        text.gameObject.SetActive(false);

        // removing reference to coroutine
        currentBonus = null;
    }
}
