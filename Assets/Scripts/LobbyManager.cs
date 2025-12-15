using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Net.Sockets;

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

    /// <summary>
    /// Refreshes information in the scene based on the client synchronized info
    /// </summary>
    /// <param name="panel_id"></param>
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

    /// <summary>
    /// Refreshes the "ready" button based on players that are ready
    /// </summary>
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

    /// <summary>
    /// Refreshes character select toggles based on client info
    /// </summary>
    /// <param name="id"></param>
    void RefreshToggles(int id)
    {
        //  *** TOGGLE STATES ***
        refreshing_toggles = true;

        List<Toggle> toggles = lobby_panel[id].starting_character_toggles;
        List<Sprite> character_images = lobby_panel[id].toggle_sprites;
        Image toggle_image = lobby_panel[id].toggle_image;

        for (int a = 0; a < toggles.Count; a++)
        {
            // Making the player unable to interact with opponents toggles
            toggles[a].interactable = (id == owner_id);

            // Making the player unable to deselect the character (you can only switch between two of them)
            if (lobby_client[owner_id].character_selected.Value == a) toggles[a].interactable = false;

            // Deactivating the toggles once ready
            toggles[a].gameObject.SetActive(!lobby_client[id].ready.Value);

            // Syncing the states of toggles between clients
            toggles[a].isOn = (lobby_client[id].character_selected.Value == a);


            //  *** TOGGLE VISUALS ***

            // Enabling the correct character image
            toggle_image.sprite = character_images[lobby_client[id].character_selected.Value];

            // Making the image the correct transparency (half transparent when character is not ready;
            toggle_image.color = (lobby_client[id].ready.Value) ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 0.3f);
        }
       
        refreshing_toggles = false;
    }

    void CharacterAlreadySelected()
    {
        // Make it do something when you are trying to press "Ready" with a character that your opponent has selected;
    }

    // This happens when clicking on character select toggles
    public void ToggleStartingCharacter(int character_id)
    {
        Debug.Log("LobbyManager: toggle " + character_id + " was clicked. Refreshing toggles: " + refreshing_toggles);

        // making sure if only triggers when you set a toggle to "on"
        if (lobby_panel[owner_id].starting_character_toggles[character_id].isOn == false)
        {
            Debug.Log("LobbyManager: the " + character_id + " toggle is deselected");
            return;
        }
        else
        {
            if (refreshing_toggles) return;
            lobby_client[owner_id].SetCharacter(lobby_client[owner_id], character_id);
        }

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

        lobby_client[0].StartMatch();
        lobby_client[1].StartMatch();
    }

    /// <summary>
    /// Registering and putting a new client in the list whenever a new client joins
    /// </summary>
    /// <param name="client"></param>
    /// <param name="is_owner"></param>
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
