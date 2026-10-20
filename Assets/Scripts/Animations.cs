using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Animations : MonoBehaviour
{
    public static Animations instance;
    public bool skipPause = false; // when true skips all current animations

    // For overlay click check
    [SerializeField] private GraphicRaycaster overlayRaycaster; // Drag your Overlay Canvas's GraphicRaycaster here
    private PointerEventData pointerEventData;
    private List<RaycastResult> raycastResults = new List<RaycastResult>();

    private void Awake()
    {
        instance = this;
    }

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
            // checking if object still exists
            if (obj == null) yield break;

            t += Time.deltaTime;
            float clampedT = t / phaseTime;
            float coolT = 1 - (1 - clampedT) * (1 - clampedT);

            Vector3 newScale = Vector3.Lerp(startingScale, targetScale, coolT);
            obj.transform.localScale = Vector3.Max(Vector3.zero, newScale);
            yield return null;
        }

        t = 0;
        // shrinking phase
        while (t < phaseTime)
        {
            // checking if object still exists
            if (obj == null) yield break;

            t += Time.deltaTime;
            float clampedT = t / phaseTime;
            float coolT = clampedT * clampedT;

            Vector3 newScale = Vector3.Lerp(targetScale, startingScale, coolT);
            obj.transform.localScale = Vector3.Max(Vector3.zero, newScale);
            yield return null;
        }

        obj.transform.localScale = startingScale;
    }

    public void ShakeAnim(GameObject obj, float shakeIntensity, float shakeLength)
    {
        StartCoroutine(Shake(obj, shakeLength, shakeIntensity));
    }

    IEnumerator Shake(GameObject obj, float shakeLength, float shakeIntensity)
    {
        // shaking the healthbar for extra juice
        float t = 0;
        float maxIntensity = shakeIntensity;
        Vector3 startingPosition = obj.transform.localPosition;

        while (t < shakeLength)
        {
            // checking if object still exists
            if (obj == null) yield break;

            // Gradually decreasing the intensity
            t += Time.deltaTime;
            float actualT = t / shakeLength;
            float currentIntensity = Mathf.Lerp(maxIntensity, 0f, actualT);

            // Random position offset
            float xOffset = UnityEngine.Random.Range(-1f, 1f) * currentIntensity;
            float yOffset = UnityEngine.Random.Range(-1f, 1f) * currentIntensity;

            obj.transform.localPosition += new Vector3(xOffset, yOffset, 0);

            yield return null;

            // Reverting the offset
            obj.transform.localPosition = startingPosition;
        }
    }

    /// <summary>
    /// Squashes (amount > 0) or stretches (amount < 0) the object, then springs it back to restScale with a little wobble
    /// </summary>
    public IEnumerator SquashStretch(Transform target, Vector3 restScale, float amount, float time)
    {
        float t = 0;
        while (t < time)
        {
            // checking if object still exists
            if (target == null) yield break;

            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / time);

            // damped wobble that starts at full squash
            float damping = (1 - clampedT) * (1 - clampedT);
            float squash = amount * Mathf.Cos(clampedT * Mathf.PI * 2.5f) * damping;
            SetSquash(target, restScale, squash);
            yield return null;
        }

        if (target != null) target.localScale = restScale;
    }

    /// <summary>
    /// Sets a squashed (amount > 0) or stretched (amount < 0) scale, keeping the volume roughly the same
    /// </summary>
    public void SetSquash(Transform target, Vector3 restScale, float amount)
    {
        target.localScale = new Vector3(restScale.x * (1 + amount), restScale.y * (1 - amount), restScale.z);
    }

    // ===========================
    //  **** COROUTINE STUFF ****
    // ===========================


    /// <summary>
    /// Can be used instead of yield return new WaitForSeconds() if you want to make the pause skipped when player clicks
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public IEnumerator SkippablePause(float time)
    {
        if (skipPause) yield break;

        float t = 0;
        while (t < time)
        {
            // immediately skipping the pause (when clicked)
            if (skipPause && t > time * 0.05f) yield break;

            t += Time.deltaTime;
            yield return null;
        }
    }
    
    private bool IsPointerOverOverlayUI()
    {
        if (overlayRaycaster == null || EventSystem.current == null) return false;

        // Set up the pointer event at the current mouse position
        pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        raycastResults.Clear();
        overlayRaycaster.Raycast(pointerEventData, raycastResults);

        // If any raycast result hit a target on this canvas, it's over Overlay UI
        return raycastResults.Count > 0;
    }

    private void Update()
    {
        // registering clicks
        if (Input.GetButtonDown("Fire1"))
        {
            skipPause = true;
            if (IsPointerOverOverlayUI()) skipPause = false; // not letting it trigger if the pointer is over a UI object
        }
        if (Input.GetButtonUp("Fire1")) skipPause = false;
    }
}
