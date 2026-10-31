using UnityEngine;
using UnityEngine.EventSystems;

public class CardInput : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
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

    public void OnDrag(PointerEventData eventData)
    {
        
    }

    public void OnBeginDrag(PointerEventData eventData) 
    {
        thisCard.StartDrag();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        thisCard.EndDrag();
    }
}
