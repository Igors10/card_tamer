using System;
using System.Collections;
using FishNet;
using FishNet.Managing.Scened;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Runs the Lobby scene UI: the connect screen (nickname, lobby code) and the lobby screen (two player cards, ready and start buttons).
/// Not networked itself, it only reads LobbyClient values and calls their ServerRpcs. Server side it also starts the match.
/// </summary>
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager instance;

    [Header("refs")]
    [SerializeField] LoadingFog loadingFog;
    [SerializeField] PlayerConfigObj playerConfig;

    [Header("connect screen")]
    [SerializeField] GameObject connectScreen; // only shows "opening/looking for the lobby", and host errors with a retry button
    [SerializeField] TMP_Text connectTitle;
    [SerializeField] Button connectButton; // host only: try opening the lobby again
    [SerializeField] TMP_Text connectButtonText;
    [SerializeField] TMP_Text connectStatusText;
    [SerializeField] Button backButton;

    [Header("lobby screen")]
    [SerializeField] GameObject lobbyScreen;
    [SerializeField] LobbyPanel[] panels = new LobbyPanel[2]; // by seat
    [SerializeField] Button readyButton;
    [SerializeField] TMP_Text readyButtonText;
    [SerializeField] Button startButton;
    [SerializeField] Button leaveButton;
    [SerializeField] TMP_Text hintText;
    [FormerlySerializedAs("hostIPNote")][SerializeField] GameObject codeNote; // host's sticky note with the lobby code
    [FormerlySerializedAs("hostIPText")][SerializeField] TMP_Text codeText;
    [SerializeField] Button copyCodeButton;
    [SerializeField] TMP_Text copyCodeButtonText;

    [Header("match start")]
    [SerializeField] float matchStartDelay = 1.2f; // server waits for the fog to cover the screen before switching scenes

    [HideInInspector] public bool matchStarting = false;
    bool connecting = false;
    bool leavingOnPurpose = false;
    bool hadLobbyClient = false; // true once our own LobbyClient got spawned, used to tell "host left" from "couldn't join"
    bool fogCovering = true; // the scene starts covered by the loading fog (ApplyLoadingFog only moves it relative to where it is, so tracking it here)
    Coroutine transition;
    Coroutine copiedRoutine;

    private void Awake()
    {
        instance = this;

        connectButton.onClick.AddListener(ConnectButton);
        backButton.onClick.AddListener(BackToMainMenu);
        readyButton.onClick.AddListener(ReadyButton);
        startButton.onClick.AddListener(StartButton);
        leaveButton.onClick.AddListener(LeaveButton);
        copyCodeButton.onClick.AddListener(CopyCodeButton);
    }

    private void OnEnable()
    {
        ConnectionScript.OnLocalConnectionStopped += OnLocalConnectionStopped;
    }

    private void OnDisable()
    {
        ConnectionScript.OnLocalConnectionStopped -= OnLocalConnectionStopped;
    }

    private void Start()
    {
        ShowConnectScreen("");

        // the fog stays over the screen while the lobby is being created/found, it lifts once we're in (see Refresh)
        loadingFog.gameObject.SetActive(true);
        fogCovering = true;
        loadingFog.StartLoading(GetLoadingMessage());

        // nothing to type here: hosts open the lobby right away, guests join with the code typed in the main menu
        StartCoroutine(ConnectNextFrame());
    }

    IEnumerator ConnectNextFrame()
    {
        yield return null; // letting ConnectionScript subscribe to its connection events first
        Connect();
    }

    string GetLoadingMessage() => GameData.hostingLobby ? "Creating a lobby..." : "Looking for a lobby...";

    /// <summary>
    /// Lifts the loading fog (when the lobby opened, or to show a host error)
    /// </summary>
    void UncoverScreen()
    {
        if (!fogCovering) return;
        fogCovering = false;

        loadingFog.StopLoading();
        StartCoroutine(loadingFog.ApplyLoadingFog(false));
    }

    /// <summary>
    /// Brings the loading fog down, optionally with the loading animation and a message
    /// </summary>
    IEnumerator CoverScreen(string loadingMessage)
    {
        if (loadingMessage != null) loadingFog.StartLoading(loadingMessage);
        if (fogCovering) yield break;

        fogCovering = true;
        yield return StartCoroutine(loadingFog.ApplyLoadingFog(true));
    }

    // ===============
    // CONNECT SCREEN
    // ===============

    void ShowConnectScreen(string status)
    {
        connectScreen.SetActive(true);
        lobbyScreen.SetActive(false);

        connectTitle.text = GameData.hostingLobby ? "Host Match" : "Join Lobby";
        connectButtonText.text = "Open lobby";
        connectButton.gameObject.SetActive(GameData.hostingLobby && status != ""); // retry button, only after a host error

        connecting = false;
        connectButton.interactable = true;
        connectStatusText.text = status;
    }

    void ConnectButton()
    {
        if (connecting || transition != null) return;
        AudioManager.instance.PlaySFX("ButtonSFX");
        StartCoroutine(RetryHosting());
    }

    IEnumerator RetryHosting()
    {
        connectButton.gameObject.SetActive(false);
        yield return StartCoroutine(CoverScreen(GetLoadingMessage()));
        Connect();
    }

    /// <summary>
    /// Hosts or joins. Nickname and lobby code come from the main menu (GameData.nickname, GameData.joinCode)
    /// </summary>
    async void Connect()
    {
        if (connecting || ConnectionScript.connectionManager == null) return;

        bool hosting = GameData.hostingLobby;
        connecting = true;
        connectButton.gameObject.SetActive(false);
        leavingOnPurpose = false;
        hadLobbyClient = false;

        // talking to Unity Relay takes a moment, the game keeps running while waiting (async/await)
        // the fog with "creating/looking for a lobby" covers the screen meanwhile, the lobby screen opens once our LobbyClient spawns (see Refresh)
        try
        {
            if (hosting) await ConnectionScript.connectionManager.HostAsync();
            else await ConnectionScript.connectionManager.JoinAsync(GameData.joinCode);
        }
        catch (Exception exception)
        {
            // scene could have been left while waiting
            if (this == null || transition != null) return;

            string message = ConnectionScript.GetErrorMessage(exception, !hosting);
            if (ConnectionScript.connectionManager != null) ConnectionScript.connectionManager.Leave();

            // hosts get a retry button here, guests go back to the code field in the main menu
            if (hosting) ShowHostError(message);
            else ReturnToMainMenu(message);
        }
    }

    /// <summary>
    /// Host couldn't open the lobby: lifting the fog to show what went wrong and the retry button
    /// </summary>
    void ShowHostError(string message)
    {
        ShowConnectScreen(message);
        UncoverScreen();
    }

    /// <summary>
    /// Our connection ended, either because we pressed leave, the host left, the lobby was full or connecting failed
    /// </summary>
    void OnLocalConnectionStopped(bool wasConnected)
    {
        if (matchStarting || transition != null) return;

        string message;
        if (leavingOnPurpose) message = "";
        else if (hadLobbyClient) message = "The host closed the lobby";
        else if (wasConnected) message = "Couldn't join, the lobby is full or the match has started";
        else message = "Couldn't connect to the lobby, try again";

        leavingOnPurpose = false;
        hadLobbyClient = false;

        // a host who lost the connection stays here and can open a new lobby, guests go back to the main menu
        if (GameData.hostingLobby) ShowHostError(message);
        else ReturnToMainMenu(message);
    }

    void BackToMainMenu()
    {
        if (transition != null) return;
        AudioManager.instance.PlaySFX("ButtonSFX");
        ReturnToMainMenu("");
    }

    /// <summary>
    /// Leaves the lobby scene. A message (wrong code, host left...) is shown by the code field in the main menu
    /// </summary>
    void ReturnToMainMenu(string message)
    {
        if (transition != null) return;
        GameData.menuMessage = message;
        transition = StartCoroutine(BackToMainMenuSequence());
    }

    IEnumerator BackToMainMenuSequence()
    {
        leavingOnPurpose = true;
        yield return StartCoroutine(CoverScreen(null));

        // removing the NetworkManager, the next lobby makes a fresh one
        if (ConnectionScript.connectionManager != null) ConnectionScript.connectionManager.Shutdown();

        UnityEngine.SceneManagement.SceneManager.LoadScene("Main Menu");
    }

    // ===============
    // LOBBY SCREEN
    // ===============

    /// <summary>
    /// Redraws the lobby from the LobbyClient values. Called whenever any of them changes, or a player joins/leaves
    /// </summary>
    public void Refresh()
    {
        LobbyClient local = LobbyClient.GetLocal();

        // our LobbyClient showing up means we're in, switching to the lobby screen
        if (local != null && !hadLobbyClient)
        {
            hadLobbyClient = true;
            connecting = false;
            connectScreen.SetActive(false);
            lobbyScreen.SetActive(true);
            UncoverScreen();
        }
        if (!lobbyScreen.activeSelf) return;

        // player cards
        LobbyClient host = LobbyClient.GetBySeat(0);
        LobbyClient guest = LobbyClient.GetBySeat(1);
        panels[0].Show(host, host != null && host == local);
        panels[1].Show(guest, guest != null && guest == local);

        bool bothJoined = host != null && guest != null;
        bool bothReady = bothJoined && host.ready.Value && guest.ready.Value;
        bool localIsHost = local != null && local.seat.Value == 0;

        // ready button flips between ready and not ready, it only works once the other player is here
        readyButton.gameObject.SetActive(local != null);
        SetButtonEnabled(readyButton, bothJoined && !matchStarting);
        if (local != null) readyButtonText.text = local.ready.Value ? "Not ready" : "Ready";

        // only the host has a start button, it works once both players are ready
        startButton.gameObject.SetActive(localIsHost);
        SetButtonEnabled(startButton, bothReady && !matchStarting);

        // the host sees the lobby code to send to the other player
        string code = ConnectionScript.connectionManager != null ? ConnectionScript.connectionManager.joinCode : "";
        codeNote.SetActive(localIsHost && code != "");
        codeText.text = "Lobby code:\n<size=150%>" + code + "</size>";

        if (!bothJoined) hintText.text = "Waiting for another player to join...";
        else if (!bothReady) hintText.text = "Waiting for both players to get ready";
        else if (localIsHost) hintText.text = "Everyone is ready!";
        else hintText.text = "Waiting for the host to start";
    }

    void CopyCodeButton()
    {
        if (ConnectionScript.connectionManager == null) return;
        AudioManager.instance.PlaySFX("ButtonSFX");

        // putting the code on the clipboard, ready to paste into Discord
        GUIUtility.systemCopyBuffer = ConnectionScript.connectionManager.joinCode;

        if (copiedRoutine != null) StopCoroutine(copiedRoutine);
        copiedRoutine = StartCoroutine(ShowCopied());
    }

    IEnumerator ShowCopied()
    {
        copyCodeButtonText.text = "Copied!";
        yield return new WaitForSeconds(1.5f);
        copyCodeButtonText.text = "Copy";
        copiedRoutine = null;
    }

    void SetButtonEnabled(Button button, bool enabled)
    {
        button.interactable = enabled;
        CanvasGroup group = button.GetComponent<CanvasGroup>();
        if (group != null) group.alpha = enabled ? 1f : 0.45f;
    }

    void ReadyButton()
    {
        LobbyClient local = LobbyClient.GetLocal();
        if (local == null || matchStarting) return;

        AudioManager.instance.PlaySFX("ButtonSFX");
        local.SetReady(!local.ready.Value);
    }

    void StartButton()
    {
        LobbyClient local = LobbyClient.GetLocal();
        if (local == null || matchStarting) return;

        AudioManager.instance.PlaySFX("ButtonSFX");
        SetButtonEnabled(startButton, false); // stops double clicks, server answer re-enables it if starting failed
        local.RequestStartMatch();
    }

    void LeaveButton()
    {
        // leaving the lobby goes back to the main menu (closing the lobby if we're the host)
        if (matchStarting) return;
        BackToMainMenu();
    }

    // ===============
    // STARTING THE MATCH
    // ===============

    /// <summary>
    /// Server only: starts the match if both players are in and ready
    /// </summary>
    public void TryStartMatchServer(LobbyClient requester)
    {
        if (matchStarting) return;

        LobbyClient host = LobbyClient.GetBySeat(0);
        LobbyClient guest = LobbyClient.GetBySeat(1);
        if (host == null || guest == null || !host.ready.Value || !guest.ready.Value)
        {
            Refresh();
            return;
        }

        // nobody else can join from now on
        ConnectionScript.connectionManager.acceptingPlayers = false;

        requester.MatchStartingRpc();
        StartCoroutine(LoadBoardAfterDelay());
    }

    IEnumerator LoadBoardAfterDelay()
    {
        yield return new WaitForSeconds(matchStartDelay);

        // replacing the lobby with the board for everyone connected
        SceneLoadData sceneLoadData = new SceneLoadData("Board") { ReplaceScenes = ReplaceOption.All };
        InstanceFinder.SceneManager.LoadGlobalScenes(sceneLoadData);
    }

    /// <summary>
    /// Every client: the server announced the match start. Saving lobby info for the board and covering the screen
    /// </summary>
    public void OnMatchStarting()
    {
        matchStarting = true;
        Refresh();

        LobbyClient local = LobbyClient.GetLocal();
        if (local != null) GameData.localSeat = local.seat.Value;
        for (int s = 0; s < GameData.seatNicknames.Length; s++)
        {
            LobbyClient client = LobbyClient.GetBySeat(s);
            GameData.seatNicknames[s] = client != null ? client.nickname.Value : "";
        }

        // fresh card config like in offline matches
        playerConfig.ResetCardConfig();
        playerConfig.offlineMatch = false;

        AudioManager.instance.PlaySFX("StartGameSFX");
        StartCoroutine(CoverScreen(null));
    }
}
