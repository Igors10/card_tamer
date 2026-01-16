using System.Collections.Generic;
using UnityEngine;

public class HandManager : MonoBehaviour
{
    [Header("refs")]
    public List<Card> cardsInHand = new List<Card>();
    public List<Card> cardsOnField = new List<Card>();
    public GameObject hand;
    public Card activeCard; // card currently being dragged

    [Header("Card fan params")]
    [SerializeField] float fanSpread;
    [SerializeField] float cardSpacing = 200f;
    [SerializeField] float verticalSpacing = 10f;

    /// <summary>
    /// Adds a card to player's hand
    /// </summary>
    public void AddCardToHand(Card card)
    {
        cardsInHand.Add(card);
        UpdateHandVisuals();
    }

    /// <summary>
    /// Moves a card from player's hand to cards currently on the field
    /// </summary>
    /// <param name="card"></param>
    public void AddCardToField(Card card)
    {
        cardsInHand.Remove(card);
        cardsOnField.Add(card);
        card.gameObject.SetActive(false);
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
