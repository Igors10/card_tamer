using UnityEngine;
using UnityEngine.EventSystems;

public class WorkshopOption : MonoBehaviour, IPointerClickHandler
{
    public Card card;
    [HideInInspector] public Workshop workshop;
    public void OnPointerClick(PointerEventData eventData)
    {
        workshop.PickCardOption(card.cardData);
    }
}
