using UnityEngine;
using System.Collections;
using UnityEngine.UI;

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
            float clampedT = t / moveTime;
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
            doodleT += Time.deltaTime;

            // rotating the loading symbol
            loadingSymbol.transform.localRotation = Quaternion.Euler(0f, 0f, t * symbolSpinIntensity);

            // rotating the doodle
            if (doodleT > doodleRotationIntervals)
            {
                doodleT = 0;
                doodleRotateLeft = !doodleRotateLeft;
                loadingDoodle.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                float doodleRotationAngle = (doodleRotateLeft) ? -doodleRotationIntensity : doodleRotationIntensity; 
                loadingDoodle.transform.localRotation = Quaternion.Euler(0f, 0f, doodleRotationAngle);
            }

            yield return null;
        }

        // Disabling loading elements
        loadingObj.SetActive(false);

        StartCoroutine(ApplyLoadingFog(false));
    }
}
