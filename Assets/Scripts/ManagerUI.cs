using UnityEngine;
using TMPro;

public class ManagerUI : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] GameObject gameplayUI;
    [SerializeField] TextMeshProUGUI hintMessage;
    [SerializeField] TextMeshProUGUI turnMessage;
    [SerializeField] Color yourTurnColor;
    [SerializeField] Color oppTurnColor;

    [Header("card preview")]
    [SerializeField] Card previewCard;
    [SerializeField] Vector3 cardPreviewOffset = new Vector3(0f, 400f, 0f);

    /// <summary>
    /// Makes a readOnly card version above specific unit
    /// </summary>
    /// <param name="enable"></param>
    /// <param name="unit"></param>
    public void PreviewCard(bool enable, Unit unit)
    {
        previewCard.gameObject.SetActive(enable);

        // Passing correct data to the card and positioning it above the unit
        if (!enable) return;
        previewCard.AssignCardData(unit.card.cardData, unit.card.player);
        previewCard.transform.position = Camera.main.WorldToScreenPoint(unit.transform.position);
        previewCard.transform.position += cardPreviewOffset;
    }


    /// <summary>
    /// Enabling or disabling all in-game interaction UI
    /// </summary>
    /// <param name="enable"></param>
    public void EnableUI(bool enable)
    {
        gameplayUI.SetActive(enable);
        GameManager.instance.gameStateUI[(int)GameManager.instance.currentState].SetActive(enable);
    }

    /// <summary>
    /// Changes hint text on the top of UI
    /// </summary>
    /// <param name="hintText"></param>
    public void NewHint(string hintText)
    {
        hintMessage.text = hintText;
    }

    public void UpdateTurnMessage()
    {
        turnMessage.color = (GameManager.instance.yourTurn) ? yourTurnColor : oppTurnColor;
        turnMessage.text = (GameManager.instance.yourTurn) ? "YOUR TURN" : "OPPONENT'S TURN";
    }
}
