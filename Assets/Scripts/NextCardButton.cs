using UnityEngine;
using UnityEngine.EventSystems;

public class NextCardButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("refs")]
    public GameObject glow;
    Vector3 defaultGlowScale;
    Vector3 highlightedGlowScale;

    void Start()
    {
        // set scales
        defaultGlowScale = glow.transform.localScale;
        highlightedGlowScale = defaultGlowScale * 1.2f;
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
        if (!GameManager.instance.executeManager.readyRevealCard) return;
        glow.transform.localScale = (mouseOver) ? highlightedGlowScale : defaultGlowScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!GameManager.instance.executeManager.readyRevealCard) return;
        glow.SetActive(false);
        GameManager.instance.executeManager.RevealCard();
    }
}
