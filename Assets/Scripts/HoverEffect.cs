using UnityEngine;
using UnityEngine.EventSystems;

public class HoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
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

    public void OnPointerClick(PointerEventData eventData)
    {
        ClickJuiceEffect();
    }

    void OnHover(bool mouseOver)
    {
        // Only do the effect if it is your turn
        if (inGame && GameManager.instance != null && !GameManager.instance.yourTurn) return;

        transform.localScale = (mouseOver) ? highlightedScale : defaultScale;
    }

    /// <summary>
    /// Makes the button pop when player clicks it
    /// </summary>
    public void ClickJuiceEffect()
    {
        transform.localScale = defaultScale;

        if (Animations.instance != null)
        {
            Animations.instance.PopAnim(this.gameObject, 0.3f, -0.25f);
        }
    }
}
