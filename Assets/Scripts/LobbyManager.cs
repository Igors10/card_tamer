using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

//[DefaultExecutionOrder(-1000)]
public class LobbyManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public static LobbyManager instance;
    public List<LobbyPanel> lobby_panel = new List<LobbyPanel>(); 
    public List<LobbyClient> lobby_client = new List<LobbyClient>();
    public int owner_id;
    bool refreshing_toggles = false;

    [Header("ReadyButtton")]
    [SerializeField] Button ready_button;
    [SerializeField] TMP_Text wait_text;
    [SerializeField] Button start_button;
    
    void Awake()
    {
        if (instance == null) instance = this;
        Debug.Log("LobbyManager: instance has been created");

        // adding functionality to buttons
        ready_button.onClick.AddListener(ButtonReady);
        start_button.onClick.AddListener(StartTheGame);
    }

    public void Refresh(int panel_id)
    {
        Debug.Log("LobbyManager: panel " + panel_id + " refreshed");

        LobbyPanel panel = lobby_panel[panel_id]; // panel to refresh
        LobbyClient client = lobby_client[panel_id]; 
        if (client != null)
        {
            panel.gameObject.SetActive(true); // enabling it if client connects
            panel.nickname_text.text = client.nickname.Value; // refreshing the nickname
            panel.ready.text = (client.ready.Value) ? "ready" : "waiting"; // refresing ready  state
            panel.ready.color = (client.ready.Value) ? Color.green : Color.gray; 
        }

        //Refresh the ready button
        RefreshReadyButton();
        RefreshToggles(panel_id);
    }

    void RefreshReadyButton()
    {
        if (lobby_client[0] == null || lobby_client[1] == null)
        {
            ready_button.gameObject.SetActive(false);
            start_button.gameObject.SetActive(false);
            wait_text.gameObject.SetActive(true);
            wait_text.text = "Wait for another player to join";
        }
        else if (lobby_client[0] != null && lobby_client[1] != null && lobby_client[owner_id].ready.Value == false)
        {
            ready_button.gameObject.SetActive(true);
            wait_text.gameObject.SetActive(false);
        }
        else if (lobby_client[owner_id].ready.Value && lobby_client[owner_id].is_host == false)
        {
            ready_button.gameObject.SetActive(false);
            wait_text.gameObject.SetActive(true);
            wait_text.text = "Wait for the host to start the game";
        }
        else if (lobby_client[GetOpponentID()].ready.Value == false && lobby_client[owner_id].is_host)
        {
            ready_button.gameObject.SetActive(false);
            wait_text.gameObject.SetActive(true);
            wait_text.text = "Wait for the other player to get ready";
        }
        else if (lobby_client[owner_id].ready.Value)
        {
            ready_button.gameObject.SetActive(false);
            wait_text.gameObject.SetActive(false);
            start_button.gameObject.SetActive(true);
        }
    }


    void RefreshToggles(int id)
    {
        //  *** TOGGLE STATES ***
        refreshing_toggles = true;

        Toggle ghost_toggle = lobby_panel[id].g_toggle;
        Toggle robber_toggle = lobby_panel[id].r_toggle;

        // Making the player unable to interact with opponents toggles
        if (id == owner_id)
        {
            ghost_toggle.interactable = true;
            robber_toggle.interactable = true;
        }
        else
        {
            ghost_toggle.interactable = false;
            robber_toggle.interactable = false;
        }

        // Making the player unable to deselect the character (you can only switch between two of them)
        if (lobby_client[owner_id].character_selected.Value == 0)
        {
            robber_toggle.interactable = false;
        }
        else if (lobby_client[owner_id].character_selected.Value == 1)
        {
            ghost_toggle.interactable = false;
        }

        // Deactivating the toggles once ready
        ghost_toggle.gameObject.SetActive(!lobby_client[id].ready.Value);
        robber_toggle.gameObject.SetActive(!lobby_client[id].ready.Value);

        // Syncing the states of toggles 
        robber_toggle.isOn = (lobby_client[id].character_selected.Value == 0) ? true : false;
        ghost_toggle.isOn = (lobby_client[id].character_selected.Value == 1) ? true : false;

        //  *** TOGGLE VISUALS ***

        // Enabling the correct character image
        if (lobby_client[id].character_selected.Value == 0)
        {
            lobby_panel[id].toggle_images[0].gameObject.SetActive(true);
            lobby_panel[id].toggle_images[1].gameObject.SetActive(false);
        }
        else if (lobby_client[id].character_selected.Value == 1)
        {
            lobby_panel[id].toggle_images[0].gameObject.SetActive(false);
            lobby_panel[id].toggle_images[1].gameObject.SetActive(true);
        }

        // Making the image the correct transparency
        if (lobby_client[id].ready.Value)
        {
            lobby_panel[id].toggle_images[0].color = new Color(1f, 1f, 1f, 1f);
            lobby_panel[id].toggle_images[1].color = new Color(1f, 1f, 1f, 1f);
        }
        else if (lobby_client[id].ready.Value == false)
        {
            lobby_panel[id].toggle_images[0].color = new Color(1f, 1f, 1f, 0.3f);
            lobby_panel[id].toggle_images[1].color = new Color(1f, 1f, 1f, 0.3f);
        }

        refreshing_toggles = false;
    }

    void CharacterAlreadySelected()
    {
        // Make it do something when you are trying to press "Ready" with a character that your opponent has selected;
    }

    // Those happen when clicking on character select toggles
    public void ToggleRobber() 
    {
        if (refreshing_toggles) return;
        lobby_client[owner_id].SetCharacter(lobby_client[owner_id], 0);
    }

    public void ToggleGhost()
    {
        if (refreshing_toggles) return;
        lobby_client[owner_id].SetCharacter(lobby_client[owner_id], 1);
    }

    public void ButtonReady() // This happens when "Ready" button is being pressed
    {
        if (lobby_client[GetOpponentID()].ready.Value && lobby_client[GetOpponentID()].character_selected.Value == lobby_client[owner_id].character_selected.Value) // checking if opponent already has your character and ready
        {
            CharacterAlreadySelected();
        }
        else
        {
            lobby_client[owner_id].SetReady(true);
        }
    }

    int GetOpponentID()
    {
        return owner_id == 0 ? 1 : 0;
    }

    public void StartTheGame()
    {
        Debug.Log("LobbyManager: the game is starting");

        // Logic for starting the game (probably will have to call a function in the lobby client so that the scene can be changed in Fishnet way)
    }

    public void RegisterClient(LobbyClient client, bool is_owner)
    {
        Debug.Log("LobbyManager: client " + client.lobby_id.Value + " is being registered");

        // making sure list can be indexed by id
        //while (lobby_client.Count <= client.lobby_id) lobby_client.Add(null);
        lobby_client[client.lobby_id.Value] = client;

        client.client_panel = lobby_panel[client.lobby_id.Value];

        // deciding on owner and opponent_id
        if (is_owner) owner_id = client.lobby_id.Value;
    }
}
