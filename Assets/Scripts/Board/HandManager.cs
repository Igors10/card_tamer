using NUnit.Framework.Constraints;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.UI.CanvasScaler;
public class HandManager : MonoBehaviour
{
    [Header("refs")]
    public List<Card> cardsInHand = new List<Card>();
    public GameObject hand;
    public GameObject fieldHand;
    public GameObject opponentHand;
    public Card activeCard; // card currently being dragged
    public GameObject activeCardObj;

    [Header("Fan spread params")]
    [SerializeField] float fanSpread;
    [SerializeField] float defaultCardSpacing = 200f;
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
        card.ExitHandLayout();
        card.transform.SetParent(fieldHand.transform, false);
        //card.gameObject.SetActive(false);
        UpdateHandVisuals(player);
        GameManager.instance.managerUI.Refresh();
    }

    public void AddCardToDiscarded(Card card, Player player)
    {
        //player.cardsOnField.Remove(card);
        player.cardsInDiscard.Add(card);
    }

    /// <summary>
    /// Takes a single card off the field back to its owner's hand, its unit is removed and its power is reset
    /// </summary>
    public void ReturnCardToHand(Card card)
    {
        Player player = card.player;
        player.cardsOnField.Remove(card);

        card.gameObject.SetActive(true);
        if (card.unit != null) card.unit.RemoveFromBoard();

        Transform playerHand = (player == GameManager.instance.player) ? hand.transform : opponentHand.transform;
        card.transform.SetParent(playerHand, false);
        card.CardEndRound();
        AddCardToHand(card, player);
    }

    public void ReturnCardsToHand(Player player)
    {
        // moving all cards to cards in hand
        foreach (Card card in player.cardsOnField)
        {
            card.gameObject.SetActive(true);
            if (card.unit != null) card.unit.RemoveFromBoard();

            Transform playerHand = (player == GameManager.instance.player) ? hand.transform : opponentHand.transform;
            card.transform.SetParent(playerHand, false);
            card.CardEndRound();
            AddCardToHand(card, player);
        }

        player.cardsOnField.Clear();
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
            player.cardsInHand[0].SetHandLayout(Vector3.zero, 0f);
            return;
        }

        for (int a = 0; a < cardCount; a++) 
        {
            float rotationAngle = fanSpread * (a - (cardCount - 1) / 2f);

            float cardSpacing = (cardsInHand.Count < 7) ? -defaultCardSpacing : -defaultCardSpacing / cardsInHand.Count * 6;
            float horizontalOffset = cardSpacing * (a - (cardCount - 1) / 2f);

            float normalizedPosition = 2f * a / (cardCount - 1) - 1f; 
            float verticalOffset = verticalSpacing * (1 - normalizedPosition * normalizedPosition);
            // cards ease to their new place instead of snapping
            player.cardsInHand[a].SetHandLayout(new Vector3(horizontalOffset, verticalOffset, 0f), rotationAngle);
        }
    }

    /// <summary>
    /// Hides the hand a little below the screen. Shows when player lowers mouse below certain point
    /// </summary>
    void HandHidingCheck()
    {
        // Hide the hand if following
        bool forceHide = (activeCard != null || GameManager.instance.managerUI.workshop.drawingCanvas.isDrawing);
        RectTransform handRT = hand.GetComponent<RectTransform>();
        float hideSpeed = 0;

        // Deciding if hand should go up, down or not move
        // Hand goes down (hides)
        if (Input.mousePosition.y >= handHideDistance && handRT.transform.position.y > handHiddenPos.transform.position.y || forceHide)
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

        // temp clearing hand solution
        if (Input.GetKey(KeyCode.Backspace) && GameManager.instance.currentState == GameState.PLACING) ClearHand();
    }

    void ClearHand()
    {
        Player player = GameManager.instance.player;
        
        foreach (Card card in player.cardsInHand)
        {
            Destroy(card.gameObject);
        }

        player.cardsInHand.Clear();

        Debug.Log("Hand Manager: hand cleared");
    }
}
