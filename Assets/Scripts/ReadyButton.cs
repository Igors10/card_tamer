using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class ReadyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("refs")]
    [SerializeField] TextMeshProUGUI buttonText;
    public Button button;
    [SerializeField] Image sprite;
    [SerializeField] GameObject buttonGlow;

    private void Start()
    {
        UpdateButtonState();
    }

    public void UpdateButtonState(string customButtonText = null)
    {
        button.gameObject.SetActive(true);
        button.interactable = true;
        buttonText.text = (customButtonText == null) ? GameManager.instance.GetState().buttonText : customButtonText;
        sprite.color = GameManager.instance.GetState().buttonColor;
    }

    public void ButtonClick()
    {
        // game state specific button click effects
        switch (GameManager.instance.currentState)
        {
            // PLACING AND PLANNING
            default:

                GameManager.instance.player.endStateReady = true;
                button.interactable = false;
                buttonText.text = GameManager.instance.GetState().pressedButtonText;
                sprite.color = GameManager.instance.GetState().pressedButtonColor;
                break;

            case GameState.EXECUTING:

                GameManager.instance.fieldManager.DisableAllSlots();
                GameManager.instance.executeManager.currentCard.UseSelectedAbility();
                break;

            case GameState.BATTLING:
                break;
        }

        // general buttom clicks effects
        buttonGlow.SetActive(false);

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
