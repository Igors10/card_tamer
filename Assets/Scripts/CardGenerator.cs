using Unity.VisualScripting;
using UnityEngine;
 
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
        CreateStartingHand(startingCardNumber);
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

    void CreateStartingHand(int cardAmount)
    {
        for (int a = 0; a <  cardAmount; a++)
        {
            CreateCard(PickRandomCard());
        }
    }

    /// <summary>
    /// Creates card gameObject from chosen card data
    /// </summary>
    /// <param name="cardData"></param>
    void CreateCard(CreatureObj cardData)
    {
        GameObject newCardObject = Instantiate(cardPrefab, transform.position, Quaternion.identity, handManager.hand.transform);
        Card newCard = newCardObject.GetComponent<Card>();
        newCard.AssignCardData(cardData);

        // Adding new card to the hand
        GameManager.instance.handManager.AddCardToHand(newCard);
    }

    private void Update()
    {
        // temp solution for adding cards to hand
        if (Input.GetKeyDown(KeyCode.A)) CreateCard(PickRandomCard());
    }
}
