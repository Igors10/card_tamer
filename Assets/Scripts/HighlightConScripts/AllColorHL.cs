using System.Collections.Generic;
using UnityEngine;

public class AllColorHL : MonoBehaviour
{
    Card card;

    private void Start()
    {
        // getting reference to the card
        card = GetComponentInParent<Card>();

        GameManager.OnCardHovered += HighlightColorUnits;
    }

    void HighlightColorUnits(Card cardHovered)
    {
        // checking if this card is hovered over
        if (cardHovered != card) return;

        // getting reference to the list of respective color
        List<Unit> unitsToHighlight = GameManager.instance.fieldManager.coloredUnitList[Colors.instance.GetSecondaryColorID(card.abilities[0].abilityColor)];

        // highlighting all units in the list
        GameManager.instance.fieldManager.HighlightUnits(unitsToHighlight);
    }
}