using Unity.VisualScripting;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using TMPro.EditorUtilities;
using UnityEngine.VFX;

public enum GameState
{
    PLACING,
    PLANNING,
    EXECUTING,
    BATTLING,
    BUYING
}

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Game State")]
    public GameState currentState;
    public List<GameStateData> gameStates = new List<GameStateData>();
    public List<GameObject> gameStateUI = new List<GameObject>();
    [HideInInspector] public bool yourTurn;
    public bool endStateReady;
    public bool opponentEndStateReady; // debug thing

    [Header("Managers")]
    public HandManager handManager;
    public CardGenerator cardGenerator;
    public FieldManager fieldManager;
    public PlanningManager planningManager;
    public ExecuteManager executeManager;
    public ManagerUI managerUI;
    public VFXManager VFXmanager;

    [Header("UI stuff")]
    [SerializeField] TextMeshProUGUI hintMessage;
    public ReadyButton readyButton;
   

    private void Awake()
    {
        // Making GameManager accessible from anywhere
        instance = this;
    }
    void Start()
    {
        StartTurn();
    }

    public void TransitionGameState(GameState newState)
    {
        Debug.Log("GameManager: Transitioning to state: " + newState.ToString());

        // Changing the state
        currentState = newState;

        // Setting the button correctly
        readyButton.UpdateButtonState();

        // Moving the camera
        Camera.main.GetComponent<Viewpoint>().ChangeViewpoint(GetState());

        // Applying new state to the game
        switch (currentState)
        {
            case GameState.PLACING:
                break;
            case GameState.PLANNING:
                // Making field cards appear correctly
                planningManager.UpdateFieldHandVisuals();

                break;
            case GameState.EXECUTING:
                // passing cards in the correct order to executing manager
                executeManager.LoadCardStack(planningManager.cardsOnField);

                // button is only available after choosing an ability
                readyButton.gameObject.SetActive(false);

                break;
            case GameState.BATTLING:
                break;
            case GameState.BUYING:
                break;
        }

        // temp
        StartTurn();
    }

    private void Update()
    {
        DebugStateInput();
    }

    void DebugStateInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !yourTurn) StartTurn();

        if (Input.GetKeyDown(KeyCode.O)) opponentEndStateReady = true;
    }

    public void EndTurn()
    {
        Debug.Log("GameManager: Ending the turn");

        yourTurn = false;

        // updating UI
        readyButton.gameObject.SetActive(false);
        managerUI.NewHint("It's your opponents turn (space to skip)");
        managerUI.UpdateTurnMessage();

        // state specific effects
        switch (currentState)
        {
            case GameState.EXECUTING:
                executeManager.StopRevealCard();

                // checking if there are any more cards prepared
                if (executeManager.plannedCardStack.Count <= 0) endStateReady = true;
                break;
        }

        // debug solution for transitioning states
        CheckEndState();
    }

    public void StartTurn()
    {
        Debug.Log("GameManager: Starting the turn");

        yourTurn = true;

        // updating UI
        readyButton.gameObject.SetActive(true);
        managerUI.NewHint(GetState().defaultHintText);
        managerUI.UpdateTurnMessage();

        // state specific effects
        switch (GameManager.instance.currentState)
        {
            case GameState.PLACING:
                // disables "finish placing" button if there are no units on player's side
                if (planningManager.cardsOnField.Count <= 0) readyButton.gameObject.SetActive(false);
                break;

            case GameState.EXECUTING:
                executeManager.NextCardReady();
                readyButton.gameObject.SetActive(false);
                break;
        }
    }

    /// <summary>
    /// Transitions to next state if both players are finished with current one, or restarts the turn if only opponent is finished
    /// </summary>
    void CheckEndState()
    {
        if (endStateReady && opponentEndStateReady) FinishCurrentState();
        else if (opponentEndStateReady) StartTurn();
    }


    public GameStateData GetState()
    {
        return gameStates[(int)currentState];
    }

    void FinishCurrentState()
    {
        Debug.Log("GameManager: wrapping up state: " + currentState.ToString());
        // resetting turn logic values
        opponentEndStateReady = false;
        endStateReady = false;

        // disabling current state UI 
        managerUI.EnableUI(false);

        // Deciding which next state should be
        GameState nextGameState = ((int)currentState + 1 < gameStates.Count) ? (GameState)(currentState + 1) : GameState.PLACING;
        TransitionGameState(nextGameState);
    }
}
