using UnityEngine;

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

    public GameState state;

    [Header("Managers")]
    public HandManager handManager;
    public CardGenerator cardGenerator;
    public FieldManager fieldManager;
    void Start()
    {
        // Making GameManager accessible from anywhere
        instance = this;
    }

    public void TransitionGameState(GameState newState)
    {
        // Changing the state
        state = newState;

        // Applying new state to the game
        switch (state)
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

    void FinishCurrentState()
    {
        switch (state)
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
