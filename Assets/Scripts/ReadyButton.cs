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
        //UpdateButtonState();
    }

    public void UpdateButtonState(string customButtonText = null)
    {
        Debug.Log("Ready button text was updated to " + customButtonText);

        button.gameObject.SetActive(true);
        button.interactable = true;
        buttonText.text = (customButtonText == null) ? GameManager.instance.GetState().buttonText : customButtonText;
        sprite.color = GameManager.instance.GetState().buttonColor;
    }

    public void ButtonClick()
    {
        // playing soundeffect
        AudioManager.instance.PlaySFX("ButtonSFX");

        // game state specific button click effects
        switch (GameManager.instance.currentState)
        {
            // PLACING AND PLANNING
            default:

                if (GameManager.instance.executeManager.currentCard != null)
                {
                    GameManager.instance.executeManager.currentCard.UseSelectedAbility();
                    return;
                }
                else
                {
                    GameManager.instance.player.endStateReady = true;
                    button.interactable = false;
                    buttonText.text = GameManager.instance.GetState().pressedButtonText;
                }
                break;

            case GameState.BATTLING:
                break;

            case GameState.BUYING:
                GameManager.instance.player.endStateReady = true;
                if (GameManager.instance.opponent.isAI) GameManager.instance.opponent.endStateReady = true;
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
