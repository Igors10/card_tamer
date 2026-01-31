using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections;
using NUnit.Framework.Constraints;
public class HandManager : MonoBehaviour
{
    [Header("refs")]
    public List<Card> cardsInHand = new List<Card>();
    public GameObject hand;
    public GameObject opponentHand;
    public Card activeCard; // card currently being dragged

    [Header("Fan spread params")]
    [SerializeField] float fanSpread;
    [SerializeField] float cardSpacing = 200f;
    [SerializeField] float verticalSpacing = 10f;

    [Header("Hand hide params")]
    [SerializeField] RectTransform handShownPos;
    [SerializeField] RectTransform handHiddenPos;
    [SerializeField] float handHideDistance;
    [SerializeField] float handHideDistanceVertical;
    [SerializeField] float handHideSpeed;

    /// <summary>
    /// Adds a card to player's hand
    /// </summary>
    public void AddCardToHand(Card card, Player player)
    {
        player.cardsInHand.Add(card);
        UpdateHandVisuals(player);
    }

    /// <summary>
    /// Moves a card from player's hand to cards currently on the field
    /// </summary>
    /// <param name="card"></param>
    public void AddCardToField(Card card, Player player)
    {
        player.cardsInHand.Remove(card);
        player.cardsOnField.Add(card);
        card.transform.SetParent(GameManager.instance.planningManager.fieldHand.transform, false);
        card.gameObject.SetActive(false);
        UpdateHandVisuals(player);
    }

    /// <summary>
    /// Makes the cards in hand render in a fancy fan spread way
    /// </summary>
    public void UpdateHandVisuals(Player player)
    {
        if (player.isAI) return;

        int cardCount = player.cardsInHand.Count;

        if (cardCount == 1)
        {
            player.cardsInHand[0].transform.localPosition = Vector3.zero;
            player.cardsInHand[0].transform.localRotation = Quaternion.identity;
            return;
        }

        for (int a = 0; a < cardCount; a++) 
        {
            float rotationAngle = fanSpread * (a - (cardCount - 1) / 2f);
            player.cardsInHand[a].RotateCard(rotationAngle);

            float horizontalOffset = cardSpacing * (a - (cardCount - 1) / 2f);

            float normalizedPosition = 2f * a / (cardCount - 1) - 1f; 
            float verticalOffset = verticalSpacing * (1 - normalizedPosition * normalizedPosition);
            player.cardsInHand[a].transform.localPosition = new Vector3(horizontalOffset, verticalOffset, 0f);
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
        // Hand goes down (hides)
        if (Input.mousePosition.y >= handHideDistance && handRT.transform.position.y > handHiddenPos.transform.position.y)
        {
            hideSpeed = -handHideSpeed;
            // Making the hand movement speed increase exponentially
            hideSpeed *= Mathf.Abs(handRT.transform.position.y - handHiddenPos.transform.position.y);
        }
        // Hand goes up (shows)
        else if (Input.mousePosition.y < handHideDistance && Mathf.Abs(hand.transform.position.x - Input.mousePosition.x) < handHideDistanceVertical 
            && handRT.transform.position.y < handShownPos.transform.position.y)
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
}
