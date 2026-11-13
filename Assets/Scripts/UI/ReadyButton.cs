using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class ReadyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("refs")]
    public TextMeshProUGUI buttonText;
    public Button button;
    [SerializeField] Image sprite;
    [SerializeField] GameObject buttonGlow;

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

        // general buttom clicks effects
        buttonGlow.SetActive(false);

        // the cards are arranged
        if (GameManager.instance.currentState == GameState.PLACING) RatBuddy.instance.ClearHint("ArrangeHint", "ArrangeProtectHint", "ArrangeBuffHint");

        // online the press goes to the server first, both games apply it when it comes back (MatchNetwork)
        if (!GameManager.instance.playerConfig.offlineMatch)
        {
            button.interactable = false;

            // the field order was only changed here, the other game gets it before the Ready
            if (GameManager.instance.currentState == GameState.PLACING) MatchNetwork.instance.SendArrange(GameManager.instance.player);
            MatchNetwork.instance.SendReady();
            return;
        }

        // bot finishes the workshop together with the player
        if (GameManager.instance.currentState == GameState.BUYING && GameManager.instance.opponent.isAI) GameManager.instance.opponent.endStateReady = true;

        // ending the turn
        GameManager.instance.PlayerReady(GameManager.instance.player);
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
