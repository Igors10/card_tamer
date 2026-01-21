using Unity.VisualScripting;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

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

    [Header("refs")]
    [SerializeField] TextMeshProUGUI hintMessage;
    [SerializeField] ReadyButton readyButton;

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
        readyButton.InitButtonState();

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
                break;
            case GameState.BATTLING:
                break;
            case GameState.BUYING:
                break;
        }

    }

    private void Update()
    {
        DebugStateInput();
    }

    void DebugStateInput()
    {
        if (Input.GetKeyDown(KeyCode.Space)) StartTurn();

        if (Input.GetKeyDown(KeyCode.O)) opponentEndStateReady = true;
    }

    public void EndTurn()
    {
        Debug.Log("GameManager: Ending the turn");

        yourTurn = false;
        readyButton.gameObject.SetActive(false);

        hintMessage.text = "It's your opponents turn (space to skip)";

        // debug solution for transitioning states
        CheckEndState();
    }

    public void StartTurn()
    {
        Debug.Log("GameManager: Starting the turn");

        yourTurn = true;
        readyButton.gameObject.SetActive(true);

        hintMessage.text = GetState().defaultHintText;
    }

    /// <summary>
    /// Checks if a game needs to transition to next game state
    /// </summary>
    void CheckEndState()
    {
        if (endStateReady && opponentEndStateReady) FinishCurrentState();
    }

    /// <summary>
    /// Enabling or disabling all in-game interaction UI
    /// </summary>
    /// <param name="enable"></param>
    public void EnableUI(bool enable)
    {
        readyButton.gameObject.SetActive(enable);
        hintMessage.gameObject.SetActive(enable);
        gameStateUI[(int)currentState].SetActive(enable);
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
        EnableUI(false);

        // Deciding which next state should be
        GameState nextGameState = ((int)currentState + 1 < gameStates.Count) ? (GameState)(currentState + 1) : GameState.PLACING;
        TransitionGameState(nextGameState);
    }
}
