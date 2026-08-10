using UnityEngine;
using UnityEngine.EventSystems;

public class WorkshopOption : MonoBehaviour, IPointerClickHandler
{
    public AbilityNote abilityNote;
    [HideInInspector] public Workshop workshop;
    public void OnPointerClick(PointerEventData eventData)
    {
        workshop.PickAbilityOption(abilityNote.storedAbility, abilityNote.storedAbilityColor);
    }
}
