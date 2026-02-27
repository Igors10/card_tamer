using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [Header("refs")]
    public ShopSlot[] shopSlots;

    [Header("rerolling")]
    [SerializeField] Button rerollButton;
    [SerializeField] TextMeshProUGUI rerollText;
    [SerializeField] Image rerollPriceImage;
    FoodType rerollPrice = FoodType.BERRIES;
    bool freeReroll = true;

    public void EnableRerollButton(bool isEnable)
    {
        rerollButton.interactable = isEnable;
    }

    public void ResetReroll()
    {
        freeReroll = true;
        rerollText.text = "";
        rerollPriceImage.GetComponent<Image>().enabled = false;
    }

    /// <summary>
    /// Checks if player has all the needed resources and randomizes shop content if they do (returns true if they do)
    /// </summary>
    /// <param name="roller"></param>
    /// <returns></returns>
    public bool RerollShop(Player roller)
    {
        // do checks if player has enough food and change random food when used
        if (!freeReroll)
        {
            if (roller.food[(int)rerollPrice] > 0) roller.playerUI.foodCounters[(int)rerollPrice].AddFood(-1);
            else
            {
                // starting red blinking animation for resources
                StartCoroutine(roller.playerUI.foodCounters[(int)rerollPrice].NegativeBlink());

                // play soundeffect

                return false;
            }
        }

        // Deciding on new price resource
        int randomFoodType = 0;
        do
        {
            randomFoodType = UnityEngine.Random.Range(0, Enum.GetNames(typeof(FoodType)).Length);
        } while (randomFoodType == (int)rerollPrice);
        rerollPriceImage.GetComponent<Image>().enabled = true;
        rerollPriceImage.sprite = shopSlots[0].foodSprites[randomFoodType];
        rerollText.text = "";
        rerollPrice = (FoodType)randomFoodType;

        // Making slots actually reroll the cards stored
        RandomizeSlots();

        // Disabling free reroll
        freeReroll = false;

        return true;
    }

    public void RandomizeSlots()
    {
        for (int i = 0; i < shopSlots.Length; i++)
        {
            shopSlots[i].gameObject.SetActive(true);

            bool cardRepeated = false;
            CreatureObj cardForSale;
            int loopSave = 0;

            do
            {
                // picking random card for the shop
                cardForSale = GameManager.instance.cardGenerator.PickRandomCard("shop");
                cardRepeated = false;
                loopSave++;

                for (int a = 0; a < i; a++)
                {
                    if (cardForSale == shopSlots[a].cardData) cardRepeated = true;
                }

            } while (cardRepeated && loopSave < 10);

            shopSlots[i].InitSlot(cardForSale);
        }
    }
}
