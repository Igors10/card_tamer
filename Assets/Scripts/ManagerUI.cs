using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEditor.ShaderGraph.Internal;

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
    public void PreviewCard(bool enable, Unit unit)
    {
        previewCard.gameObject.SetActive(enable);

        // Passing correct data to the card and positioning it above the unit
        if (!enable) return;
        previewCard.AssignCardData(unit.card.cardData, unit.card.player);
        previewCard.transform.position = Camera.main.WorldToScreenPoint(unit.transform.position);
        previewCard.transform.position += cardPreviewOffset;

        // If preview is away from the screen, move it down
        RectTransform cardRT = previewCard.GetComponent<RectTransform>();
        float cardTopY = cardRT.position.y + (1f - cardRT.pivot.y) * cardRT.rect.height;
        if (cardTopY > Screen.height) previewCard.transform.position -= cardPreviewOffset * 2f;
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

    public void GameOverScreen(Player lostPlayer)
    {
        gameOverScreen.SetActive(true);
        gameOverText.text = (lostPlayer == GameManager.instance.player) ? lostText : wonText;

        StartCoroutine(GameOverFadeIn());
    }

    IEnumerator GameOverFadeIn()
    {
        float t = 0;

        // background color
        Color startingBGColor = new Color(gameOverBG.color.r, gameOverBG.color.g, gameOverBG.color.b, 0f);
        Color targetBGColor = new Color(gameOverBG.color.r, gameOverBG.color.g, gameOverBG.color.b, bgAlpha);

        // text color
        Color startingTextColor = new Color(gameOverText.color.r, gameOverText.color.g, gameOverText.color.b, 0f);
        Color targetTextColor = gameOverText.color;

        while (t < fadeInTime)
        {
            t += Time.deltaTime;
            float actualT = t / fadeInTime;
            float coolT = actualT * actualT;

            gameOverBG.color = Color.Lerp(startingBGColor, targetBGColor, coolT);
            gameOverText.color = Color.Lerp(startingTextColor, targetTextColor, coolT);

            yield return null;
        }
    }


}
