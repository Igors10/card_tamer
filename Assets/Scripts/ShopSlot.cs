using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ShopSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
    [Header("refs")]
    [HideInInspector] public CreatureObj cardData;
    [SerializeField] Image[] priceTokens;
    public Sprite[] foodSprites;
    [SerializeField] Image slotSprite;

    [Header("buying")]
    FoodType[] priceTag;
    [SerializeField] float tokenHorSpacing;
    [SerializeField] float tokenVerSpacing;
    [SerializeField] float tokenVerOffset = -200f;
    [SerializeField] float buyingSpeed;
    [SerializeField] float buyingDelay;
    [SerializeField] float tokenAnimInterval = 0.3f;
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

        // starting token animation
        StartCoroutine(ShopTokenAnim(true));
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
            StartCoroutine(BuyCard(GameManager.instance.player));
        }

    }

    void IndicatePlayer(Player player)
    {
        // getting reference to the indicator
        TextMeshProUGUI indicatorText = GameManager.instance.shopManager.shopPlayerIndicator;
        GameObject indicatorObj = indicatorText.transform.parent.gameObject;

        // setting correct indicator values 
        indicatorObj.transform.localPosition = transform.localPosition + new Vector3(0, -tokenVerOffset, 0);
        indicatorText.color = player.playerColor;
        string text = player.playerName + " is buying";
        indicatorText.text = text;

        // activating the indicator
        indicatorObj.SetActive(true);
    }

    /// <summary>
    /// Deactivates shop player indicator
    /// </summary>
    void StopIndicatePlayer()
    {
        GameManager.instance.shopManager.shopPlayerIndicator.transform.parent.gameObject.SetActive(false);
    }

    /// <summary>
    /// Makes tokens appear one after another (or disappear if parameter is set to false)
    /// </summary>
    /// <param name="appearing"></param>
    /// <returns></returns>
    IEnumerator ShopTokenAnim(bool appearing, Player buyer = null)
    {
            // gettting all the active tokens
        List<GameObject> activeTokens = new List<GameObject>();

        foreach (Image token in priceTokens)
        {
            if (token.gameObject.activeSelf) activeTokens.Add(token.gameObject);
            if (appearing) token.gameObject.SetActive(false);
        }

        for (int i = 0; i < activeTokens.Count; i++)
        {
            // pause interval
            float animInterval = (appearing) ? tokenAnimInterval / 2 : tokenAnimInterval;
            yield return new WaitForSeconds(animInterval);

            // Activating (or diactivating correct token)
            int currentTokenID = (appearing) ? i : activeTokens.Count - 1 - i;
            activeTokens[currentTokenID].SetActive(appearing);

            // Playing VFX
            GameManager.instance.animations.PopAnim(activeTokens[currentTokenID], 0.3f, 0.25f);
            ParticleSystem tokenVFX = Instantiate(GameManager.instance.shopManager.shopTokenVFX, activeTokens[currentTokenID].transform.position, Quaternion.identity);
            Debug.Log("ShopSlot: shop token VFX instantiated at " + tokenVFX.transform.position);
        }

        // Deactiavting the indicator
        if (buyer != null) StopIndicatePlayer();
    }

    /// <summary>
    /// Calculates if player has enough resources to buy a card, buys it and returns true if so. Returns false if resources are not enough
    /// </summary>
    /// <param name="buyer"></param>
    /// <returns></returns>
    public IEnumerator BuyCard(Player buyer)
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
            Debug.Log("ShopSlot: player has not enough money to purchase the slot");
            // also play negative sound effect
            yield break;
        }

        // buying
        // substract the resources
        for (int i = 0; i < requiredFood.Length; i++)
        {
            buyer.playerUI.foodCounters[i].AddFood(-requiredFood[i]);
        }

        // Acitvating the indicator
        if (buyer == GameManager.instance.opponent)
        {
            IndicatePlayer(buyer);
            yield return new WaitForSeconds(buyingDelay);
            StopIndicatePlayer();
        }

        // give card to the player
        GameManager.instance.cardGenerator.CreateCard(cardData, buyer);

        this.gameObject.SetActive(false);

        // Ending the turn
        buyer.EndTurn();

        Debug.Log("ShopSlot: ["+ buyer.playerName +"] purchases " + cardData.name);
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
        if (!GameManager.instance.yourTurn) return;

        highlightBackground.gameObject.SetActive(mouseOver);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!GameManager.instance.yourTurn) return;
        ResetBuyProgress();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!GameManager.instance.yourTurn) return;
        isBuying = true;
    }

    // ===============================================       

}
