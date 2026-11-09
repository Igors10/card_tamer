using NUnit.Framework.Constraints;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
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
    [SerializeField] float opponentCardSpacing = 270f; // opponent's row at the top, no fan

    [Header("Hand hide params")]
    [SerializeField] RectTransform handShownPos;
    [SerializeField] RectTransform handHiddenPos;
    [SerializeField] float handHideDistance;
    [SerializeField] float handHideDistanceVertical;
    [SerializeField] float handHideSpeed;
    [SerializeField] float roundFieldRaise = 130f; // during the round the hand (field) sits this much higher
    [SerializeField] bool neverHideInRound = false; // placing and battle: the cards always stay shown

    [Header("Arranging (cards lined up in the middle of the screen)")]
    [SerializeField] Transform arrangeRow; // your cards sit here while arranging, then fly down to the hand for the battle
    [SerializeField] Image arrangeBackdrop; // dims the board while arranging
    [SerializeField] float arrangeCardSpacing = 280f;
    [SerializeField] float arrangeRowWidth = 1600f; // with many cards they get closer together

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

    /// <summary>
    /// Knocked out: the card leaves the field until the round is over (the other cards close up)
    /// </summary>
    public void AddCardToDiscarded(Card card, Player player)
    {
        player.cardsOnField.Remove(card);
        player.cardsInDiscard.Add(card);
        card.gameObject.SetActive(false);
        UpdateHandVisuals(player);
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

    /// <summary>
    /// Start of round: every card in hand goes to the field face down, in the same order
    /// </summary>
    public void MoveHandToField(Player player)
    {
        foreach (Card card in player.cardsInHand)
        {
            card.SetFaceDown(true);
            player.cardsOnField.Add(card);

            // your cards fly up to the middle of the screen for arranging
            if (player == GameManager.instance.player) card.transform.SetParent(arrangeRow, true);
        }
        player.cardsInHand.Clear();
        player.roundStartOrder = new List<Card>(player.cardsOnField);

        // the board gets darker, so the row in the middle stands out (instantly, the round starts under the loading fog)
        if (player == GameManager.instance.player) GameManager.instance.managerUI.DimActivate(arrangeBackdrop, true, true);

        UpdateHandVisuals(player);
    }

    /// <summary>
    /// Arranging: field order follows where the cards are on the screen (left to right), so a dropped card takes its new place
    /// </summary>
    public void ReorderField(Player player)
    {
        player.cardsOnField.Sort((a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));
        UpdateHandVisuals(player);
    }

    public void ReturnCardsToHand(Player player)
    {
        // knocked out cards come back too (the destroyed one is already gone)
        player.cardsOnField.AddRange(player.cardsInDiscard);
        player.cardsInDiscard.Clear();

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
        UpdateHandVisuals(player);
    }

    /// <summary>
    /// Makes the cards in hand render in a fancy fan spread way
    /// </summary>
    public void UpdateHandVisuals(Player player)
    {
        // during a round the cards are on the field (same place on the screen as the hand)
        List<Card> cards = (player.cardsOnField.Count > 0) ? player.cardsOnField : player.cardsInHand;

        // opponent's cards are a straight row at the top
        if (player != GameManager.instance.player)
        {
            for (int a = 0; a < cards.Count; a++)
            {
                cards[a].SetHandLayout(new Vector3(opponentCardSpacing * (a - (cards.Count - 1) / 2f), 0f, 0f), 0f);
            }
            return;
        }

        int cardCount = cards.Count;

        // arranging: a straight row in the middle of the screen
        if (cards == player.cardsOnField && GameManager.instance.currentState == GameState.PLACING)
        {
            float spacing = (cardCount > 0) ? Mathf.Min(arrangeCardSpacing, arrangeRowWidth / cardCount) : arrangeCardSpacing;
            for (int a = 0; a < cardCount; a++)
            {
                cards[a].SetHandLayout(new Vector3(spacing * (a - (cardCount - 1) / 2f), 0f, 0f), 0f);
            }
            return;
        }

        if (cardCount == 1)
        {
            cards[0].SetHandLayout(Vector3.zero, 0f);
            return;
        }

        for (int a = 0; a < cardCount; a++)
        {
            // first card on the left (field positions go left to right)
            float rotationAngle = -fanSpread * (a - (cardCount - 1) / 2f);

            float cardSpacing = (cardCount < 7) ? defaultCardSpacing : defaultCardSpacing / cardCount * 6;
            float horizontalOffset = cardSpacing * (a - (cardCount - 1) / 2f);

            float normalizedPosition = 2f * a / (cardCount - 1) - 1f;
            float verticalOffset = verticalSpacing * (1 - normalizedPosition * normalizedPosition);
            // cards ease to their new place instead of snapping
            cards[a].SetHandLayout(new Vector3(horizontalOffset, verticalOffset, 0f), rotationAngle);
        }
    }

    /// <summary>
    /// Opponent's row is only shown during the round (in the workshop their hand is private)
    /// </summary>
    void RefreshOpponentRow()
    {
        opponentHand.SetActive(GameManager.instance.currentState != GameState.BUYING);
        UpdateHandVisuals(GameManager.instance.opponent);
    }

    /// <summary>
    /// Arranging is over: your cards fly down from the middle of the screen into the hand
    /// </summary>
    void LeaveArrangeRow()
    {
        if (GameManager.instance.currentState != GameState.BATTLING) return;

        GameManager.instance.managerUI.DimActivate(arrangeBackdrop, false);
        Player player = GameManager.instance.player;
        foreach (Card card in player.cardsOnField)
        {
            if (card.transform.parent == arrangeRow) card.transform.SetParent(hand.transform, true);
        }
        UpdateHandVisuals(player);
    }

    void OnStateTransition()
    {
        RefreshOpponentRow();
        LeaveArrangeRow();
    }

    void OnEnable()
    {
        GameManager.OnStateTransition += OnStateTransition;
    }

    void OnDisable()
    {
        GameManager.OnStateTransition -= OnStateTransition;
    }

    /// <summary>
    /// Hides the hand a little below the screen. Shows when player lowers mouse below certain point
    /// </summary>
    void HandHidingCheck()
    {
        // hides in all phases when the cursor is away from it
        // (always shown while picking a card for a new sticker, and in the round if neverHideInRound)
        bool inRound = GameManager.instance.currentState != GameState.BUYING;
        bool forceShow = GameManager.instance.managerUI.workshop.ChoosingStickerCard || (inRound && neverHideInRound);
        bool forceHide = !inRound && !forceShow && (activeCard != null || GameManager.instance.managerUI.workshop.drawingCanvas.isDrawing);
        RectTransform handRT = hand.GetComponent<RectTransform>();
        float hideSpeed = 0f;

        bool cursorNearHand = Input.mousePosition.y < handHideDistance && Mathf.Abs(hand.transform.position.x - Input.mousePosition.x) < handHideDistanceVertical;
        bool shouldShow = !forceHide && (forceShow || cursorNearHand);
        // (the field sits a bit higher than the hand, so the cards can be read)
        float shownY = handShownPos.transform.position.y + ((inRound) ? roundFieldRaise * handRT.lossyScale.y : 0f);

        // Deciding if hand should go up, down or not move
        // Hand goes up (shows)
        if (shouldShow && handRT.transform.position.y < shownY)
        {
            hideSpeed = handHideSpeed;
            // Making the hand movement speed increase exponentially
            hideSpeed *= Mathf.Abs(handRT.transform.position.y - shownY);
        }
        // Hand goes down (hides)
        else if (!shouldShow && handRT.transform.position.y > handHiddenPos.transform.position.y)
        {
            hideSpeed = -handHideSpeed;
            // Making the hand movement speed increase exponentially
            hideSpeed *= Mathf.Abs(handRT.transform.position.y - handHiddenPos.transform.position.y);
        }

        // Actually moving the hand
        handRT.transform.position += new Vector3(0f, hideSpeed, 0f);
    }

    private void Update()
    {
        HandHidingCheck();

        // temp clearing hand solution
        if (GameManager.instance.DebugKeysEnabled && Input.GetKey(KeyCode.Backspace) && GameManager.instance.currentState == GameState.PLACING) ClearHand();
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
