using System.Collections.Generic;
using UnityEngine;

public class HandManager : MonoBehaviour
{
    [Header("refs")]
    public List<Card> cardsInHand = new List<Card>();
    public GameObject hand;

    [Header("Card fan params")]
    [SerializeField] float fanSpread;
    [SerializeField] float cardSpacing = 200f;
    [SerializeField] float verticalSpacing = 10f;

    /// <summary>
    /// Adds a card to player's hand
    /// </summary>
    public void AddCard(Card cardToAdd)
    {
        cardsInHand.Add(cardToAdd);
        UpdateHandVisuals();
    }

    /// <summary>
    /// Makes the cards in hand render in a fancy fan spread way
    /// </summary>
    public void UpdateHandVisuals()
    {
        int cardCount = cardsInHand.Count;

        if (cardCount == 1)
        {
            cardsInHand[0].transform.localPosition = Vector3.zero;
            cardsInHand[0].transform.localRotation = Quaternion.identity;
            return;
        }

        for (int a = 0; a < cardCount; a++) 
        {
            float rotationAngle = fanSpread * (a - (cardCount - 1) / 2f);
            cardsInHand[a].RotateCard(rotationAngle);

            float horizontalOffset = cardSpacing * (a - (cardCount - 1) / 2f);

            float normalizedPosition = 2f * a / (cardCount - 1) - 1f; 
            float verticalOffset = verticalSpacing * (1 - normalizedPosition * normalizedPosition);
            cardsInHand[a].transform.localPosition = new Vector3(horizontalOffset, verticalOffset, 0f);
        }
    }
}
