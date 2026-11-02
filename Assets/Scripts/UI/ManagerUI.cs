using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

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
    [SerializeField] UnitSprite[] gameOverDoodles;

    [Header("card preview")]
    [SerializeField] Card previewCard;
    [SerializeField] Vector3 cardPreviewOffset = new Vector3(0f, 400f, 0f);
    [SerializeField] float zoomScale = 2f; // right click zoom size

    [Header("game over")]
    [SerializeField] GameObject gameOverScreen;
    [SerializeField] float bgAlpha;
    [SerializeField] float fadeInTime;
    [SerializeField] Image gameOverBG;
    [SerializeField] TextMeshProUGUI gameOverText;
    [SerializeField] string lostText;
    [SerializeField] GameObject rematchButton;

    /// <summary>
    /// Makes a readOnly card version above specific unit
    /// </summary>
    /// <param name="enable"></param>
    /// <param name="unit"></param>
    public void PreviewCard(bool enable, CreatureObj cardData, Player player, Vector3 position, bool worldToScreen = true)
    {
        previewCard.gameObject.SetActive(enable);
        ZoomedCard = null;

        // Passing correct data to the card and positioning it above the unit
        if (!enable) return;
        if (previewRestScale != Vector3.zero) previewCard.transform.localScale = previewRestScale; // right click zoom made it bigger
        previewCard.AssignCardData(cardData, player);
        Vector3 previewPos = (worldToScreen) ? Camera.main.WorldToScreenPoint(position) : position;
        previewCard.transform.position = previewPos;
        previewCard.transform.position += cardPreviewOffset;

        // If preview is away from the screen, move it down
        RectTransform cardRT = previewCard.GetComponent<RectTransform>();
        float cardTopY = cardRT.position.y + (1f - cardRT.pivot.y) * cardRT.rect.height;
        if (cardTopY - 50f > Screen.height) previewCard.transform.position -= cardPreviewOffset * 2f;
    }

    /// <summary>
    /// Right click zoom: the preview card shows a big copy of the card in the middle of the screen, null hides it
    /// </summary>
    public void ZoomCard(Card card)
    {
        ZoomedCard = card;
        previewCard.gameObject.SetActive(card != null);
        if (card == null) return;

        // the zoomed copy never catches the mouse, so the card under it keeps its hover
        CanvasGroup group = previewCard.GetComponent<CanvasGroup>();
        if (group == null) group = previewCard.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        if (previewRestScale == Vector3.zero) previewRestScale = previewCard.transform.localScale;
        previewCard.AssignCardData(card.cardData, card.player);
        previewCard.transform.position = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        previewCard.transform.localScale = previewRestScale * zoomScale;
        previewCard.transform.SetAsLastSibling();
    }

    public Card ZoomedCard { get; private set; }
    Vector3 previewRestScale;

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

    public void Refresh()
    {
        GameManager.instance.player.playerUI.Refresh();
        GameManager.instance.opponent.playerUI.Refresh();
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

        // updating turn hint (top of the screen text), only the battle has turns
        turnHint.gameObject.SetActive(GameManager.instance.currentState == GameState.BATTLING);
        turnHint.color = (GameManager.instance.yourTurn) ? yourTurnColor : oppTurnColor;
        turnHint.text = (GameManager.instance.yourTurn) ? "YOUR TURN" : "OPPONENT'S TURN";

        // enabling turn message only in the battle (players take turns attacking)
        if (GameManager.instance.currentState != GameState.BATTLING) return;

        // enabling the turn message
        turnMessage.color = (GameManager.instance.yourTurn) ? yourTurnColor : oppTurnColor;
        turnMessage.text = (GameManager.instance.yourTurn) ? "YOUR TURN" : "OPPONENT'S TURN";
        turnMessageObj.SetActive(true);
    }

    public void GameOverScreen(Player lostPlayer, string customText = null)
    {
        // everything is shown from the local player's side
        Player localPlayer = GameManager.instance.player;

        // creating game over text
        gameOverScreen.SetActive(true);
        gameOverText.text = customText ?? ((lostPlayer == localPlayer) ? lostText : localPlayer.playerName + " won!");
        gameOverText.color = localPlayer.playerColor;

        // online only the host restarts (for both players), and only while the opponent is still there
        if (!GameManager.instance.playerConfig.offlineMatch) EnableRematch(ConnectionScript.connectionManager.networkManager.IsServerStarted && !GameManager.instance.opponentLeft);

        for (int i = 0; i < gameOverDoodles.Length && i < localPlayer.cardsOnField.Count; i++)
        {
            // copying sprites of units to doodles
            gameOverDoodles[i].gameObject.SetActive(true);
            gameOverDoodles[i].RefreshSprite(localPlayer.cardsOnField[i].cardData.unitSprite, localPlayer.playerColor, localPlayer.cardsOnField[i].cardData.secondaryColor);
        }
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }

    public void Rematch()
    {
        // online the host's server loads a new Board for both players
        if (!GameManager.instance.playerConfig.offlineMatch)
        {
            EnableRematch(false); // no second click
            ConnectionScript.connectionManager.RestartMatch();
            return;
        }

        SceneManager.LoadScene("Board");
    }

    public void EnableRematch(bool enable)
    {
        rematchButton.SetActive(enable);
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
