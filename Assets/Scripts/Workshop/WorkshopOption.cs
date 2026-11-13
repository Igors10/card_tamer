using UnityEngine;
using UnityEngine.EventSystems;

public class WorkshopOption : MonoBehaviour, IPointerClickHandler
{
    public Ability sticker;
    [HideInInspector] public Workshop workshop;
    public void OnPointerClick(PointerEventData eventData)
    {
        workshop.PickAbilityOption(new Sticker(sticker.abilityData, sticker.onRight), sticker.abilityColor);
    }
}
