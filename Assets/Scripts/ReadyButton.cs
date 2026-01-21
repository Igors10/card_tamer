using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class ReadyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("refs")]
    [SerializeField] TextMeshProUGUI buttonText;
    [SerializeField] Button button;
    [SerializeField] Image sprite;
    [SerializeField] GameObject buttonGlow;

    private void Start()
    {
        InitButtonState();
    }

    public void InitButtonState()
    {
        button.gameObject.SetActive(true);
        button.interactable = true;
        buttonText.text = GameManager.instance.GetState().buttonText;
        sprite.color = GameManager.instance.GetState().buttonColor;
    }

    public void ButtonClick()
    {
        // general buttom clicks effects
        buttonGlow.SetActive(false);

        // game state specific button click effects
       
        GameManager.instance.endStateReady = true;
        button.interactable = false;
        buttonText.text = GameManager.instance.GetState().pressedButtonText;
        sprite.color = GameManager.instance.GetState().pressedButtonColor;

        // ending the turn
        GameManager.instance.EndTurn();
    }

    /// <summary>
    /// Enables juice effects when mouse hovers over the button
    /// </summary>
    /// <param name="mouseOver"></param>
    void OnHover(bool mouseOver)
    {
        if (!button.interactable) return;
        buttonGlow.SetActive(mouseOver);
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnHover(false);
    }
}
