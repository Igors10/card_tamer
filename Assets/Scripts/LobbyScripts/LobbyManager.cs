using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//[DefaultExecutionOrder(-1000)]
public class LobbyManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public static LobbyManager instance;
    public List<LobbyPanel> lobbyPanel = new List<LobbyPanel>(); 
    public List<LobbyClient> lobbyClient = new List<LobbyClient>();
    public int ownerID;
    bool refreshingToggles = false;

    [Header("ReadyButtton")]
    [SerializeField] Button readyButton;
    [SerializeField] TMP_Text waitText;
    [SerializeField] Button startButton;
    
    void Awake()
    {
        if (instance == null) instance = this;
        Debug.Log("LobbyManager: instance has been created");

        // adding functionality to buttons
        readyButton.onClick.AddListener(ButtonReady);
        startButton.onClick.AddListener(StartTheGame);
    }

    /// <summary>
    /// Refreshes information in the scene based on the client synchronized info
    /// </summary>
    /// <param name="panelID"></param>
    public void Refresh(int panelID)
    {
        Debug.Log("LobbyManager: panel " + panelID + " refreshed");

        LobbyPanel panel = lobbyPanel[panelID]; // panel to refresh
        LobbyClient client = lobbyClient[panelID]; 
        if (client != null)
        {
            panel.gameObject.SetActive(true); // enabling it if client connects
            panel.nicknameText.text = client.nickname.Value; // refreshing the nickname
            panel.ready.text = (client.ready.Value) ? "ready" : "waiting"; // refresing ready  state
            panel.ready.color = (client.ready.Value) ? Color.green : Color.gray; 
        }

        //Refresh the ready button
        RefreshReadyButton();
        RefreshToggles(panelID);
    }

    /// <summary>
    /// Refreshes the "ready" button based on players that are ready
    /// </summary>
    void RefreshReadyButton()
    {
        if (lobbyClient[0] == null || lobbyClient[1] == null)
        {
            readyButton.gameObject.SetActive(false);
            startButton.gameObject.SetActive(false);
            waitText.gameObject.SetActive(true);
            waitText.text = "Wait for another player to join";
        }
        else if (lobbyClient[0] != null && lobbyClient[1] != null && lobbyClient[ownerID].ready.Value == false)
        {
            readyButton.gameObject.SetActive(true);
            waitText.gameObject.SetActive(false);
        }
        else if (lobbyClient[ownerID].ready.Value && lobbyClient[ownerID].isHost == false)
        {
            readyButton.gameObject.SetActive(false);
            waitText.gameObject.SetActive(true);
            waitText.text = "Wait for the host to start the game";
        }
        else if (lobbyClient[GetOpponentID()].ready.Value == false && lobbyClient[ownerID].isHost)
        {
            readyButton.gameObject.SetActive(false);
            waitText.gameObject.SetActive(true);
            waitText.text = "Wait for the other player to get ready";
        }
        else if (lobbyClient[ownerID].ready.Value)
        {
            readyButton.gameObject.SetActive(false);
            waitText.gameObject.SetActive(false);
            startButton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Refreshes character select toggles based on client info
    /// </summary>
    /// <param name="id"></param>
    void RefreshToggles(int id)
    {
        //  *** TOGGLE STATES ***
        refreshingToggles = true;

        List<Toggle> toggles = lobbyPanel[id].startingCharacterToggles;
        List<Sprite> character_images = lobbyPanel[id].toggleSprites;
        Image toggle_image = lobbyPanel[id].toggleImage;

        for (int a = 0; a < toggles.Count; a++)
        {
            // Making the player unable to interact with opponents toggles
            toggles[a].interactable = (id == ownerID);

            // Making the player unable to deselect the character (you can only switch between two of them)
            if (lobbyClient[ownerID].characterSelected.Value == a) toggles[a].interactable = false;

            // Deactivating the toggles once ready
            toggles[a].gameObject.SetActive(!lobbyClient[id].ready.Value);

            // Syncing the states of toggles between clients
            toggles[a].isOn = (lobbyClient[id].characterSelected.Value == a);


            //  *** TOGGLE VISUALS ***

            // Enabling the correct character image
            toggle_image.sprite = character_images[lobbyClient[id].characterSelected.Value];

            // Making the image the correct transparency (half transparent when character is not ready;
            toggle_image.color = (lobbyClient[id].ready.Value) ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 0.3f);
        }
       
        refreshingToggles = false;
    }

    void CharacterAlreadySelected()
    {
        // Make it do something when you are trying to press "Ready" with a character that your opponent has selected;
    }

    // This happens when clicking on character select toggles
    public void ToggleStartingCharacter(int character_id)
    {
        Debug.Log("LobbyManager: toggle " + character_id + " was clicked. Refreshing toggles: " + refreshingToggles);

        // making sure if only triggers when you set a toggle to "on"
        if (lobbyPanel[ownerID].startingCharacterToggles[character_id].isOn == false)
        {
            Debug.Log("LobbyManager: the " + character_id + " toggle is deselected");
            return;
        }
        else
        {
            if (refreshingToggles) return;
            lobbyClient[ownerID].SetCharacter(lobbyClient[ownerID], character_id);
        }

    }

    public void ButtonReady() // This happens when "Ready" button is being pressed
    {
        if (lobbyClient[GetOpponentID()].ready.Value && lobbyClient[GetOpponentID()].characterSelected.Value == lobbyClient[ownerID].characterSelected.Value) // checking if opponent already has your character and ready
        {
            CharacterAlreadySelected();
        }
        else
        {
            lobbyClient[ownerID].SetReady(true);
        }
    }

    int GetOpponentID()
    {
        return ownerID == 0 ? 1 : 0;
    }

    public void StartTheGame()
    {
        Debug.Log("LobbyManager: the game is starting");

        lobbyClient[0].StartMatch();
        lobbyClient[1].StartMatch();
    }

    /// <summary>
    /// Registering and putting a new client in the list whenever a new client joins
    /// </summary>
    /// <param name="client"></param>
    /// <param name="is_owner"></param>
    public void RegisterClient(LobbyClient client, bool is_owner)
    {
        Debug.Log("LobbyManager: client " + client.lobbyID.Value + " is being registered");

        // making sure list can be indexed by id
        //while (lobby_client.Count <= client.lobby_id) lobby_client.Add(null);
        lobbyClient[client.lobbyID.Value] = client;

        client.clientPanel = lobbyPanel[client.lobbyID.Value];

        // deciding on owner and opponent_id
        if (is_owner) ownerID = client.lobbyID.Value;
    }
}
