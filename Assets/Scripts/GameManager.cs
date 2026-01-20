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
    public bool yourTurn;

    [Header("Managers")]
    public HandManager handManager;
    public CardGenerator cardGenerator;
    public FieldManager fieldManager;

    [Header("refs")]
    [SerializeField] TextMeshProUGUI hintMessage;
    void Start()
    {
        // Making GameManager accessible from anywhere
        instance = this;

        StartTurn();
    }

    public void TransitionGameState(GameState newState)
    {
        // Changing the state
        currentState = newState;

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
    }

    public void EndTurn()
    {
        yourTurn = false;

        hintMessage.text = "It's your opponents turn (space to skip)";
    }

    public void StartTurn()
    {
        yourTurn = true;

        hintMessage.text = gameStates[(int)currentState].defaultHintText;
    }

    void FinishCurrentState()
    {
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
    // Moving camera angles??

    //Battle camera- (Position[0, 8, -7], Rotation[40, 0, 0])
}
