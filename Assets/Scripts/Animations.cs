using UnityEngine;
using System.Collections;

public class Animations : MonoBehaviour
{
    public void PopAnim(GameObject obj, float time, float intensity)
    {
        StartCoroutine(Pop(obj, time, intensity));
    }

    IEnumerator Pop(GameObject obj, float time, float intensity)
    {
        Vector3 startingScale = obj.transform.localScale;
        Vector3 targetScale = startingScale + new Vector3(intensity, intensity, 0f);
        float t = 0;
        float phaseTime = time / 2;

        // growing phase
        while (t < phaseTime)
        {
            t += Time.deltaTime;
            float clampedT = t / phaseTime;
            float coolT = 1 - (1 - clampedT) * (1 - clampedT);

            obj.transform.localScale = Vector3.Lerp(startingScale, targetScale, coolT);
            yield return null;
        }

        t = 0;
        // shrinking phase
        while (t < phaseTime)
        {
            t += Time.deltaTime;
            float clampedT = t / phaseTime;
            float coolT = clampedT * clampedT;

            obj.transform.localScale = Vector3.Lerp(targetScale, startingScale, coolT);
            yield return null;
        }

        obj.transform.localScale = startingScale;
    }
}
