using FishNet.Component.Transforming.Beta;
using FishNet.Utility.Extension;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    Player player;

    [Header("refs")]
    public List<SpriteRenderer> stunnedPlaceholders;
    public GameObject UIobj;
    public TextMeshProUGUI playerName;

    [Header("Hearts")]
    public GameObject healthbar; // holds the hearts
    [SerializeField] PlayerHeart heartTemplate; // the first heart, copied until there are GameManager.startingHearts, the row is centered on it
    [SerializeField] float heartSpacing = 90f;
    readonly List<PlayerHeart> hearts = new List<PlayerHeart>();

    [Header("Stars")]
    [SerializeField] GameObject starsObj;
    [SerializeField] TextMeshProUGUI starsText;

    [Header("Heart break effects")]
    [SerializeField] float shakeLenght;
    [SerializeField] float shakeIntensity;
    [Header("Avatar")]
    [SerializeField] Image head;
    [SerializeField] Image face;
    [SerializeField] Color avatarColor;

    [Header("Food tokens")]
    public FoodToken[] foodCounters = new FoodToken[3];
    [SerializeField] GameObject foodObj;
    [SerializeField] float offsetX;
    public float tokenStayTime;
    public float tokenShowTime = 0.6f;
    Vector3 defaultTokenPos;
    Vector3 shownTokenPos;

    [Header("Card Count")]
    [SerializeField] TextMeshProUGUI cardCountText;
    [SerializeField] GameObject cardCountObj;

    [Header("Status effects")]
    [SerializeField] GameObject randomLaneIndicator;
    [SerializeField] GameObject mustPlaySpecialIndicator; // sits below the turn hint, so it hides together with it


    private void Start()
    {
        // getting reference for the player
        player = GetComponent<Player>();

        // Getting positions for food tokens UI positions
        defaultTokenPos = foodObj.transform.localPosition;
        shownTokenPos = (player.isOpponent) ? defaultTokenPos - new Vector3(offsetX, 0, 0) : defaultTokenPos + new Vector3(offsetX, 0, 0);

        SpawnHearts();
        Refresh();
    }

    /// <summary>
    /// Copies the heart until there are as many as GameManager.startingHearts, in a row centered where the first one was
    /// </summary>
    void SpawnHearts()
    {
        hearts.Add(heartTemplate);
        for (int i = 1; i < GameManager.instance.startingHearts; i++) hearts.Add(Instantiate(heartTemplate, heartTemplate.transform.parent));

        RectTransform templateRect = (RectTransform)heartTemplate.transform;
        Vector2 center = templateRect.anchoredPosition;
        for (int i = 0; i < hearts.Count; i++)
        {
            ((RectTransform)hearts[i].transform).anchoredPosition = center + new Vector2(heartSpacing * (i - (hearts.Count - 1) / 2f), 0f);
            hearts[i].SetFull(true);
        }
    }

    /// <summary>
    /// Updates the hearts (a heart that just got lost breaks) and the rest of the player UI
    /// </summary>
    public void Refresh()
    {
        // REFRESHING HEARTS
        // ==============
        // hearts are lost from the last one
        for (int i = 0; i < hearts.Count; i++)
        {
            bool full = i < player.health;
            if (hearts[i].IsFull && !full) HeartBreakEffect(hearts[i]);
            hearts[i].SetFull(full);
        }

        // REFRESHING CARD COUNT
        if (cardCountObj != null)
        {
            cardCountObj.SetActive(GameManager.instance.currentState == GameState.PLACING);
            cardCountText.text = player.cardsInHand.Count.ToString();
        }

        // REFRESHING STATUS EFFECTS
        if (randomLaneIndicator != null) randomLaneIndicator.SetActive(player.playsOnRandomLane);
        if (mustPlaySpecialIndicator != null) mustPlaySpecialIndicator.SetActive(player.mustPlaySpecial && player.GetSpecialInHand() != null);


        // REFRESHING STARS
        // ================
        starsObj.SetActive(player.currentStars > 0);

        // playing the animation if star value has changed
        //if (Convert.ToInt32(starsText.text) != player.currentStars) Animations.instance.PopAnim(starsObj, 0.3f, 0.45f);
        starsText.text = player.currentStars.ToString();
    } 

    /// <summary>
    /// Juice for losing a heart: the heart breaks in two (with its sound) and the hearts shake
    /// </summary>
    void HeartBreakEffect(PlayerHeart heart)
    {
        heart.Break(); // plays the sound when the heart shatters
        Animations.instance.ShakeAnim(healthbar, shakeLenght, shakeIntensity);
    }

    /// <summary>
    /// Add (or reduce) food tokens on the player
    /// </summary>
    /// <param name="type"></param>
    /// <param name="amount"></param>
    public void AddRandomFoodToken(bool showing = false)
    {
        // playing soundeffect
        AudioManager.instance.PlaySFX("GainResourceSFX");

        FoodType type = (FoodType)GameRandom.Range(0, Enum.GetNames(typeof(FoodType)).Length);
        player.food[(int)type]++;
        StartCoroutine(TokenUpdateAnim(type, showing));
    }
    public IEnumerator ShowTokens(bool show, bool instant = false)
    {
        Vector3 startingPosition = foodObj.transform.localPosition;
        Vector3 targetPosition = (show) ? shownTokenPos : defaultTokenPos;
        if (instant)
        {
            foodObj.transform.localPosition = targetPosition;
            yield break;
        }
        float t = 0;
        float timeAppearing = tokenShowTime;

        while (t < timeAppearing)
        {
            t += Time.deltaTime;
            float clampedT = t / timeAppearing;
            float coolT = (show) ? 1 - (1 - clampedT) * (1 - clampedT) : clampedT * clampedT; 

            foodObj.transform.localPosition = Vector3.Lerp(startingPosition, targetPosition, coolT);

            yield return null;
        }

        // snapping to correct position
        foodObj.transform.localPosition = targetPosition;
    }
    IEnumerator TokenUpdateAnim(FoodType type, bool showing)
    {
        if (showing) yield return StartCoroutine(ShowTokens(true));
        foodCounters[(int)type].RefreshToken(true);

        yield return new WaitForSeconds(tokenStayTime);
        if (showing) yield return StartCoroutine(ShowTokens(false));
    }
}
