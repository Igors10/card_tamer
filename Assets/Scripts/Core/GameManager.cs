using System;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;
using Unity.Multiplayer.Playmode;

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
    [HideInInspector] public Player firstPlayer; // starts every placing phase. Offline always the player, online chosen by the server
    bool matchStarted; // online matches wait for MatchNetwork to start them
    [HideInInspector] public int stateNr; // counts state transitions, online commands wait until both games are in the same state
    [HideInInspector] public bool waitingForRemote; // online: the other player's turn, their next move can be applied
    [HideInInspector] public bool gameOver;
    [HideInInspector] public bool opponentLeft;
    [SerializeField] float opponentLeftMenuDelay = 2f; // online: how long "left the match" stays on the fog before going to the main menu
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
    public DiscardManager discardManager;

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
    public int startingCardAmount = 4;
    public int maxHandSize;
    public PlayerConfigObj playerConfig;
    
    //[Header("events")]
    public static event Action OnStateTransition;
    public static event Action OnRoundEnd;
    public static event Action<Card, int> OnDiceRolled;
    public static event Action<Card> OnCardPlayed;
    public static event Action<Card> OnCardPlayResolved;
    public static event Action OnLineResolved;
    public static event Action<Card> OnCardHovered;

    private void Awake()
    {
        // Making GameManager accessible from anywhere
        instance = this;

        // not connected to anyone (Board started straight from the editor, the config asset can still say online from an earlier test): bot match
        ConnectionScript connection = ConnectionScript.connectionManager;
        if (connection == null || !connection.networkManager.ClientManager.Started) playerConfig.offlineMatch = true;

        // fresh gameplay random numbers for every match (online, the host's seed will replace this)
        GameRandom.Init(GameRandom.NewSeed());

        mainCamera = Camera.main.GetComponent<Viewpoint>();
    }
    void Start()
    {
        // in offline matches player always goes first
        if (playerConfig.offlineMatch)
        {
            firstPlayer = player;
            matchStarted = true;
            StartTurn();
        }
        StartCoroutine(StartGame());
    }

    private void OnEnable()
    {
        // host hears it from the server, the guest from its own connection stopping (host left or connection lost)
        ConnectionScript.OnRemotePlayerLeft += OnOpponentLeft;
        ConnectionScript.OnLocalConnectionStopped += OnLocalConnectionStopped;
    }

    private void OnDisable()
    {
        ConnectionScript.OnRemotePlayerLeft -= OnOpponentLeft;
        ConnectionScript.OnLocalConnectionStopped -= OnLocalConnectionStopped;
    }

    void OnLocalConnectionStopped(bool wasConnected) => OnOpponentLeft();

    /// <summary>
    /// Online: the other player is gone. Before the match starts back to the main menu, during it the game ends
    /// </summary>
    void OnOpponentLeft()
    {
        if (playerConfig.offlineMatch || opponentLeft) return;
        opponentLeft = true;

        // left after the game ended, nobody to restart with
        if (gameOver) { managerUI.EnableRematch(false); return; }

        string message = opponent.playerName + " left the match";
        Debug.Log("GameManager: " + message);

        if (!matchStarted) StartCoroutine(OpponentLeftBeforeStart(message));
        else
        {
            gameOver = true;
            managerUI.GameOverScreen(opponent, message);
        }
    }

    IEnumerator OpponentLeftBeforeStart(string message)
    {
        loadingFog.StartLoading(message);
        yield return new WaitForSeconds(opponentLeftMenuDelay);
        SceneManager.LoadScene("Main Menu");
    }

    /// <summary>
    /// Online: called on both players by MatchNetwork once both have loaded the Board
    /// </summary>
    public void StartOnlineMatch(int seed, int firstSeat)
    {
        GameRandom.Init(seed);
        firstPlayer = (firstSeat == GameData.localSeat) ? player : opponent;
        Debug.Log("GameManager: online match started, seat " + firstSeat + " goes first, you are seat " + GameData.localSeat);

        matchStarted = true;
        StartTurn();
    }

    IEnumerator StartGame()
    {
        loadingFog.gameObject.SetActive(true);

        // online, the fog stays until the other player has loaded too
        if (!matchStarted)
        {
            loadingFog.StartLoading("Waiting for opponent...");
            while (!matchStarted) yield return null;
            loadingFog.StopLoading();
        }

        // Loading fog fading away effect
        yield return StartCoroutine(loadingFog.LoadingAnimation(loadingTime));

        // "workshop" text
        managerUI.StateChangeMessage("Workshop");       

        // pause while intro text is on the screen
        while (managerUI.stateTransitionObj.activeSelf)
        {
            yield return null;
        }

        managerUI.workshop.PlayWelcomeDialogue();

        // starting workshop starting sequence after a short pause
        //managerUI.workshop.LaunchStartingSequence();

        // refreshing player UI
        player.playerUI.Refresh();
    }


    public void TransitionGameState(GameState newState)
    {
        Debug.Log("GameManager: Transitioning to state: " + newState.ToString());

        // Changing the state
        currentState = newState;
        stateNr++;

        // online both games should be exactly the same here, the server compares them
        if (!playerConfig.offlineMatch) MatchNetwork.instance.CheckSync();

        // Enabling new UI 
        managerUI.EnableUI(true);
        managerUI.Refresh();

        // Setting the button correctly
        readyButton.UpdateButtonState();

        // Moving the camera
        //if (currentState != GameState.BATTLING)
        //mainCamera.ChangeViewpoint(GetState());   No longer there is a need to move the camera between game states

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

                // Rat tutorial
                if (GameData.firstTimePlaying) StartCoroutine(RatSmart.instance.PlayDialogue("PlaceExplainDial"));
                break;

            case GameState.BATTLING:
                managerUI.EnableUI(true);
                managerUI.turnHint.gameObject.SetActive(false);
                battleManager.ResetBattleVals();
                battleManager.NextLine();

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
                managerUI.turnHint.gameObject.SetActive(false);

                // resetting shop values
                shopManager.ResetShopValues();
                managerUI.EnableUI(true);
                managerUI.EnableWorkshop(true);
                readyButton.gameObject.SetActive(true);

                // opponent buys cards
                if (opponent.isAI) StartCoroutine(opponent.GetComponent<AIOpponent>().DrawNewCards());
                break;
        }

        // trigger the event
        OnStateTransition?.Invoke();

        // placing starts with the first player, in the other states the player acts right away
        Player startingPlayer = (currentState == GameState.PLACING) ? firstPlayer : player;
        startingPlayer.StartTurn();
    }

    void RoundEnd()
    {
        // Resets field state for next round
        player.EndRound();
        opponent.EndRound();

        OnRoundEnd?.Invoke();
        roundNr++;
    }

    /// <summary>
    /// Triggers at the beginning of each round
    /// </summary>
    void RoundStart()
    {
        player.StartRound();
        opponent.StartRound();

        fieldManager.Refresh();
    }
    private void Update()
    {
        DebugStateInput();
    }

    void DebugStateInput()
    {
        if (Input.GetKeyDown(KeyCode.O)) opponent.endStateReady = true;
    }

    public void EndTurn()
    {
        yourTurn = false;

        // updating UI
        readyButton.gameObject.SetActive(false);
        managerUI.NewHint("It's your opponents turn");
        managerUI.UpdateTurnMessage();

        // debug solution for transitioning states
        CheckEndState();
    }
   
    /// <summary>
    /// Online: the other player's turn. Waits until their moves arrive over the network
    /// </summary>
    public void StartRemoteTurn()
    {
        Debug.Log("GameManager: [" + opponent.playerName + "] (online) starting the turn.");

        yourTurn = false;
        waitingForRemote = true;

        // updating UI
        readyButton.gameObject.SetActive(false);
        managerUI.NewHint("It's your opponents turn");
        managerUI.UpdateTurnMessage();
    }

    /// <summary>
    /// Online: the other player's turn ended (their move arrived over the network)
    /// </summary>
    public void EndRemoteTurn()
    {
        // we're done with this state, so the opponent goes again or the state ends
        if (player.endStateReady) CheckEndState();

        // in the workshop both act at the same time, so it can already be our turn
        else if (!yourTurn) player.StartTurn();
    }

    /// <summary>
    /// Player is finished with the current state (ready button). Online it arrives as a command for both players
    /// </summary>
    public void PlayerReady(Player p)
    {
        p.endStateReady = true;

        if (p.isRemote) EndRemoteTurn();
        else EndTurn();
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
                GetCurrentPlayer().cardPlayed = false;

                // disables "finish placing" button if there are no units on player's side
                if (player.cardsOnField.Count <= 0) readyButton.gameObject.SetActive(false);

                // automatically pressing "Finish placing" when no more cards in hand
                // online it's sent like a button press, the other game has to know too (and checks its own hand itself)
                else if (player.cardsInHand.Count <= 0 && !playerConfig.offlineMatch)
                {
                    readyButton.gameObject.SetActive(false);
                    MatchNetwork.instance.SendReady();
                }
                else if (player.cardsInHand.Count <= 0)
                {
                    player.endStateReady = true;

                    // checking if opponent has any cards left, ending their turn right away if has no cards
                    if (opponent.cardsInHand.Count <= 0) opponent.endStateReady = true;
                        
                    EndTurn(); 
                }

                // otherwise resetting its visuals
                else readyButton.UpdateButtonState();
                
                break;

            case GameState.BATTLING:
                
                readyButton.gameObject.SetActive(false);
                break;

            case GameState.BUYING:
             
                break;
        }
    }

    /// <summary>
    /// Transitions to next state if both players are finished with current one, or restarts the turn if only opponent is finished
    /// </summary>
    public void CheckEndState() // rework it so that it keeps track of the current players and stores both players in a list
    {
        // Do nothing if game over
        if (gameOver) return;

        // go to next state if both players are finished with the current one
        if (player.endStateReady && opponent.endStateReady) FinishCurrentState();

        // start player's turn
        else if (opponent.endStateReady) player.StartTurn();

        // start opponent's turn
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

    public void BroadcastOnCardPlayResolved(Card card)
    {
        OnCardPlayResolved?.Invoke(card);
    }

    public void BroadcastOnDiceRolled(Card card, int diceResult)
    {
        OnDiceRolled?.Invoke(card, diceResult);
    }

    public void BroadcastOnLineResolved()
    {
        OnLineResolved?.Invoke();
    }

    public void BroadcastOnCardHovered(Card card)
    {
        OnCardHovered?.Invoke(card);
    }
}
