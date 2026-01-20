using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections;
using NUnit.Framework.Constraints;
public class HandManager : MonoBehaviour
{
    [Header("refs")]
    public List<Card> cardsInHand = new List<Card>();
    public List<Card> cardsOnField = new List<Card>();
    public GameObject hand;
    public Card activeCard; // card currently being dragged

    [Header("Fan spread params")]
    [SerializeField] float fanSpread;
    [SerializeField] float cardSpacing = 200f;
    [SerializeField] float verticalSpacing = 10f;

    [Header("Hand hide params")]
    [SerializeField] RectTransform handShownPos;
    [SerializeField] RectTransform handHiddenPos;
    [SerializeField] float handHideDistance;
    [SerializeField] float handHideSpeed;

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

    /// <summary>
    /// Hides the hand a little below the screen. Shows when player lowers mouse below certain point
    /// </summary>
    void HandHidingCheck()
    {
        if (GameManager.instance.currentState != GameState.PLACING) return;
        RectTransform handRT = hand.GetComponent<RectTransform>();
        float hideSpeed = 0;

        // Deciding if hand should go up, down or not move
        if (Input.mousePosition.y >= handHideDistance && handRT.transform.position.y > handHiddenPos.transform.position.y)
        {
            hideSpeed = -handHideSpeed;
            // Making the hand movement speed increase exponentially
            hideSpeed *= Mathf.Abs(handRT.transform.position.y - handHiddenPos.transform.position.y);
        }
        else if (Input.mousePosition.y < handHideDistance && handRT.transform.position.y < handShownPos.transform.position.y)
        {
            hideSpeed = handHideSpeed;
            // Making the hand movement speed increase exponentially
            hideSpeed *= Mathf.Abs(handRT.transform.position.y - handShownPos.transform.position.y);
        }
        else hideSpeed = 0f;

        // Actually moving the hand
        handRT.transform.position += new Vector3(0f, hideSpeed, 0f);
    }

    private void Update()
    {
        HandHidingCheck();
    }

    public void HandState(GameState state)
    {
        // resetting hand state
        for (int i = 0; i < cardsInHand.Count; i++)
        {
            cardsInHand[i].gameObject.SetActive(false);
        }
        for (int i = 0; i < cardsOnField.Count; i++)
        {
            cardsOnField[i].gameObject.SetActive(false);
        }

        // switching to new hand state
        switch (state)
        {
            case GameState.PLACING:

                // Activating the hand
                for (int i = 0; i < cardsInHand.Count; i++)
                {
                    cardsInHand[i].gameObject.SetActive(true);
                }
                UpdateHandVisuals();

                break;
            case GameState.PLANNING:

                // Activating field cards
                for (int i = 0; i < cardsOnField.Count; i++)
                {
                    cardsOnField[i].gameObject.SetActive(true);
                }

                break;
            case GameState.EXECUTING:
                break;
            case GameState.BATTLING:
                break;
            case GameState.BUYING:
                break;
        }
    }
}
