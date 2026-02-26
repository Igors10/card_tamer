using FishNet.Demo.AdditiveScenes;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public class ShopSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
    [Header("refs")]
    [HideInInspector] public CreatureObj cardData;
    [SerializeField] Image[] priceTokens;
    [SerializeField] Sprite[] foodSprites;
    [SerializeField] Image slotSprite;

    [Header("buying")]
    FoodType[] priceTag;
    [SerializeField] float tokenHorSpacing;
    [SerializeField] float tokenVerSpacing;
    [SerializeField] float tokenVerOffset = -200f;
    [SerializeField] float buyingSpeed;
    bool isBuying;
    float buyProgress = 0f;

    [Header("highlight")]
    [SerializeField] Image highlightBackground;
    [SerializeField] Image highlightFill;
    [SerializeField] Color highlightColor;
    Color highlightBgColor;

    private void Start()
    {
        highlightBgColor = Color.Lerp(highlightColor, Color.white, 0.6f);
        highlightFill.color = highlightColor;
        highlightBackground.color = highlightBgColor;
    }

    /// <summary>
    /// Make the slot sell chosen card, updates its visuals and price accordingly
    /// </summary>
    /// <param name="soldCardData"></param>
    public void InitSlot(CreatureObj soldCardData)
    {
        // Makes the slot store selected card
        cardData = soldCardData;
        
        // Updates slot sprite
        slotSprite.sprite = cardData.unitSprite;

        // Updates the price
        priceTag = cardData.cost;

        for (int i = 0; i < priceTokens.Length; i++)
        {
            // reset (deactivate) all tokens
            priceTokens[i].gameObject.SetActive(false);

            if (i < priceTag.Length)
            {
                // activates tokens necessary
                priceTokens[i].gameObject.SetActive(true);

                // makes the tokens render correct foods
                priceTokens[i].sprite = foodSprites[(int)priceTag[i]];

                // special case for when its only 1 token
                if (priceTag.Length == 1)
                {
                    priceTokens[i].transform.localPosition = new Vector3(0, tokenVerOffset, 0f);
                    priceTokens[i].transform.localRotation = Quaternion.identity;
                }
                else
                {
                    // Makes the price tokens spaced correctly
                    float horizontalOffset = tokenHorSpacing * (i - (priceTag.Length - 1) / 2f);

                    float normalizedPosition = 2f * i / (priceTag.Length - 1) - 1f;
                    float verticalOffset = tokenVerSpacing * (1 - normalizedPosition * normalizedPosition) + tokenVerOffset;
                    priceTokens[i].transform.localPosition = new Vector3(horizontalOffset, verticalOffset, 0f);
                }
            }           
        }

        ResetBuyProgress();
        highlightBackground.gameObject.SetActive(false);
    }

    void Update()
    {
        BuyCheck();
    }

    void BuyCheck()
    {
        if (!isBuying) return;

        buyProgress += Time.deltaTime * buyingSpeed;
        if (buyProgress < 1f)
        {
            highlightFill.fillAmount = buyProgress;
        }
        else
        {
            BuyCard(GameManager.instance.player);
        }

    }

    public void BuyCard(Player buyer)
    {
        ResetBuyProgress();

        // check how much of each food player needs to have
        int[] requiredFood = {0, 0, 0 };

        for (int i = 0; i < priceTag.Length; i++)
        {
            requiredFood[(int)priceTag[i]]++;
        }

        // checking if player has enough of each food
        bool enoughFood = true;
        for (int i = 0; i < requiredFood.Length; i++)
        {
            if (buyer.food[i] < requiredFood[i])
            {
                enoughFood = false;
                StartCoroutine(buyer.playerUI.foodCounters[i].NegativeBlink());
            }
        }

        // if not enough food resources do not proceed with buying
        if (!enoughFood)
        {
            // also play negative sound effect
            return;
        }

        // buying
        // substract the resources
        for (int i = 0; i < requiredFood.Length; i++)
        {
            buyer.food[i] -= requiredFood[i];
            buyer.playerUI.foodCounters[(int)requiredFood[i]].RefreshToken(false);
        }

        // give card to the player
        GameManager.instance.cardGenerator.CreateCard(cardData, buyer);

        this.gameObject.SetActive(false);
    }

    /// <summary>
    /// Resetting values related to buying card from the slot
    /// </summary>
    void ResetBuyProgress()
    {
        highlightFill.fillAmount = 0f;
        buyProgress = 0f;
        isBuying = false;
    }

    // =============== Input =========================

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnHover(false);
    }

    void OnHover(bool mouseOver)
    {
        highlightBackground.gameObject.SetActive(mouseOver);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetBuyProgress();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isBuying = true;
    }

    // ===============================================       

}
