using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class PrevColorHL : MonoBehaviour
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

        // finding the last unit in a given color array and highlighting it
        int listID = Colors.instance.GetSecondaryColorID(card.abilities[0].abilityColor);
        if (GameManager.instance.fieldManager.coloredUnitList[listID].Count < 1) return;
        GameManager.instance.fieldManager.coloredUnitList[listID][GameManager.instance.fieldManager.coloredUnitList[listID].Count - 1].HighlightUnit(true);

        // marking that there is a highlighted unit
        GameManager.instance.fieldManager.anyUnitHighlighted = true;
    }
}
