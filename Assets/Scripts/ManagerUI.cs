using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ManagerUI : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] GameObject gameplayUI;
    public TextMeshProUGUI hintMessage;
    public TextMeshProUGUI turnHint;
    [SerializeField] Color yourTurnColor;
    [SerializeField] Color oppTurnColor;
    [SerializeField] GameObject turnMessageObj;
    [SerializeField] TextMeshProUGUI turnMessage;
    [SerializeField] GameObject workshopObj;
    public Workshop workshop;
    [SerializeField] TextMeshProUGUI stateTransitionText;
    public GameObject stateTransitionObj;
    [SerializeField] Image[] gameOverDoodles;

    [Header("card preview")]
    [SerializeField] Card previewCard;
    [SerializeField] Vector3 cardPreviewOffset = new Vector3(0f, 400f, 0f);

    [Header("game over")]
    [SerializeField] GameObject gameOverScreen;
    [SerializeField] float bgAlpha;
    [SerializeField] float fadeInTime;
    [SerializeField] Image gameOverBG;
    [SerializeField] TextMeshProUGUI gameOverText;
    [SerializeField] string wonText;
    [SerializeField] string lostText;

    /// <summary>
    /// Makes a readOnly card version above specific unit
    /// </summary>
    /// <param name="enable"></param>
    /// <param name="unit"></param>
    public void PreviewCard(bool enable, CreatureObj cardData, Player player, Vector3 position)
    {
        previewCard.gameObject.SetActive(enable);

        // Passing correct data to the card and positioning it above the unit
        if (!enable) return;
        previewCard.AssignCardData(cardData, player);
        previewCard.transform.position = Camera.main.WorldToScreenPoint(position);
        previewCard.transform.position += cardPreviewOffset;

        // If preview is away from the screen, move it down
        RectTransform cardRT = previewCard.GetComponent<RectTransform>();
        float cardTopY = cardRT.position.y + (1f - cardRT.pivot.y) * cardRT.rect.height;
        if (cardTopY - 50f > Screen.height) previewCard.transform.position -= cardPreviewOffset * 2f;
    }

    public void StateChangeMessage(string messageText)
    {
        stateTransitionObj.SetActive(true);
        stateTransitionText.text = messageText;
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
        // play soundeffect
        AudioManager.instance.PlaySFX("RerollSFX");

        // updating turn hint (top of the screen text)
        turnHint.color = (GameManager.instance.yourTurn) ? yourTurnColor : oppTurnColor;
        turnHint.text = (GameManager.instance.yourTurn) ? "YOUR TURN" : "OPPONENT'S TURN";

        // enabling turn message only if it is a placing phase
        if (GameManager.instance.currentState != GameState.PLACING) return;

        // enabling the turn message
        turnMessage.color = (GameManager.instance.yourTurn) ? yourTurnColor : oppTurnColor;
        turnMessage.text = (GameManager.instance.yourTurn) ? "YOUR TURN" : "OPPONENT'S TURN";
        turnMessageObj.SetActive(true);
    }

    public void GameOverScreen(Player lostPlayer)
    {
        Player winner = (lostPlayer == GameManager.instance.player) ? GameManager.instance.opponent : GameManager.instance.player;

        // creating game over text
        gameOverScreen.SetActive(true);
        gameOverText.text = winner.playerName + " won!";
        gameOverText.color = winner.playerColor;

        for (int i = 0; i < gameOverDoodles.Length && i < winner.cardsOnField.Count; i++)
        {
            // copying sprites of units to doodles
            gameOverDoodles[i].gameObject.SetActive(true);
            gameOverDoodles[i].sprite = winner.cardsOnField[i].cardData.unitSprite;
            gameOverDoodles[i].color = winner.playerColor;
        }
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }

    public void Rematch()
    {
        SceneManager.LoadScene("Board");
    }

    public void EnableWorkshop(bool isEnable, bool moveWorkshopBG = true)
    {
        if (isEnable) workshopObj.SetActive(true);
        StartCoroutine(workshop.WorkshopAnim(isEnable, moveWorkshopBG));
        hintMessage.gameObject.SetActive(!isEnable);
        turnHint.gameObject.SetActive(!isEnable);
        GameManager.instance.opponent.playerUI.UIobj.SetActive(!isEnable);
    }
}
