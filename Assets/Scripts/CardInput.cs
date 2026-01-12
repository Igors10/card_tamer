using UnityEngine;
using UnityEngine.EventSystems;

public class CardInput : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    Card thisCard;

    void Start()
    {
        thisCard = GetComponent<Card>();
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        thisCard.OnHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        thisCard.OnHover(false);
    }
}
