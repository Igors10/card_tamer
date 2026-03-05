using UnityEngine;
using UnityEngine.EventSystems;

public class HoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Makes the element scale up a little when a mouse is over it

    [SerializeField] bool inGame = true;
    [SerializeField] float sizeMod = 1.1f;
    Vector3 defaultScale;
    Vector3 highlightedScale;

    void Start()
    {
        defaultScale = transform.localScale;
        highlightedScale = defaultScale * sizeMod;
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnHover(false);
    }

    void OnHover(bool mouseOver)
    {
        // Only do the effect if it is your turn
        if (inGame && !GameManager.instance.yourTurn) return;

        transform.localScale = (mouseOver) ? highlightedScale : defaultScale;
    }
}
