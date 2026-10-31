using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    // Makes menu buttons grow and tilt when hovered, sink when pressed and wobble when released

    [Header("hover")]
    [SerializeField] float hoverScale = 1.1f;
    [SerializeField] float hoverTilt = 4f; // degrees, direction is picked randomly every hover
    [SerializeField] float easeSpeed = 16f;
    [SerializeField] string hoverSFX = "OnHoverSFX";

    [Header("press")]
    [SerializeField] float pressScale = 0.92f;
    [SerializeField] float releaseSquash = 0.15f;
    [SerializeField] float releaseSquashTime = 0.35f;

    Button button;
    Vector3 restScale;
    Quaternion restRotation;
    float currentScale = 1f;
    float currentTilt = 0f;
    float tiltDirection = 1f;
    bool hovered = false;
    bool pressed = false;
    float squashT = 1f; // 1 = no squash playing

    private void Awake()
    {
        button = GetComponent<Button>();
        restScale = transform.localScale;
        restRotation = transform.localRotation;
    }

    private void OnDisable()
    {
        // menus get switched off while the button is still hovered, so it has to come back in the resting state
        hovered = false;
        pressed = false;
        squashT = 1f;
        currentScale = 1f;
        currentTilt = 0f;
        transform.localScale = restScale;
        transform.localRotation = restRotation;
    }

    bool Interactable => button == null || button.interactable;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!Interactable) return;

        hovered = true;
        tiltDirection = (Random.value > 0.5f) ? 1f : -1f;
        if (hoverSFX != "" && AudioManager.instance != null) AudioManager.instance.PlaySFX(hoverSFX);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        pressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (Interactable) pressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (pressed) squashT = 0f;
        pressed = false;
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;
        float easeT = 1 - Mathf.Exp(-easeSpeed * deltaTime);

        // easing scale and tilt
        float targetScale = (pressed) ? pressScale : (hovered) ? hoverScale : 1f;
        float targetTilt = (hovered && !pressed) ? hoverTilt * tiltDirection : 0f;
        currentScale = Mathf.Lerp(currentScale, targetScale, easeT);
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, easeT);

        // damped squash wobble after release
        float squash = 0f;
        if (squashT < 1f)
        {
            squashT = Mathf.Min(1f, squashT + deltaTime / releaseSquashTime);
            float damping = (1 - squashT) * (1 - squashT);
            squash = releaseSquash * Mathf.Cos(squashT * Mathf.PI * 2.5f) * damping;
        }

        transform.localScale = new Vector3(restScale.x * currentScale * (1 + squash), restScale.y * currentScale * (1 - squash), restScale.z);
        transform.localRotation = restRotation * Quaternion.Euler(0f, 0f, currentTilt);
    }
}
