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

    [Header("prefabs")]
    [SerializeField] GameObject cardPrefab;

    private void Start()
    {
        handManager = GameManager.instance.handManager;
        CreateStartingHand(GameManager.instance.playerConfig.startingCards, GameManager.instance.player);
    }

    /// <summary>
    /// Picks random card scriptalbe object from an array of all available cards
    /// </summary>
    /// <returns></returns>
    CreatureObj PickRandomCard()
    {
        int randomCard_ID = Random.Range(0, mainList.cardList.Count);
        return mainList.cardList[randomCard_ID];
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
    void CreateCard(CreatureObj cardData, Player player)
    {
        Transform ParentTransform = (player == GameManager.instance.player) ? handManager.hand.transform : handManager.opponentHand.transform;
        GameObject newCardObject = Instantiate(cardPrefab, transform.position, Quaternion.identity, ParentTransform);
        Card newCard = newCardObject.GetComponent<Card>();
        newCard.AssignCardData(cardData, player);

        // Adding new card to the hand
        GameManager.instance.handManager.AddCardToHand(newCard, player);
    }

    private void Update()
    {
        // temp solution for adding cards to hand
        if (Input.GetKeyDown(KeyCode.A)) CreateCard(PickRandomCard(), GameManager.instance.player);
    }
}
