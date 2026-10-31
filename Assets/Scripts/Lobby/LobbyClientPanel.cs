using UnityEngine;
using FishNet.Object.Synchronizing;
using FishNet.Object;
using TMPro;

public class LobbyClientPanel : NetworkBehaviour
{
    /*
    public readonly SyncVar<LobbyClient> owner = new SyncVar<LobbyClient>();
    public readonly SyncVar<string> nickname = new SyncVar<string>();
    public TMP_Text nickname_text;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        
        // subscribe a method group
        owner.OnChange += OnOwnerChanged;
        nickname.OnChange += OnNicknameChanged;
    }

    

    private void OnOwnerChanged(LobbyClient previous, LobbyClient next, bool asServer)
    {
        LobbyManager.instance.Refresh(owner.Value.lobby_id);
    }
    private void OnNicknameChanged(string previous, string next, bool asServer)
    {
        LobbyManager.instance.Refresh(owner.Value.lobby_id);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetOwner(LobbyClient value)
    {
        //if (!IsServer) return;
        owner.Value = value; // sync owner to all observers
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetNickname(string value)
    {
        //if (!IsServer) return;
        nickname.Value = value; // sync owner to all observers

        Debug.Log("LobbyClientPanel: Nickname of panel " + owner.Value.lobby_id + " was set to " + nickname);
    }

    private void OnDestroy()
    {
        // unsubscribe
        owner.OnChange -= OnOwnerChanged;
    }*/
}
