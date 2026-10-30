using System;
using System.Threading.Tasks;
using UnityEngine;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

/// <summary>
/// Sits on the NetworkManager object and moves between scenes together with it.
/// Hosts and joins through Unity Relay with a lobby code, and turns away anyone past the second player.
/// </summary>
public class ConnectionScript : MonoBehaviour
{
    public static ConnectionScript connectionManager;
    public const int maxPlayers = 2;
    const string relayConnectionType = "dtls"; // encrypted UDP through the relay

    [HideInInspector] public bool acceptingPlayers = true; // switched off once the match starts, so nobody can join mid game

    /// <summary>
    /// Code of the lobby this machine is hosting, empty when not hosting
    /// </summary>
    public string joinCode { get; private set; } = "";

    public NetworkManager networkManager { get; private set; }
    UnityTransport unityTransport;
    bool wasConnected = false;

    /// <summary>
    /// Fires on this machine when its client connection is fully started
    /// </summary>
    public static event Action OnLocalConnectionStarted;

    /// <summary>
    /// Fires on this machine when its client connection stops. True if it was connected before (got kicked/host left), false if it never managed to connect
    /// </summary>
    public static event Action<bool> OnLocalConnectionStopped;

    private void Awake()
    {
        connectionManager = this;
        networkManager = GetComponent<NetworkManager>();
        unityTransport = GetComponent<UnityTransport>();
    }

    private void Start()
    {
        // managers get created in NetworkManager.Awake, so subscribing here
        networkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
    }

    private void OnDestroy()
    {
        if (connectionManager == this) connectionManager = null;
        if (networkManager == null) return;

        if (networkManager.ServerManager != null) networkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        if (networkManager.ClientManager != null) networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
    }

    // ===============
    // STARTING / STOPPING
    // ===============

    /// <summary>
    /// Opens a lobby on Unity Relay, then starts a server and joins it on this machine (host = server + client).
    /// Returns the lobby code for the other player. Throws if the online services can't be reached
    /// </summary>
    public async Task<string> HostAsync()
    {
        await SignInAsync();

        // reserving a spot on a relay server for the guest and getting a code that points to it
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
        string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        unityTransport.SetRelayServerData(allocation.ToRelayServerData(relayConnectionType));

        joinCode = code;
        acceptingPlayers = true;
        networkManager.ServerManager.StartConnection();
        networkManager.ClientManager.StartConnection(); // the host's own client talks to the server locally, not through the relay
        return code;
    }

    /// <summary>
    /// Finds the lobby with given code on Unity Relay and connects to it. Throws if the code is wrong or the services can't be reached
    /// </summary>
    public async Task JoinAsync(string code)
    {
        await SignInAsync();

        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(NormalizeCode(code));
        unityTransport.SetRelayServerData(allocation.ToRelayServerData(relayConnectionType));

        networkManager.ClientManager.StartConnection();
    }

    /// <summary>
    /// Disconnects from the lobby/match. Hosts also close the server, which kicks the other player
    /// </summary>
    public void Leave()
    {
        joinCode = "";
        if (networkManager.ClientManager.Started) networkManager.ClientManager.StopConnection();
        if (networkManager.ServerManager.Started) networkManager.ServerManager.StopConnection(true);
    }

    /// <summary>
    /// Leaves and removes the NetworkManager completely. Used when going back to the main menu, so the next lobby starts fresh
    /// </summary>
    public void Shutdown()
    {
        Leave();
        Destroy(gameObject);
    }

    // ===============
    // UNITY SERVICES
    // ===============

    /// <summary>
    /// Relay needs a signed in player. Players never see this: it's an anonymous account made on the spot
    /// </summary>
    static async Task SignInAsync()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            // every launch gets its own anonymous profile, so two copies of the game on one computer
            // (Multiplayer Play Mode, or two builds) don't share the same player and can join each other
            InitializationOptions options = new InitializationOptions();
            options.SetProfile("p" + Guid.NewGuid().ToString("N").Substring(0, 20));
            await UnityServices.InitializeAsync(options);
        }
        while (UnityServices.State == ServicesInitializationState.Initializing) await Task.Yield();

        if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    /// <summary>
    /// Codes are typed by people, so ignoring spaces and letter case
    /// </summary>
    public static string NormalizeCode(string code) => (code ?? "").Replace(" ", "").Replace("-", "").Trim().ToUpperInvariant();

    /// <summary>
    /// Turns an exception from HostAsync/JoinAsync into a message for the player
    /// </summary>
    public static string GetErrorMessage(Exception exception, bool joining)
    {
        Debug.LogWarning("ConnectionScript: " + exception);

        if (exception is RelayServiceException relayException)
        {
            switch (relayException.Reason)
            {
                case RelayExceptionReason.JoinCodeNotFound:
                case RelayExceptionReason.InvalidRequest:
                case RelayExceptionReason.InvalidArgument:
                case RelayExceptionReason.EntityNotFound:
                case RelayExceptionReason.AllocationNotFound:
                    if (joining) return "No lobby with that code";
                    break;
                case RelayExceptionReason.RateLimited:
                    return "Too many tries, wait a moment and try again";
            }
        }
        return "Couldn't reach the online service, check your internet connection";
    }

    // ===============
    // CONNECTION EVENTS
    // ===============

    /// <summary>
    /// Server side: someone connected or disconnected
    /// </summary>
    void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState != RemoteConnectionState.Started) return;

        // turning away a third player, or anyone joining after the match started
        if (!acceptingPlayers || networkManager.ServerManager.Clients.Count > maxPlayers)
        {
            Debug.Log("ConnectionScript: turning away connection " + conn.ClientId + ", lobby is full or the match has started");
            conn.Disconnect(true);
        }
    }

    /// <summary>
    /// This machine's client connected or disconnected
    /// </summary>
    void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            wasConnected = true;
            OnLocalConnectionStarted?.Invoke();
        }
        else if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            OnLocalConnectionStopped?.Invoke(wasConnected);
            wasConnected = false;
        }
    }
}
