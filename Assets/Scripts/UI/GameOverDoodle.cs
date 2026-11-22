using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// One of your cards on the game over screen: its drawing with a stat line under it
/// </summary>
public class GameOverDoodle : MonoBehaviour, IPointerClickHandler
{
    [Header("refs")]
    public UnitSprite unitSprite;
    [SerializeField] TextMeshProUGUI statText;
    [SerializeField] CanvasGroup statGroup; // fades the stat in (the screen's AutoFade only fades the text color)

    public System.Action<GameOverDoodle> onClick;
    Coroutine fade;

    public void Show(Card card, string stat)
    {
        gameObject.SetActive(true);
        unitSprite.RefreshSprite(card.cardData.unitSprite, card.player.playerColor, card.cardData.secondaryColor);
        statText.text = stat;
        statGroup.alpha = 0f;
    }

    /// <summary>
    /// Fades the stat line in or out after the delay
    /// </summary>
    public void ShowStat(bool show, float delay, float fadeTime)
    {
        if (fade != null) StopCoroutine(fade);
        fade = StartCoroutine(FadeStat(show ? 1f : 0f, delay, fadeTime));
    }

    IEnumerator FadeStat(float target, float delay, float fadeTime)
    {
        yield return new WaitForSeconds(delay);
        float start = statGroup.alpha;
        float t = 0;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            statGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / fadeTime));
            yield return null;
        }
        statGroup.alpha = target;
    }

    public void OnPointerClick(PointerEventData eventData) => onClick?.Invoke(this);
}
