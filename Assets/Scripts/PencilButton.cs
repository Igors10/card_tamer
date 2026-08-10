using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using UnityEditor;

public class PencilButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("refs")]
    public Image pencilColorImage;
    public GameObject pencil;
    [SerializeField] Material selectMaterial;
    [HideInInspector] public WorkshopColorSelect workshopColorSelect; // only assigned to pencils in the workshop
    public Material defaultMaterial;
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
    Vector3 defaultScale = new Vector3(1.6f, 1.6f, 1f);
    Quaternion defaultRotation;
    
    void Awake()
    {
        defaultScale = pencil.transform.localScale;
        defaultRotation = pencil.transform.localRotation;
    }

    void Start()
    {
        // getting current pencil material
        defaultPos = pencil.transform.position;
    }

    void OnEnable()
    {
        OnHover(false);
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnHover(false);
    }

    public void OnHover(bool isHovered)
    {
        isHoveredOver = isHovered;
        if (selected) return;

        // playing SFX when hovering over only
        if (isHovered) AudioManager.instance.PlaySFX("OnHoverSFX", 0.25f);

        Highlight(isHovered);
    }

    public void Highlight(bool highlight)
    {
        // enabling green outline while hovered over
        pencilColorImage.material = (highlight) ? selectMaterial : defaultMaterial;

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
    public void SelectPencil(bool isSelected, bool rotationToDefault = true)
    {
        // marking selected and passing this pencil back to workshopColorSelect
        selected = isSelected;
        if (workshopColorSelect != null) workshopColorSelect.selectedPencil = this;

        // select outline
        pencilColorImage.material = (isSelected) ? selectMaterial : defaultMaterial;

        // sfx
        AudioManager.instance.PlaySFX("ButtonSFX");

        // animation
        cartoonShakeEffect.enabled = isSelected;
        if (isSelected) Animations.instance.PopAnim(pencil, 0.18f, -0.12f);

        // setting rotation back to default when deselected
        if (isSelected == false && rotationToDefault) pencil.transform.localRotation = defaultRotation; //Quaternion.Euler(0, 0, -90)
    }

    public void OffsetPencil(float offsetX)
    {
        pencil.transform.position = defaultPos + new Vector3(offsetX, 0, 0);
    }
}
