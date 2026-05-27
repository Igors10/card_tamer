using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Data;
using Unity.VisualScripting;

public class ShopManager : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] Button[] buttons; // 0- up star; 1- draw card; 2- draw special
    [SerializeField] TextMeshProUGUI[] prices;
    [SerializeField] TextMeshProUGUI nextStarAmountText;
    [SerializeField] Player player;

    [Header("prices")]
    [SerializeField] int drawCardPrice;
    [SerializeField] int drawSpecialPrice;
    // upgrade price is current max star amount + 1

    [Header("shop stars")]
    [SerializeField] int shopStarsAfterBattle;
    [SerializeField] GameObject shopStarPrefab;
    [SerializeField] Color starColor;
    [SerializeField] Color deadUnitsStarColor;
    [SerializeField] GameObject starsObj;
    [SerializeField] float starSpacing;
    [SerializeField] float starAppearIntervals;
    List<Image> shopStars = new List<Image>();
    bool skipStarAppearance = false;
    // you stopped here - make stars appear

    bool starsAlreadyUpgraded;

    private void Start()
    {
        // adding functionality to buttons
        buttons[0].onClick.AddListener(() => UpgradeStars(player));
        buttons[1].onClick.AddListener(() => DrawCard(player));
        buttons[2].onClick.AddListener(() => DrawSpecial(player));
    }

    private void OnEnable()
    {
        Debug.Log("ShopManager: OnEnable triggers");

        // player can't upgrade stars if there are more than 4
        starsAlreadyUpgraded = player.maxStars >= 4;
        StartCoroutine(CreateShopStars());

        Refresh();
    }

    private void OnDisable()
    {
        DiscardSpendStars();
    }

    void Refresh(bool onlyRefreshStars = false)
    {
        // refreshing shopStars
        for (int i = 0; i < shopStars.Count; i++)
        {
            // star position
            float starX = starSpacing * (i - (shopStars.Count - 1) / 2f);
            shopStars[i].transform.localPosition = new Vector2(starX, 0f);

            // star transparency (used stars are half transparent
            if (player.shopStars <= i)
            {
                shopStars[i].color = new Color(shopStars[i].color.r, shopStars[i].color.g, shopStars[i].color.b, 0.5f);
            }
        }

        if (onlyRefreshStars) return;

        // refreshing up star button
        int nextStarAmount = player.maxStars + 1;
        prices[0].text = nextStarAmount.ToString();
        nextStarAmountText.text = "Up Star (" + nextStarAmount.ToString() +")";
        CheckButtonAvailability(0, nextStarAmount);
        if (starsAlreadyUpgraded) buttons[0].interactable = false;

        // refreshing draw card button
        prices[1].text = drawCardPrice.ToString();
        CheckButtonAvailability(1, drawCardPrice);

        // refreshing draw special button
        prices[2].text = drawSpecialPrice.ToString();
        CheckButtonAvailability(2, drawSpecialPrice);
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

        Refresh();
    }

    void UpgradeStars(Player playerToUpgrade)
    {
        // Upgrading stars
        playerToUpgrade.maxStars += 1;
        playerToUpgrade.playerUI.Refresh();

        // Paying
        int starPrice = Convert.ToInt32(prices[0].text);
        Pay(playerToUpgrade, starPrice);
    }

    void DrawCard(Player playerToGetCard)
    {
        // Generating the card (temporary just random card)
        CardGenerator generator = GameManager.instance.cardGenerator;
        generator.CreateCard(generator.PickRandomCard("basic"), playerToGetCard);

        // Paying
        int cardPrice = Convert.ToInt32(prices[1].text);
        Pay(playerToGetCard, cardPrice);
    }

    void DrawSpecial(Player playerToGetSpecial)
    {
        // Generating Special (temporary just random card)
        CardGenerator generator = GameManager.instance.cardGenerator;
        generator.CreateCard(generator.PickRandomCard("special"), playerToGetSpecial);

        // Paying 
        int specialPrice = Convert.ToInt32(prices[2].text);
        Pay(playerToGetSpecial, specialPrice);
    }

    // SHOP STARS (MONEY)
    // ==================

    private void Update()
    {
        if (Input.GetButtonDown("Fire1")) { skipStarAppearance = true; Debug.Log("ShopManager: star appearance anim skipped"); }
    }

    IEnumerator CreateShopStars()
    {
        // calculating player's shop stars
        int newStars = player.deadUnitsThisRound + shopStarsAfterBattle;
        player.shopStars += newStars;
        player.deadUnitsThisRound = 0;

        Debug.Log("ShopManager: Player has " + player.shopStars + " shop stars");
        
        for (int i = 0; i < newStars; i++)
        {
            Image newShopStar = Instantiate(shopStarPrefab, starsObj.transform.position, Quaternion.identity, starsObj.transform).GetComponent<Image>();
            shopStars.Add(newShopStar);
            newShopStar.color = (i < shopStarsAfterBattle) ? starColor : deadUnitsStarColor;
            Animations.instance.PopAnim(newShopStar.gameObject, 0.4f, 0.2f);
            Debug.Log("ShopManager: new shop star created");

            // playing sound effect
            AudioManager.instance.PlaySFX("ShopStarSFX");

            Refresh(true);
            if (skipStarAppearance) starAppearIntervals = 0.05f; 
            yield return new WaitForSeconds(starAppearIntervals);
        }
        Refresh();
    }

    /// <summary>
    ///  Removes all stars that were spend
    /// </summary>
    void DiscardSpendStars()
    {
        List<Image> shopStarsToRemove = new List<Image>();
        foreach (Image shopStar in shopStars.Where(r => r.color.a < 1))
        {
            shopStarsToRemove.Add(shopStar);
        }

        for (int i = 0; i < shopStarsToRemove.Count; i++)
        {
            shopStars.Remove(shopStarsToRemove[i]);
            Destroy(shopStarsToRemove[i]);
        }
    }
}
