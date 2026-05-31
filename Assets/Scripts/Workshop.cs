using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Workshop : MonoBehaviour
{
    [Header("starting")]
    bool startingSequence;
    [SerializeField] TextMeshProUGUI startingMinionCounter;

    [Header("creating cards")]
    [SerializeField] GameObject cardCreationUI;
    [SerializeField] GameObject abilitySelectUI;
    [SerializeField] GameObject canvasUI;
    [SerializeField] WorkshopOption[] abilityOption = new WorkshopOption[2];
    [SerializeField] TMP_InputField nameInput;
    [HideInInspector] public AbilityObj chosenAbility;
    [SerializeField] DrawingTool drawingCanvas;

    [Header("refs")]
    Player player;
    CardGenerator generator;
    [SerializeField] TextMeshProUGUI abilityReminder;
    [SerializeField] GameObject createButton;

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
        for (int a = 0; a < abilityOption.Length; a++)
        {
            abilityOption[a].workshop = this;
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
        EnableAbilityOptions(true);

        
        if (drawSpecial) // options for special cards
        {
            for (int a = 0; a < abilityOption.Length; a++)
            {
                AbilityObj specialAbilityToAssign = generator.PickRandomAbility("special");
                abilityOption[a].abilityNote.InitAbilityNote(specialAbilityToAssign);
            }
        }
        else // options for basic cards
        {
            // first option is always a standart +1 power card
            abilityOption[0].abilityNote.InitAbilityNote(generator.PickRandomAbility("attack"));

            // second is a random card
            AbilityObj abilityToAssign = generator.PickRandomAbility("basic");
            abilityOption[1].abilityNote.InitAbilityNote(abilityToAssign);
        }
    }

    void EnableAbilityOptions(bool isEnable)
    {
        abilitySelectUI.SetActive(isEnable);
        canvasUI.SetActive(false);
    }

    void EnableDrawingCanvas()
    {
        // enalbing the drawing canvas gameObject
        canvasUI.SetActive(true);

        // passing the ability
        abilityReminder.text = chosenAbility.abilityDescription;

        // resetting name input field
        nameInput.text = "";
    }

    public void PickAbilityOption(AbilityObj pickedAbility)
    {
        // save the ability
        chosenAbility = pickedAbility;

        // play Audio Effect
        AudioManager.instance.PlaySFX("BuySFX");

        // switching to drawing UI
        abilitySelectUI.SetActive(false);
        EnableDrawingCanvas();
    }

    public void CreateNewCard()
    {
        // creating the card and giving it to player
        CreatureObj newCardData = generator.ConstructNewCard(nameInput.text, drawingCanvas.GetSprite(), chosenAbility);
        generator.CreateCard(newCardData, GameManager.instance.player);

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
            cardCreationUI.SetActive(false);
            GameManager.instance.readyButton.gameObject.SetActive(true);
            GameManager.instance.readyButton.buttonText.text = "Ready";
            GameManager.instance.gameStateUI[2].SetActive(true);
        }
    }

    void StopStartingSequence()
    {
        startingSequence = false;
        cardCreationUI.SetActive(false);
        GameManager.instance.readyButton.gameObject.SetActive(true);
        startingMinionCounter.gameObject.SetActive(false);
    }
}