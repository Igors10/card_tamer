using UnityEngine;
using UnityEngine.EventSystems;

public class CardInput : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
{
    Card thisCard;

    void Start()
    {
        thisCard = GetComponent<Card>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // right click zooms the card to read it, left click picks it
        if (eventData.button == PointerEventData.InputButton.Right) thisCard.Zoom(true);
        else thisCard.OnClick();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        thisCard.OnHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        thisCard.OnHover(false);
        thisCard.Zoom(false);
    }

    public void OnDrag(PointerEventData eventData)
    {

    }

    // only the left button drags (right click is for zooming)
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) thisCard.StartDrag();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) thisCard.EndDrag();
    }
}
