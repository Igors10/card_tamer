using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using TMPro;

public class LobbyClient : NetworkBehaviour   // Look into registering the client correctly
{
    //[HideInInspector] public int lobby_id;
    //[HideInInspector] public bool ready = false;
    [HideInInspector] public bool is_host = false;
    public LobbyPanel client_panel;
    

    public readonly SyncVar<string> nickname = new SyncVar<string>();
    public readonly SyncVar<bool> ready = new SyncVar<bool>();
    public readonly SyncVar<int> lobby_id = new SyncVar<int>();
    public readonly SyncVar<int> character_selected = new SyncVar<int>(); // 0- robber 1- ghost

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
            is_host = true; // marking this client as server
        }

        // Deciding which character selected by default
        SetCharacter(this, lobby_id.Value);

        // passing it to LobbyManager
        LobbyManager.instance.RegisterClient(this, IsOwner);

        // Assigning a lobby panel to this client
        client_panel = LobbyManager.instance.lobby_panel[lobby_id.Value];

        // Checking if client panel is null
        if (client_panel == null) Debug.Log("LobbyClient: client panel of client " + 0 + " is equal to null");

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
        character_selected.OnChange += OnCharacterChanged;
    }

    //[ServerRpc(RequireOwnership = false)]
    void SetLobbyID(LobbyClient client)
    {
        if (!IsServer) return;

        int id = ConnectionScript.connection_manager.nextID;
        Debug.Log("LobbyClient: client with id " + id + " joined the lobby");

        ConnectionScript.connection_manager.nextID++;
        client.lobby_id.Value = id;
    }
    private void OnNicknameChanged(string previous, string next, bool asServer)
    {
        LobbyManager.instance.Refresh(lobby_id.Value);
    }

    private void OnCharacterChanged(int previous, int next, bool asServer)
    {
        LobbyManager.instance.Refresh(lobby_id.Value);
    }

    void OnReadyChange(bool previous, bool next, bool asServer)
    {
        LobbyManager.instance.Refresh(lobby_id.Value);
    }


    [ServerRpc(RequireOwnership = false)]
    public void SetCharacter(LobbyClient client, int character_id)
    {
        client.character_selected.Value = character_id;

        Debug.Log("LobbyClient: " + nickname.Value + " has selected " + (character_id == 0 ? "robber" : "ghost"));
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetNickname(string value)
    {
        //if (!IsServer) return;
        nickname.Value = value; // sync owner to all observers

        Debug.Log("LobbyClient: Nickname of panel " + lobby_id.Value + " was set to " + nickname.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetReady(bool value)
    {
        //if (!IsServer) return;
        ready.Value = value; // sync owner to all observers

        Debug.Log("LobbyClient: " + nickname.Value + (ready.Value ? "ready" : "not ready"));
    }


    private void OnDestroy()
    {
        // unsubscribe
        nickname.OnChange -= OnNicknameChanged;
    }

}
