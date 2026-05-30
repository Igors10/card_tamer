using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;
 
public class CardGenerator : MonoBehaviour
{
    // temporary variable for how many random cards to create at the beginning
    [SerializeField] int startingCardNumber;

    [Header("refs")]
    HandManager handManager;
    [SerializeField] CardList mainList;
    [SerializeField] CardList basicList;
    [SerializeField] CardList specialList;

    [Header("prefabs")]
    [SerializeField] GameObject cardPrefab;
    public CreatureObj basicCardData;

    [Header("Card search")]
    [SerializeField] bool searchEnabled;
    [SerializeField] string cardsName;

    private void Start()
    {
        handManager = GameManager.instance.handManager;
        //CreateStartingHand(GameManager.instance.playerConfig.startingCards, GameManager.instance.player);
    }

    /// <summary>
    /// Picks random card scriptalbe object from an array of all available cards
    /// </summary>
    /// <returns></returns>
    public CreatureObj PickRandomCard(string listName = "")
    {
        CardList list = mainList;

        // Picking from a specific list if asked in parameters
        switch (listName)
        {
            case "basic":
                list = basicList;
                break;

            case "special":
                list = specialList;
                break;
        }

        int randomCard_ID = Random.Range(0, list.cardList.Count);
        return list.cardList[randomCard_ID];
    }


    /// <summary>
    /// Looks up a specific card from the pool
    /// </summary>
    /// <param name="cardName"></param>
    /// <returns></returns>
    CreatureObj GetSpecificCard(string cardName)
    {
        foreach (CreatureObj card in mainList.cardList)
        {
            if (card.name == cardName) return card;
        }

        return null;
    }

    public void CreateStartingHand(List<CreatureObj> startingCardList, Player player)
    {
        // Creating starting hand for the player
        for (int a = 0; a <  startingCardList.Count; a++)
        {
            CreateCard(startingCardList[a], player);
        }
    }

    /// <summary>
    /// Creates card gameObject from chosen card data
    /// </summary>
    /// <param name="cardData"></param>
    public void CreateCard(CreatureObj cardData, Player player)
    {
        // checking if player has reached their card limit
        if (player.cardsInHand.Count >= GameManager.instance.maxHandSize) return;

        Transform ParentTransform = (player == GameManager.instance.player) ? handManager.hand.transform : handManager.opponentHand.transform;
        GameObject newCardObject = Instantiate(cardPrefab, transform.position, Quaternion.identity, ParentTransform);
        Card newCard = newCardObject.GetComponent<Card>();
        newCard.AssignCardData(cardData, player);

        // Adding new card to the hand
        GameManager.instance.handManager.AddCardToHand(newCard, player);

        Debug.Log("CardGenerator: card " + cardData.name + " has been added to " + player.playerName + "'s hand");
    }

    private void Update()
    {
        // temp solution for adding cards to hand
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (cardsName != null || searchEnabled == false) CreateCard(PickRandomCard(), GameManager.instance.player);

            // getting a specific card if field with name is not empty
            else CreateCard(GetSpecificCard(cardsName), GameManager.instance.player);
        }
    }
}
