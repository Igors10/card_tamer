using FishNet;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using TMPro;
using UnityEngine;

public class LobbyClient : NetworkBehaviour   // Look into registering the client correctly
{
    //[HideInInspector] public int lobby_id;
    //[HideInInspector] public bool ready = false;
    [HideInInspector] public bool isHost = false;
    public LobbyPanel clientPanel;
    

    public readonly SyncVar<string> nickname = new SyncVar<string>();
    public readonly SyncVar<bool> ready = new SyncVar<bool>();
    public readonly SyncVar<int> lobbyID = new SyncVar<int>();
    public readonly SyncVar<int> characterSelected = new SyncVar<int>(); // 0- robber 1- ghost

    public override void OnStartServer()
    {
        base.OnStartServer();

        SetLobbyID(this);
    }
    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner && IsServer)
        {
            isHost = true; // marking this client as server
        }

        // Deciding which character selected by default
        SetCharacter(this, lobbyID.Value);

        // passing it to LobbyManager
        LobbyManager.instance.RegisterClient(this, IsOwner);

        // Assigning a lobby panel to this client
        clientPanel = LobbyManager.instance.lobbyPanel[lobbyID.Value];

        // Checking if client panel is null
        if (clientPanel == null) Debug.Log("LobbyClient: client panel of client " + 0 + " is equal to null");

        // Getting the nickname
        if (IsOwner) SetNickname(GameData.nickname);
        //else SetNickname(nickname.Value);
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        // subscribe a method group
        nickname.OnChange += OnNicknameChanged;
        ready.OnChange += OnReadyChange;
        characterSelected.OnChange += OnCharacterChanged;
    }

    /// <summary>
    /// Assigns id to the client in the lobby
    /// </summary>
    /// <param name="client"> </param>
    void SetLobbyID(LobbyClient client)
    {
        if (!IsServer) return;

        int id = ConnectionScript.connectionManager.nextID;
        Debug.Log("LobbyClient: client with id " + id + " joined the lobby");

        ConnectionScript.connectionManager.nextID++;
        client.lobbyID.Value = id;
    }
    private void OnNicknameChanged(string previous, string next, bool asServer)
    {
        LobbyManager.instance.Refresh(lobbyID.Value);
    }

    private void OnCharacterChanged(int previous, int next, bool asServer)
    {
        LobbyManager.instance.Refresh(lobbyID.Value);
    }

    void OnReadyChange(bool previous, bool next, bool asServer)
    {
        LobbyManager.instance.Refresh(lobbyID.Value);
    }


    [ServerRpc(RequireOwnership = false)]
    public void SetCharacter(LobbyClient client, int character_id)
    {
        client.characterSelected.Value = character_id;

        Debug.Log("LobbyClient: " + nickname.Value + " has selected character "+ character_id);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetNickname(string value)
    {
        //if (!IsServer) return;
        nickname.Value = value; // sync owner to all observers

        Debug.Log("LobbyClient: Nickname of panel " + lobbyID.Value + " was set to " + nickname.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetReady(bool value)
    {
        //if (!IsServer) return;
        ready.Value = value; // sync owner to all observers

        Debug.Log("LobbyClient: " + nickname.Value + (ready.Value ? "ready" : "not ready"));
    }

    /// <summary>
    /// Launches the gameplay scene and puts both clients in it
    /// </summary>
    [Server]
    public void StartMatch()
    {
        if (!IsServer) return;

        // Replace current (lobby) with gameplay for ALL connected clients
        var sld = new SceneLoadData("Board")
        {
            ReplaceScenes = ReplaceOption.All
        };

        InstanceFinder.SceneManager.LoadGlobalScenes(sld);
    }
    private void OnDestroy()
    {
        // unsubscribe
        nickname.OnChange -= OnNicknameChanged;
    }

}
