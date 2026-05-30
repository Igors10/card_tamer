using UnityEngine;
using TMPro;
using System.Collections;

public class Workshop : MonoBehaviour
{
    [Header("starting")]
    bool startingSequence;
    [SerializeField] TextMeshProUGUI startingMinionCounter;

    [Header("creating cards")]
    [SerializeField] GameObject drawingUI;
    [SerializeField] WorkshopOption[] cardOptions = new WorkshopOption[2];

    [Header("refs")]
    Player player;
    CardGenerator generator;

    [Header("workshop animation")]
    [SerializeField] GameObject background;
    [SerializeField] GameObject[] bgItems;
    [SerializeField] Transform[] bgItemPositions;
    Vector3[] bgItemStartingPositions = new Vector3[3];
    Vector3[] bgItemWorkshopPositions = new Vector3[3];
    [SerializeField] float appearanceTime;


    private void Start()
    {
        player = GameManager.instance.player;
        generator = GameManager.instance.cardGenerator;

        // passing workshop reference to card options
        for (int a = 0; a < cardOptions.Length; a++)
        {
            cardOptions[a].workshop = this;
        }

        // getting starting bg items positions
        for (int a = 0; a < bgItems.Length;  a++)
        {
            bgItemStartingPositions[a] = bgItems[a].transform.localPosition;
            bgItemWorkshopPositions[a] = bgItemPositions[a].transform.localPosition;
        }

    
    }

    public IEnumerator WorkshopAnim(bool isAppearing, bool moveBackground = true)
    {
        Vector3[] startingPos = (isAppearing) ? bgItemStartingPositions : bgItemWorkshopPositions;
        Vector3[] targetPos = (isAppearing) ? bgItemWorkshopPositions : bgItemStartingPositions;
        Vector3 bgTargetPos = (isAppearing) ? Vector3.zero : new Vector3(0, -Camera.main.pixelHeight -200f, 0);
        Vector3 bgStartingPos = (isAppearing) ? new Vector3(0, -Camera.main.pixelHeight - 200f, 0) : Vector3.zero;

        float t = 0;

        while (t < appearanceTime)
        {
            t += Time.deltaTime;
            float clampedT = t / appearanceTime;
            float coolT = 1 - (1 - clampedT) * (1 - clampedT);

            // moving each object
            for (int i = 0; i < bgItems.Length; i++)
            {
                bgItems[i].transform.localPosition = Vector3.Lerp(startingPos[i], targetPos[i], coolT);
            }

            // moving background
            if (moveBackground) background.transform.localPosition = Vector3.Lerp(bgStartingPos, bgTargetPos, clampedT); 

            yield return null;
        }

        // snapping each object to correct position
        for (int i = 0; i < bgItems.Length; i++)
        {
            bgItems[i].transform.localPosition = targetPos[i];
        }

        // deactivating itself after playing the "fading away animation"
        if (isAppearing == false) gameObject.SetActive(false);
    }

    public void LaunchStartingSequence()
    {
        // enabling correct workshop visuals
        GameManager.instance.managerUI.EnableWorkshop(true, false);

        startingSequence = true;
        DrawCardOptions();
    }

    public void DrawCardOptions(bool drawSpecial = false)
    {
        // disabling shop UI
        GameManager.instance.gameStateUI[2].SetActive(false);
        GameManager.instance.readyButton.gameObject.SetActive(false);

        // enabling card choice buttons
        EnableCardOptions(true);

        if (drawSpecial) // options for special cards
        {
            for (int a = 0; a < cardOptions.Length; a++)
            {
                CreatureObj specialToAssign = generator.PickRandomCard("special");
                cardOptions[a].card.AssignCardData(specialToAssign, player);
            }
        }
        else // options for basic cards
        {
            // first option is always a standart +1 power card
            cardOptions[0].card.AssignCardData(generator.basicCardData, player);

            // second is a random card
            CreatureObj dataToAssign = generator.PickRandomCard("basic");
            cardOptions[1].card.AssignCardData(dataToAssign, player);
        }
    }

    void EnableCardOptions(bool isEnable)
    {
        drawingUI.SetActive(isEnable);
    }

    public void PickCardOption(CreatureObj pickedCard)
    {
        // adding chosen card to player's hand
        generator.CreateCard(pickedCard, player);

        // play Audio Effect
        AudioManager.instance.PlaySFX("BuySFX");

        // if starting sequence continue until player has 5 cards
        if (startingSequence)
        {
            // updating starting minion counter
            startingMinionCounter.text = "minions " + player.cardsInHand.Count + "/" + GameManager.instance.startingCardAmount;

            // no more options if player has 5 cards
            if (player.cardsInHand.Count == GameManager.instance.startingCardAmount) { StopStartingSequence(); return; }

            // otherwise continue choosing (drawing) cards
            DrawCardOptions();
        }
        else
        {
            // go back to shop if it is not a starting sequence
            EnableCardOptions(false);
            GameManager.instance.readyButton.gameObject.SetActive(true);
            GameManager.instance.gameStateUI[2].SetActive(true);
        }
    }

    void StopStartingSequence()
    {
        startingSequence = false;
        EnableCardOptions(false);
        GameManager.instance.readyButton.gameObject.SetActive(true);
        startingMinionCounter.gameObject.SetActive(false);
    }
}