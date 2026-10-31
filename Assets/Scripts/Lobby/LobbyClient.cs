using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

/// <summary>
/// One of these gets spawned for every connected player (by the PlayerSpawner on the NetworkManager).
/// It holds that player's lobby info. The server owns the values, clients ask the server to change them through ServerRpcs.
/// </summary>
public class LobbyClient : NetworkBehaviour
{
    public const int maxNicknameLength = GameData.maxNicknameLength;

    // every LobbyClient that exists on this machine (host sees both, guest sees both too once they're spawned)
    public static readonly List<LobbyClient> all = new List<LobbyClient>();

    public readonly SyncVar<string> nickname = new SyncVar<string>("");
    public readonly SyncVar<bool> ready = new SyncVar<bool>();
    public readonly SyncVar<int> seat = new SyncVar<int>(-1); // 0 = host, 1 = guest

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        all.Add(this);

        nickname.OnChange += OnValueChanged;
        ready.OnChange += OnValueChanged;
        seat.OnChange += OnValueChanged;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        all.Remove(this);

        nickname.OnChange -= OnValueChanged;
        ready.OnChange -= OnValueChanged;
        seat.OnChange -= OnValueChanged;

        RefreshLobby();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // the host's own connection is always seat 0, others get the first free seat
        // (on a dedicated server nobody is local, so whoever joins first gets seat 0 and can start the match)
        seat.Value = Owner.IsLocalClient ? 0 : FindFreeSeat();
        nickname.Value = DefaultNickname();
    }

    int FindFreeSeat()
    {
        for (int s = 0; s < ConnectionScript.maxPlayers; s++)
        {
            LobbyClient taken = GetBySeat(s);
            if (taken == null || taken == this) return s;
        }
        return -1;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        // someone left the lobby, the remaining player has to confirm again once a new player joins
        if (LobbyManager.instance == null || LobbyManager.instance.matchStarting) return;
        foreach (LobbyClient other in all)
        {
            if (other != this) other.ready.Value = false;
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // sending our nickname to the server, which then syncs it to everyone
        if (IsOwner) SetNickname(GameData.nickname);

        RefreshLobby();
    }

    void OnValueChanged<T>(T previous, T next, bool asServer)
    {
        // host gets every change twice (as server and as client), the client one is enough for the UI
        if (asServer) return;
        RefreshLobby();
    }

    void RefreshLobby()
    {
        if (LobbyManager.instance != null) LobbyManager.instance.Refresh();
    }

    string DefaultNickname() => "Player " + (seat.Value + 1);

    /// <summary>
    /// Returns this machine's own LobbyClient, null if it isn't spawned yet
    /// </summary>
    public static LobbyClient GetLocal()
    {
        foreach (LobbyClient client in all) if (client.IsOwner) return client;
        return null;
    }

    /// <summary>
    /// Returns LobbyClient sitting in given seat, null if the seat is empty
    /// </summary>
    public static LobbyClient GetBySeat(int seatID)
    {
        foreach (LobbyClient client in all) if (client.seat.Value == seatID) return client;
        return null;
    }

    // ===============
    // CLIENT -> SERVER
    // ===============

    [ServerRpc]
    public void SetNickname(string value)
    {
        value = (value ?? "").Trim();
        if (value.Length > maxNicknameLength) value = value.Substring(0, maxNicknameLength);
        nickname.Value = (value == "") ? DefaultNickname() : value;
    }

    [ServerRpc]
    public void SetReady(bool value)
    {
        if (LobbyManager.instance != null && LobbyManager.instance.matchStarting) return;

        // can't get ready alone, the other player has to be in the lobby
        if (value && (GetBySeat(0) == null || GetBySeat(1) == null)) return;
        ready.Value = value;
    }

    [ServerRpc]
    public void RequestStartMatch()
    {
        // only the host can start, the server checks everything again in case the client was out of date
        if (seat.Value != 0 || LobbyManager.instance == null) return;
        LobbyManager.instance.TryStartMatchServer(this);
    }

    // ===============
    // SERVER -> CLIENTS
    // ===============

    /// <summary>
    /// Tells every client the match is about to start, so they save the lobby info and cover the screen
    /// </summary>
    [ObserversRpc]
    public void MatchStartingRpc()
    {
        if (LobbyManager.instance != null) LobbyManager.instance.OnMatchStarting();
    }
}
