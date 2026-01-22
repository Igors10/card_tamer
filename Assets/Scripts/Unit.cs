using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class Unit : MonoBehaviour
{
    // Card this unit is represents on the board
    [HideInInspector] public Card card;

    [Header("refs")]
    [SerializeField] Image sprite;
    [SerializeField] TextMeshProUGUI healthValue;
    [HideInInspector] public Field currentField;
    [SerializeField] OrderMarker orderMarker;

    [Header("power")]
    [SerializeField] GameObject powerUI;
    [SerializeField] GameObject powerValue;

    [Header("health")]
    [SerializeField] Color healthValueColor = new Color(0.86f, 0.63f, 0.83f, 1f);

    [Header("movement")]
    [HideInInspector] public bool readyToMove; 

 
    public void InitUnit(Card cardToInitialize, Field field)
    {
        // Getting card data
        card = cardToInitialize;
        cardToInitialize.unit = this;
        sprite.sprite = card.cardData.unitSprite;
        RefreshUnitVisuals();

        // Setting field position
        currentField = field;
    }

    public void EnableOrderMarker(bool enable, int orderNumber = 0)
    {
        orderMarker.gameObject.SetActive(enable);

        if (orderNumber != 0) orderMarker.SetNumber(orderNumber);
    }

    void RefreshUnitVisuals()
    {
        // HEALTH
        int currentHP = card.cardData.health - card.damageToHP;
        healthValue.color = (card.damageToHP > 0) ? Color.red : healthValueColor; // number gets red if damaged
        healthValue.text = currentHP.ToString();

        // POWER
        powerUI.SetActive(false);
    }

    void ShowCard()
    {

    }

    public void OnHover()
    {

    }
}
