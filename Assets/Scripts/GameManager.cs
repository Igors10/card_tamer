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
    [HideInInspector] public bool yourTurn;
    [HideInInspector] public bool endStateReady;
    [HideInInspector] public bool opponentEndStateReady; // debug thing

    [Header("Managers")]
    public HandManager handManager;
    public CardGenerator cardGenerator;
    public FieldManager fieldManager;

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
        // Changing the state
        currentState = newState;

        // Setting the button correctly
        readyButton.InitButtonState();

        // Applying new state to the game
        switch (currentState)
        {
            case GameState.PLACING:
                break;
            case GameState.PLANNING:
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
        yourTurn = false;
        readyButton.gameObject.SetActive(false);

        hintMessage.text = "It's your opponents turn (space to skip)";
    }

    public void StartTurn()
    {
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

    public GameStateData GetState()
    {
        return gameStates[(int)currentState];
    }

    void FinishCurrentState()
    {
        // resetting turn logic values
        opponentEndStateReady = false;
        endStateReady = false;

        // Deciding which next state should be
        GameState nextGameState = ((int)currentState + 1 < gameStates.Count) ? (GameState)(currentState + 1) : GameState.PLACING;
        TransitionGameState(nextGameState);
    }
    // Moving camera angles??

    //Battle camera- (Position[0, 8, -7], Rotation[40, 0, 0])
}
