using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Juice for a Toggle: outline material while it's on, the checkmark stamps in (and pops out),
/// the toggle grows a little on hover, click sound
/// </summary>
[RequireComponent(typeof(Toggle))]
public class JuicyToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("refs")]
    [SerializeField] Image outlinedImage; // gets the outline material while on
    [SerializeField] Material onOutlineMaterial;
    [SerializeField] Transform checkmark;

    [Header("juice")]
    [SerializeField] float stampInTime = 0.3f;
    [SerializeField] float stampOutTime = 0.12f;
    [SerializeField] float stampTwist = -30f; // checkmark comes in rotated and straightens
    [SerializeField] float hoverScale = 1.06f;
    [SerializeField] float hoverTime = 0.12f;

    Toggle toggle;
    Material defaultMaterial;
    Vector3 restScale, checkRestScale;
    Quaternion checkRestRotation;
    Coroutine stampRoutine, hoverRoutine;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        defaultMaterial = outlinedImage.material;
        restScale = transform.localScale;
        checkRestScale = checkmark.localScale;
        checkRestRotation = checkmark.localRotation;

        // the checkmark is animated here instead of the toggle's fade
        toggle.graphic = null;
        toggle.onValueChanged.AddListener(OnValueChanged);
    }

    void OnEnable()
    {
        // coroutines stop when the object gets disabled, starting from a clean state
        transform.localScale = restScale;
        Snap(toggle.isOn);
    }

    void Snap(bool isOn)
    {
        outlinedImage.material = isOn ? onOutlineMaterial : defaultMaterial;
        checkmark.gameObject.SetActive(isOn);
        checkmark.localScale = checkRestScale;
        checkmark.localRotation = checkRestRotation;
    }

    void OnValueChanged(bool isOn)
    {
        // changed from code while hidden (new drawing, loaded from gallery): no animation
        if (!isActiveAndEnabled) { Snap(isOn); return; }

        outlinedImage.material = isOn ? onOutlineMaterial : defaultMaterial;

        if (stampRoutine != null) StopCoroutine(stampRoutine);
        stampRoutine = StartCoroutine(isOn ? StampIn() : StampOut());
    }

    IEnumerator StampIn()
    {
        checkmark.gameObject.SetActive(true);
        float t = 0;
        while (t < stampInTime)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / stampInTime);

            // back out ease: overshoots a bit and settles
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float coolT = 1 + c3 * Mathf.Pow(clampedT - 1, 3) + c1 * Mathf.Pow(clampedT - 1, 2);

            checkmark.localScale = checkRestScale * coolT;
            checkmark.localRotation = checkRestRotation * Quaternion.Euler(0f, 0f, stampTwist * (1 - clampedT));
            yield return null;
        }
        Snap(true);
        stampRoutine = null;
    }

    IEnumerator StampOut()
    {
        float t = 0;
        while (t < stampOutTime)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / stampOutTime);
            checkmark.localScale = checkRestScale * (1 - clampedT * clampedT);
            yield return null;
        }
        Snap(false);
        stampRoutine = null;
    }

    IEnumerator ScaleTo(Vector3 target)
    {
        Vector3 start = transform.localScale;
        float t = 0;
        while (t < hoverTime)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / hoverTime);
            transform.localScale = Vector3.Lerp(start, target, 1 - (1 - clampedT) * (1 - clampedT));
            yield return null;
        }
        transform.localScale = target;
        hoverRoutine = null;
    }

    void Hover(bool hovered)
    {
        if (hoverRoutine != null) StopCoroutine(hoverRoutine);
        hoverRoutine = StartCoroutine(ScaleTo(hovered ? restScale * hoverScale : restScale));
    }

    public void OnPointerEnter(PointerEventData eventData) => Hover(true);
    public void OnPointerExit(PointerEventData eventData) => Hover(false);
    public void OnPointerClick(PointerEventData eventData) => AudioManager.instance.PlaySFX("ButtonSFX");
}
