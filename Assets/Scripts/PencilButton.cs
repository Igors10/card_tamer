using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PencilButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("refs")]
    [SerializeField] Image pencilColorImage;
    [SerializeField] Material selectMaterial;
    Material defaultMaterial;

    void Start()
    {
        // getting current pencil material
        defaultMaterial = pencilColorImage.material;
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
        // enabling green outline while hovered over
        pencilColorImage.material = (isHovered) ? selectMaterial : defaultMaterial;
    }
}
