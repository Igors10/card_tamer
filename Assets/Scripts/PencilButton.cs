using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class PencilButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("refs")]
    public Image pencilColorImage;
    public GameObject pencil;
    [SerializeField] Material selectMaterial;
    [HideInInspector] public WorkshopColorSelect workshopColorSelect; // only assigned to pencils in the workshop
    [HideInInspector] public Material defaultMaterial;
    public Button button;
    [SerializeField] CartoonShakeEffect cartoonShakeEffect;

    [Header("upscaling effect")]
    [SerializeField] bool upscaleOnHover;
    [SerializeField] float upscaleIntensity;
    [SerializeField] float upscaleTime;
    bool isHoveredOver;
    Coroutine currentUpscale;
    [HideInInspector] public bool selected;

    Vector3 defaultPos;
    Vector3 defaultScale;
    void Start()
    {
        // getting current pencil material
        defaultMaterial = pencilColorImage.material;
        defaultScale = pencil.transform.localScale;
        defaultPos = pencil.transform.position;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnHover(false);
    }

    void OnHover(bool isHovered)
    {
        isHoveredOver = isHovered;
        if (selected) return;

        // playing SFX when hovering over only
        if (isHovered) AudioManager.instance.PlaySFX("OnHoverSFX", 0.25f);

        // enabling green outline while hovered over
        pencilColorImage.material = (isHovered) ? selectMaterial : defaultMaterial;

        // triggering pencil highlight if it is a workshop pencil
        if (workshopColorSelect != null) workshopColorSelect.HighlightPencil(pencil);

        // triggering upscale Animation
        if (upscaleOnHover)
        {
            if (currentUpscale != null) StopCoroutine(UpscaleEffect());
            currentUpscale = StartCoroutine(UpscaleEffect());
        }
    }

    /// <summary>
    /// Upscales the pencil while hovered and downscales it back when released
    /// </summary>
    /// <returns></returns>
    IEnumerator UpscaleEffect()
    {
        // setting variables
        Vector3 upscaledSize = defaultScale * upscaleIntensity;
        float t = 0;

        // Upscaling
        while (isHoveredOver || selected)
        {
            if (t < upscaleTime)
            {
                t += Time.deltaTime;
                float clampedT = t / upscaleTime;

                Vector3 currentScale = Vector3.Lerp(defaultScale, upscaledSize, clampedT);
                pencil.transform.localScale = currentScale;
            }
            yield return null;
        }

        // Downscaling
        while (t > 0)
        {
            t -= Time.deltaTime;
            float clampedT = t / upscaleTime;

            Vector3 currentScale = Vector3.Lerp(defaultScale, upscaledSize, clampedT);
            pencil.transform.localScale = currentScale;
            yield return null;
        }

        // snapping to default scale
        pencil.transform.localScale = defaultScale;
    }

    public void SetColor(Color colorToSet)
    {
        pencilColorImage.color = colorToSet;
    }

    /// <summary>
    /// Visually marking pencil as selected
    /// </summary>
    /// <param name="isSelected"></param>
    public void SelectPencil(bool isSelected)
    {
        // marking selected and passing this pencil back to workshopColorSelect
        selected = isSelected;
        workshopColorSelect.selectedPencil = this;

        // select outline
        pencilColorImage.material = (isSelected) ? selectMaterial : defaultMaterial;

        // sfx
        AudioManager.instance.PlaySFX("ButtonSFX");

        // animation
        cartoonShakeEffect.enabled = isSelected;
        if (isSelected) Animations.instance.PopAnim(pencil, 0.18f, -0.12f);

        // setting rotation back to default when deselected
        if (isSelected == false) pencil.transform.localRotation = Quaternion.Euler(0, 0, -90);
    }

    public void OffsetPencil(float offsetX)
    {
        pencil.transform.position = defaultPos + new Vector3(offsetX, 0, 0);
    }
}
