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

    private void OnEnable()
    {
        ResetReroll();
        RandomizeSlots();
    }

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

    void RandomizeSlots()
    {
        for (int i = 0; i < shopSlots.Length; i++)
        {
            shopSlots[i].gameObject.SetActive(true);

            bool cardRepeated = false;
            CreatureObj cardForSale;

            do
            {
                // picking random card for the shop
                cardForSale = GameManager.instance.cardGenerator.PickRandomCard("shop");
                cardRepeated = false;

                for (int a = 0; a < i; a++)
                {
                    if (shopSlots[i].cardData == shopSlots[a].cardData) cardRepeated = true;
                }

            } while (cardRepeated);

            shopSlots[i].InitSlot(cardForSale);
        }
    }
}
