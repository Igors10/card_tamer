using Unity.VisualScripting;
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
        highlightedGlowScale = defaultGlowScale * 1.05f;
    }

    void OnEnable()
    {
        ShowUnitOrder(false);
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
        // Applying glow
        if (GameManager.instance.executeManager.readyRevealCard)
        {
            glow.transform.localScale = (mouseOver) ? highlightedGlowScale : defaultGlowScale;
        }
        else
        {
            ShowUnitOrder(mouseOver);
        }
    }

    void ShowUnitOrder(bool isShown)
    {
        // Showing units' order of action
        for (int i = 0; i < GameManager.instance.executeManager.plannedCardStack.Count; i++)
        {
            GameManager.instance.executeManager.plannedCardStack[i].unit.orderMarker.gameObject.SetActive(isShown);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!GameManager.instance.executeManager.readyRevealCard) return;
        glow.SetActive(false);
        GameManager.instance.executeManager.RevealCard();
    }
}
