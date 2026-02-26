using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] ShopSlot[] shopSlots;

    [Header("rerolling")]
    [SerializeField] Button rerollButton;
    [SerializeField] TextMeshProUGUI rerollText;
    [SerializeField] Image rerollPriceImage;
    FoodType rerollPrice;
    bool freeReroll = true;

    void ResetReroll()
    {
        freeReroll = true;
        rerollText.text = "";
    }

    void RerollShop()
    {
        // do checks if player has enough food and change random food when used
        RandomizeSlots();

        freeReroll = false;
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
