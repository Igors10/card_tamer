using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Data;
using Unity.VisualScripting;
using System.Diagnostics.CodeAnalysis;

public class ShopManager : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] Button[] buttons; // 0- up star; 1- draw card; 2- discard card; 3- draw special; 4- add sticker
    [SerializeField] TextMeshProUGUI[] prices;
    [SerializeField] TextMeshProUGUI nextStarAmountText;
    [SerializeField] Player player;
    [SerializeField] Workshop workshop;

    [Header("prices")]
    public int drawCardPrice;
    public int drawSpecialPrice = 4;
    public int stickerPrice = 2; // placeholder until the sticker cost is decided
    [SerializeField] int freeStickers = 1; // the first sticker purchases of every round are free (for the bot too)
    public int discardReward;
    // upgrade price is current max star amount + 1

    [Header("shop stars")]
    public int shopStarsAfterBattle;
    [SerializeField] GameObject shopStarPrefab;
    [SerializeField] Color starColor;
    [SerializeField] Color deadUnitsStarColor;
    [SerializeField] GameObject starsObj;
    [SerializeField] float starSpacing;
    [SerializeField] float starAppearIntervals;
    List<Image> shopStars = new List<Image>();
    [HideInInspector] public bool skipStarAppearance = false;
    // you stopped here - make stars appear

    bool starsAlreadyUpgraded;
    [HideInInspector] public bool cardDisarded = false;

    private void Start()
    {
        // adding functionality to buttons
        buttons[0].onClick.AddListener(() => UpgradeStars(player));
        buttons[1].onClick.AddListener(() => DrawCard(player));
        buttons[2].onClick.AddListener(() => DiscardCard(player));
        buttons[3].onClick.AddListener(() => DrawSpecial(player));
        buttons[4].onClick.AddListener(() => BuySticker(player));
    }

    private void OnEnable()
    {
        Debug.Log("ShopManager: OnEnable triggers");

        StartCoroutine(CreateShopStars());

        // if no card was discarded enable auto discard
        //StartCoroutine(GameManager.instance.discardManager.StartDiscardSequence(GameManager.instance.player, true));

        Refresh();
    }

    private void OnDisable()
    {
        DiscardSpendStars();
    }

    public void ResetShopValues()
    {
        skipStarAppearance = false;
        starsAlreadyUpgraded = false;
        cardDisarded = false;
    }

    public void Refresh(bool onlyRefreshStars = false)
    {
        // refreshing shopStars
        for (int i = 0; i < shopStars.Count; i++)
        {
            // star position
            float starX = starSpacing * (i - (shopStars.Count - 1) / 2f);
            shopStars[i].transform.localPosition = new Vector2(starX, 0f);
        }

        if (onlyRefreshStars) return;

        // refreshing draw card button
        prices[1].text = drawCardPrice.ToString();
        CheckButtonAvailability(1, drawCardPrice);

        // refreshing draw special button (only one special card per player)
        prices[3].text = drawSpecialPrice.ToString();
        CheckButtonAvailability(3, drawSpecialPrice);
        if (player.HasSpecialCard()) buttons[3].interactable = false;

        // refreshing add sticker button (needs a card with room for another sticker)
        // free ones cost 0
        prices[4].text = StickerPriceFor(player).ToString();
        CheckButtonAvailability(4, StickerPriceFor(player));
        if (!player.cardsInHand.Exists(card => card.cardData.CanAddSticker)) buttons[4].interactable = false;

        // refreshing discard button
        buttons[2].interactable = player.cardsInHand.Count > 1;
        //prices[2].text = "+ " + discardReward.ToString();
        //CheckButtonAvailability(2, discardReward);
    }

    // SHOP BUTTONS
    // ===============
    void CheckButtonAvailability(int buttonID, int price)
    {
        // checking if player has enough stars
        bool available = price <= player.shopStars;

        // disabling the button if player doesnt have enough
        buttons[buttonID].interactable = available;
        prices[buttonID].color = (available) ? Color.black : Color.red;
    }

    void Pay(Player playerToPay, int price)
    {
        playerToPay.shopStars -= price;
        if (playerToPay.shopStars < 0) playerToPay.shopStars = 0;
        Debug.Log("ShopManager: player pays " + price + " stars; player has " + playerToPay.shopStars + " stars left");

        for (int i = 0; i < price; i++)
        {
            if (shopStars.Count < 1) break;
            Image nextShopStar = shopStars[0]; 
            shopStars.Remove(nextShopStar);
            Destroy(nextShopStar.gameObject);
        }

        Refresh();
    }

    void UpgradeStars(Player playerToUpgrade)
    {
        // Upgrading stars
        playerToUpgrade.maxStars += 1;
        playerToUpgrade.currentStars = playerToUpgrade.maxStars;
        playerToUpgrade.playerUI.Refresh();
        starsAlreadyUpgraded = true;

        // Paying
        int starPrice = Convert.ToInt32(prices[0].text);
        Pay(playerToUpgrade, starPrice);
    }

    void DrawCard(Player playerToGetCard)
    {
        GameManager.instance.managerUI.workshop.AbilityOptions();
        
        // Paying
        int cardPrice = Convert.ToInt32(prices[1].text);
        Pay(playerToGetCard, cardPrice);
    }

    void DrawSpecial(Player playerToGetSpecial)
    {
        if (playerToGetSpecial.HasSpecialCard()) return;

        GameManager.instance.managerUI.workshop.AbilityOptions(true);

        // Paying
        Pay(playerToGetSpecial, drawSpecialPrice);

        // Rat explaining special cards
        if (RatSmart.instance.specialExplained == false)
        {
            RatSmart.instance.StartCoroutine(RatSmart.instance.PlayDialogue("SpecialDial")); // on the rat, the shop is hidden by now
            RatSmart.instance.specialExplained = true;
        }
    }

    /// <summary>
    /// Sticker price for this player: 0 while they still have free ones
    /// </summary>
    public int StickerPriceFor(Player p) => (p.freeStickersUsed < freeStickers) ? 0 : stickerPrice;

    /// <summary>
    /// Price of the sticker the player is buying now (uses up a free one if there is one left)
    /// </summary>
    public int TakeStickerPrice(Player p)
    {
        int price = StickerPriceFor(p);
        if (price == 0) p.freeStickersUsed++;
        return price;
    }

    void BuySticker(Player playerToGetSticker)
    {
        GameManager.instance.managerUI.workshop.StickerOptions();
        Pay(playerToGetSticker, TakeStickerPrice(playerToGetSticker));
    }

    void DiscardCard(Player playerToDiscard)
    {
        StartCoroutine(GameManager.instance.discardManager.StartDiscardSequence(playerToDiscard));
    }

    // SHOP STARS (MONEY)
    // ==================

    private void Update()
    {
        if (Input.GetButtonDown("Fire1")) { skipStarAppearance = true; Debug.Log("ShopManager: star appearance anim skipped"); }
    }

    IEnumerator CreateShopStars()
    {
        // if stars were already created don't make more
        if (skipStarAppearance) yield break;

        // disabling buttons before workshop fully loaded
        for (int a = 0; a < buttons.Count(); a++)
        {
            buttons[a].gameObject.SetActive(false);
        }

        // pause while intro text is on the screen
        while (GameManager.instance.managerUI.stateTransitionObj.activeSelf)
        {
            yield return null;
        }

        // enabling buttons back
        for (int a = 0; a < buttons.Count(); a++)
        {
            if (a == 0) continue; // skip the star button (temp solution)

            buttons[a].gameObject.SetActive(true);
        }

        // calculating player's shop stars
        int newStars = shopStarsAfterBattle;
        //player.shopStars += newStars;

        Debug.Log("ShopManager: Player has " + player.shopStars + " shop stars");
        
        for (int i = 0; i < newStars; i++)
        {
            AddShopStar();

            if (skipStarAppearance) starAppearIntervals = 0.05f; 
            yield return new WaitForSeconds(starAppearIntervals);
        }

        // CHAT WITH RAT
        // shop tutorial
        if (RatSmart.instance.shopExplained == false)
        {
            yield return RatSmart.instance.PlayDialogue("ShopExplainDial");
            RatSmart.instance.shopExplained = true;
        }
        // adding the star if player lost previous round
        if (GameManager.instance.battleManager.lostCurrentRound)
        {
            yield return RatSmart.instance.PlayDialogue("PlusStarDial");
            AddShopStar();
        }

        // a star for every card the opponent's Beast destroyed
        for (; player.compensationStars > 0; player.compensationStars--)
        {
            AddShopStar();
            yield return new WaitForSeconds(starAppearIntervals);
        }

        Refresh();
    }

    // used by effects that fly a star into the counter before adding it
    public Sprite ShopStarSprite => shopStarPrefab.GetComponent<Image>().sprite;
    public Color ShopStarColor => starColor;
    public Vector3 ShopStarsPosition => starsObj.transform.position;

    /// <summary>
    /// Adds one shop star
    /// </summary>
    public void AddShopStar()
    {
        // creating the star
        Image newShopStar = Instantiate(shopStarPrefab, starsObj.transform.position, Quaternion.identity, starsObj.transform).GetComponent<Image>();
        shopStars.Add(newShopStar);
        newShopStar.color = starColor;
        Animations.instance.PopAnim(newShopStar.gameObject, 0.4f, 0.2f);
        Debug.Log("ShopManager: new shop star created");

        // updating player's star count
        player.shopStars++;

        // playing sound effect
        AudioManager.instance.PlaySFX("ShopStarSFX");

        Refresh(true);
    }

    /// <summary>
    ///  Removes all stars that were spend
    /// </summary>
    void DiscardSpendStars()
    {
        /*
        List<Image> shopStarsToRemove = new List<Image>();
        foreach (Image shopStar in shopStars.Where(r => r.color.a < 1))
        {
            shopStarsToRemove.Add(shopStar);
        }

        for (int i = 0; i < shopStarsToRemove.Count; i++)
        {
            shopStars.Remove(shopStarsToRemove[i]);
            Destroy(shopStarsToRemove[i]);
        }*/
    }
}
