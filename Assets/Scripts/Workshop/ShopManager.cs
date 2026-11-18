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
    [SerializeField] Button[] buttons; // 0- up star; 1- draw card; 2- discard card; 3- draw special
    [SerializeField] TextMeshProUGUI[] prices;
    [SerializeField] ShopSticker[] stickerOffers; // random stickers for sale, new ones every round
    [SerializeField] Button stickerRefreshButton; // swaps all offered stickers for new ones
    [SerializeField] TextMeshProUGUI stickerRefreshPriceText;
    [SerializeField] TextMeshProUGUI nextStarAmountText;
    [SerializeField] Player player;
    [SerializeField] Workshop workshop;

    [Header("prices")]
    public int drawCardPrice;
    public int drawSpecialPrice = 4;
    public int stickerPrice = 2;
    [SerializeField] int freeStickers = 1; // the first sticker purchases of every round are free (for the bot too)
    public int stickerRefreshPrice = 1;
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
    bool stickersOffered; // this round's stickers are already rolled
    bool shopLoaded; // buttons are shown after the intro text and the stars

    private void Start()
    {
        // adding functionality to buttons
        if (buttons[0] != null) buttons[0].onClick.AddListener(() => UpgradeStars(player)); // star upgrade button isn't in the shop right now
        buttons[1].onClick.AddListener(() => DrawCard(player));
        buttons[2].onClick.AddListener(() => DiscardCard(player));
        buttons[3].onClick.AddListener(() => DrawSpecial(player));
        foreach (ShopSticker offer in stickerOffers) offer.Button.onClick.AddListener(() => BuySticker(player, offer));
        stickerRefreshButton.onClick.AddListener(() => RefreshStickers(player));
    }

    private void OnEnable()
    {
        Debug.Log("ShopManager: OnEnable triggers");

        // new stickers for sale every round
        if (!stickersOffered)
        {
            RollStickers();
            stickersOffered = true;
        }

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
        stickersOffered = false;
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

        // with a full hand the draw card button is swapped for discarding a card for a star
        bool handFull = player.cardsInHand.Count >= GameManager.instance.maxHandSize;

        // refreshing draw card button
        prices[1].text = drawCardPrice.ToString();
        CheckButtonAvailability(1, drawCardPrice);
        buttons[1].gameObject.SetActive(shopLoaded && !handFull);

        // refreshing draw special button (only one special card per player)
        prices[3].text = drawSpecialPrice.ToString();
        CheckButtonAvailability(3, drawSpecialPrice);
        if (player.HasSpecialCard()) buttons[3].interactable = false;

        // refreshing discard button (only shown with a full hand)
        buttons[2].gameObject.SetActive(shopLoaded && handFull);
        buttons[2].interactable = player.cardsInHand.Count > 1;

        // refreshing the offered stickers (free ones cost 0, they need a card with room for another sticker)
        int stickerPriceNow = StickerPriceFor(player);
        bool cardHasRoom = player.cardsInHand.Exists(card => card.cardData.CanAddSticker);
        foreach (ShopSticker offer in stickerOffers)
        {
            offer.gameObject.SetActive(shopLoaded && offer.Offered != null);
            offer.PriceText.text = stickerPriceNow.ToString();
            bool affordable = stickerPriceNow <= player.shopStars;
            offer.PriceText.color = affordable ? Color.black : Color.red;
            offer.Button.interactable = affordable && cardHasRoom;
        }

        // refreshing the sticker refresh button
        stickerRefreshButton.gameObject.SetActive(shopLoaded);
        stickerRefreshPriceText.text = stickerRefreshPrice.ToString();
        bool canRefresh = stickerRefreshPrice <= player.shopStars;
        stickerRefreshPriceText.color = canRefresh ? Color.black : Color.red;
        stickerRefreshButton.interactable = canRefresh;
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

    /// <summary>
    /// Offered sticker clicked: it's paid and leaves the shop, then the player clicks a card in hand to put it on
    /// </summary>
    void BuySticker(Player playerToGetSticker, ShopSticker offer)
    {
        Sticker sticker = offer.Offered;
        if (sticker == null) return;

        offer.Show(null);
        Pay(playerToGetSticker, TakeStickerPrice(playerToGetSticker));
        GameManager.instance.managerUI.workshop.ChooseCardForSticker(sticker);
    }

    /// <summary>
    /// Every offer slot gets a new random sticker
    /// </summary>
    void RollStickers()
    {
        List<Sticker> stickers = RollStickerOffers();
        for (int i = 0; i < stickerOffers.Length; i++) stickerOffers[i].Show(stickers[i]);
    }

    /// <summary>
    /// Random stickers for the offer slots (the bot uses its own), different ones while the pool has enough
    /// </summary>
    public List<Sticker> RollStickerOffers()
    {
        CardGenerator generator = GameManager.instance.cardGenerator;
        List<Sticker> stickers = new List<Sticker>();
        for (int i = 0; i < stickerOffers.Length; i++)
        {
            Sticker sticker = generator.PickRandomSticker("basic");
            for (int tries = 0; tries < 20 && stickers.Exists(other => other.ability == sticker.ability); tries++) sticker = generator.PickRandomSticker("basic");
            stickers.Add(sticker);
        }
        return stickers;
    }

    void RefreshStickers(Player playerToPay)
    {
        RollStickers();
        foreach (ShopSticker offer in stickerOffers) Animations.instance.PopAnim(offer.gameObject, 0.3f, 0.15f);
        AudioManager.instance.PlaySFX("RerollSFX");
        Pay(playerToPay, stickerRefreshPrice);
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
        shopLoaded = false;
        for (int a = 0; a < buttons.Count(); a++)
        {
            if (buttons[a] != null) buttons[a].gameObject.SetActive(false);
        }
        foreach (ShopSticker offer in stickerOffers) offer.gameObject.SetActive(false);
        stickerRefreshButton.gameObject.SetActive(false);

        // pause while intro text is on the screen
        while (GameManager.instance.managerUI.stateTransitionObj.activeSelf)
        {
            yield return null;
        }

        // enabling buttons back (draw card / discard, sticker offers and their refresh button are shown by Refresh)
        shopLoaded = true;
        for (int a = 0; a < buttons.Count(); a++)
        {
            if (a == 0) continue; // skip the star button (temp solution)
            if (a == 1 || a == 2) continue;

            buttons[a].gameObject.SetActive(true);
        }
        Refresh();

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
