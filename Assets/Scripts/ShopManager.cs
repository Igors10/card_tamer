using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Data;
using Unity.VisualScripting;

public class ShopManager : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] Button[] buttons; // 0- up star; 1- draw card; 2- draw special
    TextMeshProUGUI[] prices;
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
        // player can't upgrade stars if there are more than 4
        starsAlreadyUpgraded = player.maxStars < 4;
        StartCoroutine(CreateShopStars());

        Refresh();
    }

    void Refresh()
    {
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
        prices[2].text = drawCardPrice.ToString();
        CheckButtonAvailability(2, drawSpecialPrice);

        // refreshing shopStars
        for (int i = 0; i < shopStars.Count; i++)
        {
            // star position
            float starX = starSpacing * (i - (shopStars.Count - 1) / 2f);
            shopStars[i].transform.localPosition = new Vector2(starX, starsObj.transform.position.y);

            // star transparency (used stars are half transparent
            if (player.shopStars < i)
            {
                shopStars[i].color = new Color(shopStars[i].color.r, shopStars[i].color.g, shopStars[i].color.b, 0.5f);
            }
        }
    }

    // SHOP BUTTONS
    // ===============
    void CheckButtonAvailability(int buttonID, int price)
    {
        // checking if player has enough stars
        bool available = price > player.shopStars;

        // disabling the button if player doesnt have enough
        buttons[buttonID].interactable = available;
        prices[buttonID].color = (available) ? Color.black : Color.red;
    }

    void Pay(Player playerToPay, int price)
    {
        playerToPay.shopStars -= price;
        if (playerToPay.shopStars < 0) playerToPay.shopStars = 0;
    }

    void UpgradeStars(Player playerToUpgrade)
    {
        // Upgrading stars
        playerToUpgrade.maxStars += 1;
        
        Refresh();
        playerToUpgrade.playerUI.Refresh();

        // Paying
        int starPrice = Convert.ToInt32(prices[0].text);
        Pay(playerToUpgrade, starPrice);
    }

    void DrawCard(Player playerToGetCard)
    {
        // Generating the card (temporary just random card)
        CardGenerator generator = GameManager.instance.cardGenerator;
        generator.CreateCard(generator.PickRandomCard(), playerToGetCard);

        // Paying
        int cardPrice = Convert.ToInt32(prices[1].text);
        Pay(playerToGetCard, cardPrice);
    }

    void DrawSpecial(Player playerToGetSpecial)
    {
        // Generating Special (temporary just random card)
        CardGenerator generator = GameManager.instance.cardGenerator;
        generator.CreateCard(generator.PickRandomCard(), playerToGetSpecial);

        // Paying 
        int specialPrice = Convert.ToInt32(prices[2].text);
        Pay(playerToGetSpecial, specialPrice);
    }

    // SHOP STARS (MONEY)
    // ==================

    private void Update()
    {
        if (Input.GetButtonDown("Fire")) skipStarAppearance = true;
    }

    IEnumerator CreateShopStars()
    {
        yield return null;

        player.shopStars = player.shopStars + player.deadUnitsThisRound + shopStarsAfterBattle;
        
        for (int i = 0; i < player.shopStars; i++)
        {
            Image newShopStar = Instantiate(shopStarPrefab, starsObj.transform.position, Quaternion.identity, starsObj.transform).GetComponent<Image>();
            shopStars.Add(newShopStar);
            newShopStar.color = (i < shopStarsAfterBattle) ? starColor : deadUnitsStarColor;
            Animations.instance.PopAnim(newShopStar.gameObject, 0.4f, 0.2f);

            Refresh();
            if (!skipStarAppearance) yield return new WaitForSeconds(starAppearIntervals);
        }
    }
}
