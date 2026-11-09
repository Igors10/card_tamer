using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class LoadingFog : MonoBehaviour
{
    [Header("appearing")]
    [SerializeField] float moveTime;
    [SerializeField] float exitDelay;

    [Header("loading")]
    [SerializeField] GameObject loadingObj;
    [SerializeField] Image loadingSymbol;
    [SerializeField] Image loadingDoodle;
    [SerializeField] float doodleRotationIntervals;
    [SerializeField] float doodleRotationIntensity;
    [SerializeField] float symbolSpinIntensity;
    [SerializeField] TMP_Text loadingText; // optional message in the middle of the fog (lobby uses it)

    Coroutine loadingLoop;


    /// <summary>
    /// Enables and disables loading screen/transitions
    /// </summary>
    /// <param name="isApplied"></param>
    /// <returns></returns>
    public IEnumerator ApplyLoadingFog(bool isApplied)
    {
        if (isApplied == false) yield return new WaitForSeconds(exitDelay);

        float t = 0;
        Vector3 startingPosition = transform.localPosition;
        float offsetY = (isApplied) ? Camera.main.pixelHeight + 600f : -Camera.main.pixelHeight - 600f;
        Vector3 targetPosition = startingPosition + new Vector3(0, offsetY, 0);

        while (t < moveTime)
        {
            t += Time.deltaTime;
            // clamped so it ends exactly on the target, the next move starts from there (the fog moves in and out every round)
            float clampedT = Mathf.Clamp01(t / moveTime);
            float coolT = (isApplied) ? clampedT * clampedT : 1 - (1 - clampedT) * (1 - clampedT);

            transform.localPosition = Vector3.Lerp(startingPosition, targetPosition, coolT);
            yield return null;
        }
    }

    public IEnumerator LoadingAnimation(float loadingTime)
    {
        // Enabling loading elements
        loadingObj.SetActive(true);

        float t = 0;
        float doodleT = 0;
        bool doodleRotateLeft = true;

        while (t < loadingTime)
        {
            t += Time.deltaTime;
            AnimateLoadingElements(t, ref doodleT, ref doodleRotateLeft);
            yield return null;
        }

        // Disabling loading elements
        loadingObj.SetActive(false);

        StartCoroutine(ApplyLoadingFog(false));
    }

    /// <summary>
    /// Shows the loading symbol, rat and a message until StopLoading is called. Doesn't move the fog itself
    /// </summary>
    public void StartLoading(string message)
    {
        loadingObj.SetActive(true);
        if (loadingText != null)
        {
            loadingText.text = message;
            loadingText.gameObject.SetActive(true);
        }

        if (loadingLoop == null) loadingLoop = StartCoroutine(LoadingLoop());
    }

    public void StopLoading()
    {
        if (loadingLoop != null) StopCoroutine(loadingLoop);
        loadingLoop = null;

        loadingObj.SetActive(false);
        if (loadingText != null) loadingText.gameObject.SetActive(false);
    }

    IEnumerator LoadingLoop()
    {
        float t = 0;
        float doodleT = 0;
        bool doodleRotateLeft = true;

        while (true)
        {
            t += Time.deltaTime;
            AnimateLoadingElements(t, ref doodleT, ref doodleRotateLeft);
            yield return null;
        }
    }

    /// <summary>
    /// Spins the loading symbol and wiggles the doodle, called every frame while loading
    /// </summary>
    void AnimateLoadingElements(float t, ref float doodleT, ref bool doodleRotateLeft)
    {
        doodleT += Time.deltaTime;

        // rotating the loading symbol
        loadingSymbol.transform.localRotation = Quaternion.Euler(0f, 0f, t * symbolSpinIntensity);

        // rotating the doodle
        if (doodleT > doodleRotationIntervals)
        {
            doodleT = 0;
            doodleRotateLeft = !doodleRotateLeft;
            float doodleRotationAngle = (doodleRotateLeft) ? -doodleRotationIntensity : doodleRotationIntensity;
            loadingDoodle.transform.localRotation = Quaternion.Euler(0f, 0f, doodleRotationAngle);
        }
    }
}
