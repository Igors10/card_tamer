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

    [Header("Managers")]
    public HandManager handManager;
    public CardGenerator cardGenerator;
    public FieldManager fieldManager;
    public PlanningManager planningManager;
    public ExecuteManager executeManager;
    public ManagerUI managerUI;
    public VFXManager VFXmanager;
    public BattleManager battleManager;

    [Header("UI stuff")]
    [SerializeField] TextMeshProUGUI hintMessage;
    public ReadyButton readyButton;

    [Header("players")]
    public Player player;
    public Player opponent;
    public int startingMaxHealth;
    public PlayerConfigObj playerConfig;
   

    private void Awake()
    {
        // Making GameManager accessible from anywhere
        instance = this;
    }
    void Start()
    {
        // in offline matches player always goes first
        if (playerConfig.offlineMatch) StartTurn();
    }

    public void TransitionGameState(GameState newState)
    {
        Debug.Log("GameManager: Transitioning to state: " + newState.ToString());

        // Changing the state
        currentState = newState;

        // Setting the button correctly
        readyButton.UpdateButtonState();

        // Moving the camera
        if (currentState != GameState.BATTLING)
        Camera.main.GetComponent<Viewpoint>().ChangeViewpoint(GetState());

        // Applying new state to the game
        switch (currentState)
        {
            case GameState.PLACING:
                break;
            case GameState.PLANNING:
                // Making field cards appear correctly
                planningManager.UpdateFieldHandVisuals(player);

                // Making AI shuffle the cards
                if (playerConfig.offlineMatch) opponent.StartTurn();

                break;
            case GameState.EXECUTING:
                // passing cards in the correct order to executing manager
                executeManager.LoadCardStack(player.cardsOnField, player);
                executeManager.LoadCardStack(opponent.cardsOnField, opponent);

                // button is only available after choosing an ability
                readyButton.gameObject.SetActive(false);

                break;
            case GameState.BATTLING:
                //battleManager.InitBattleLine();

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

        if (Input.GetKeyDown(KeyCode.O)) opponent.endStateReady = true;
    }

    public void EndTurn()
    {
        Debug.Log("GameManager: Ending the turn");

        // state specific effects
        switch (currentState)
        {
            case GameState.EXECUTING:
                executeManager.StopRevealCard();

                break;
        }

        yourTurn = false;

        // updating UI
        readyButton.gameObject.SetActive(false);
        managerUI.NewHint("It's your opponents turn (space to skip)");
        managerUI.UpdateTurnMessage();


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
                if (player.cardsOnField.Count <= 0) readyButton.gameObject.SetActive(false);
                break;

            case GameState.EXECUTING:
                executeManager.NextCardReady();
                readyButton.gameObject.SetActive(false);
                break;

            case GameState.BATTLING:
                battleManager.NextLine();
                break;
        }
    }

    /// <summary>
    /// Transitions to next state if both players are finished with current one, or restarts the turn if only opponent is finished
    /// </summary>
    public void CheckEndState()
    {
        if (player.endStateReady && opponent.endStateReady) FinishCurrentState();
        else if (opponent.endStateReady) StartTurn();
        else opponent.StartTurn();
    }

    /// <summary>
    /// Returns player object whos turn it is currently
    /// </summary>
    /// <returns></returns>
    public Player GetCurrentPlayer()
    {
        Player playerToReturn = (yourTurn) ? player : opponent;
        Debug.Log("GameManager: gives away player object, it is player's turn: " + yourTurn);
        return playerToReturn;
    }
    public GameStateData GetState()
    {
        return gameStates[(int)currentState];
    }

    void FinishCurrentState()
    {
        Debug.Log("GameManager: wrapping up state: " + currentState.ToString());
        // resetting turn logic values
        opponent.endStateReady = false;
        player.endStateReady = false;

        // disabling current state UI 
        managerUI.EnableUI(false);

        // Deciding which next state should be
        GameState nextGameState = ((int)currentState + 1 < gameStates.Count) ? (GameState)(currentState + 1) : GameState.PLACING;
        TransitionGameState(nextGameState);
    }
}
