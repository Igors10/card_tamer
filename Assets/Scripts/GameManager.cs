using System;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;

public enum GameState
{
    PLACING,
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
    public bool yourTurn;
    [HideInInspector] public bool gameOver;
    public int roundNr = 1;

    [Header("Managers")]
    public HandManager handManager;
    public CardGenerator cardGenerator;
    public FieldManager fieldManager;
    public PlanningManager planningManager;
    public ExecuteManager executeManager;
    public ManagerUI managerUI;
    public VFXManager VFXmanager;
    public BattleManager battleManager;
    public Animations animations;
    public ShopManager shopManager;
    public Viewpoint mainCamera;
    public CardDatabase cardDatabase;

    [Header("UI stuff")]
    [SerializeField] TextMeshProUGUI hintMessage;
    public ReadyButton readyButton;
    public LoadingFog loadingFog;
    [SerializeField] float loadingTime;

    [Header("players")]
    public Player player;
    public Player opponent;
    public int startingMaxHealth;
    public int startingStars;
    public int startingResourceAmount;
    public int startingCardAmount = 5;
    public int maxHandSize;
    public PlayerConfigObj playerConfig;
    
    //[Header("events")]
    public static event Action OnStateTransition;
    public static event Action OnRoundEnd;
    public static event Action<Card, int> OnDiceRolled;
    public static event Action<Card> OnCardPlayed;
    public static event Action OnLineResolved;

    private void Awake()
    {
        // Making GameManager accessible from anywhere
        instance = this;

        mainCamera = Camera.main.GetComponent<Viewpoint>();
    }
    void Start()
    {
        // in offline matches player always goes first
        if (playerConfig.offlineMatch) StartTurn();
        StartCoroutine(StartGame());
    }

    IEnumerator StartGame()
    {
        // Loading fog fading away effect
        loadingFog.gameObject.SetActive(true);
        yield return StartCoroutine(loadingFog.LoadingAnimation(loadingTime));

        // "Draw your minions" text
        managerUI.StateChangeMessage("Workshop");       

        // pause while intro text is on the screen
        while (managerUI.stateTransitionObj.activeSelf)
        {
            yield return null;
        }

        // starting workshop starting sequence after a short pause
        managerUI.workshop.LaunchStartingSequence();
    }


    public void TransitionGameState(GameState newState)
    {
        Debug.Log("GameManager: Transitioning to state: " + newState.ToString());

        // Changing the state
        currentState = newState;

        // Enabling new UI 
        managerUI.EnableUI(true);

        // Setting the button correctly
        readyButton.UpdateButtonState();

        // Moving the camera
        if (currentState != GameState.BATTLING)
        mainCamera.ChangeViewpoint(GetState());

        // Applying new state to the game
        switch (currentState)
        {
            case GameState.PLACING:
                // disabling resource UI after shop
                StartCoroutine(player.playerUI.ShowTokens(false));
                StartCoroutine(opponent.playerUI.ShowTokens(false));

                // playing placing soundtrack
                AudioManager.instance.PlaySoundtrack("AltMainTrack");

                managerUI.EnableWorkshop(false);

                // starting new round
                RoundStart();
                break;

            case GameState.BATTLING:
                managerUI.EnableUI(true);
                managerUI.turnHint.gameObject.SetActive(false);
                battleManager.ResetBattleVals();

                // playing workshop soundtrack
                AudioManager.instance.PlaySoundtrack("BattleTrack");

                break;

            case GameState.BUYING:
                // Ending round
                RoundEnd();

                // playing workshop soundtrack
                AudioManager.instance.PlaySoundtrack("WorkshopTrack");

                // Enabling hand UI
                gameStateUI[0].SetActive(true);

                // resetting shop values
                shopManager.skipStarAppearance = false;
                managerUI.EnableUI(true);
                managerUI.EnableWorkshop(true);
                readyButton.gameObject.SetActive(true);

                // opponent buys cards
                if (opponent.isAI) StartCoroutine(opponent.GetComponent<AIOpponent>().DrawNewCards());
                break;
        }

        // trigger the event
        OnStateTransition?.Invoke();

        // temp
        StartTurn();
    }

    void RoundEnd()
    {
        // Resets field state for next round
        player.EndRound();
        opponent.EndRound();

        OnRoundEnd?.Invoke();
    }

    /// <summary>
    /// Triggers at the beginning of each round
    /// </summary>
    void RoundStart()
    {
        player.StartRound();
        opponent.StartRound();

        fieldManager.Refresh();

        roundNr++;
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
        Debug.Log("GameManager: [" + player.playerName + "] starting the turn.");

        yourTurn = true;

        // updating UI
        managerUI.NewHint(GetState().defaultHintText);
        managerUI.UpdateTurnMessage();

        // state specific effects
        switch (GameManager.instance.currentState)
        {
            case GameState.PLACING:
                readyButton.gameObject.SetActive(true);

                // disables "finish placing" button if there are no units on player's side
                if (player.cardsOnField.Count <= 0) readyButton.gameObject.SetActive(false);
                // otherwise resetting its visuals
                else readyButton.UpdateButtonState();
                
                break;

            case GameState.BATTLING:
                battleManager.NextLine();
                readyButton.gameObject.SetActive(false);
                break;

            case GameState.BUYING:
             
                break;
        }
    }

    /// <summary>
    /// Transitions to next state if both players are finished with current one, or restarts the turn if only opponent is finished
    /// </summary>
    public void CheckEndState()
    {
        // Do nothing if game over
        if (gameOver) return;

        if (player.endStateReady && opponent.endStateReady) FinishCurrentState();
        else if (opponent.endStateReady) StartTurn();
        else opponent.StartTurn();
    }

    /// <summary>
    /// Ends the game
    /// </summary>
    public void GameOver(Player lostPlayer)
    {
        gameOver = true;
        managerUI.GameOverScreen(lostPlayer);
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

    /// <summary>
    /// Returns the opponent of given player
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public Player GetOpponentOfPlayer(Player p)
    {
       return (player == p) ? opponent : player;
    }

    public GameStateData GetState()
    {
        return gameStates[(int)currentState];
    }

    void FinishCurrentState()
    {
        Debug.Log("GameManager: wrapping up state: " + currentState.ToString());
        // resetting turn logic values
        opponent.FinishStatePlayer();
        player.FinishStatePlayer();

        // disabling current state UI 
        managerUI.EnableUI(false);

        // Deciding which next state should be
        //GameState nextGameState = ((int)currentState + 1 < gameStates.Count) ? (GameState)(currentState + 1) : GameState.PLACING;
        GameState nextGameState = (GameState)(((int)currentState + 1) % Enum.GetNames(typeof(GameState)).Length);
        TransitionGameState(nextGameState);
    }

    // EVENTS
    // ========
    public void BroadcastOnCardPlayed(Card card)
    {
        OnCardPlayed?.Invoke(card);
    }

    public void BroadcastOnDiceRolled(Card card, int diceResult)
    {
        OnDiceRolled?.Invoke(card, diceResult);
    }

    public void BroadcastOnLineResolved()
    {
        OnLineResolved?.Invoke();
    }
}
