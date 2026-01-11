using UnityEngine;
using System.Collections;
using FishNet;
using FishNet.Managing;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [SerializeField] ConnectionScript connectionScript;

    [SerializeField] GameObject mainMenu;
    [SerializeField] GameObject lobbyMenu;
    [SerializeField] TMP_InputField nicknameField;

    // Join Button
    // Create Button
    // Nickname field
    // Ready button
    // Deck toggle
    // Start the game button

    public void JoinButton()
    {
        connectionScript.JoinServer();
        SwitchToLobby();
    }

    public void CreateButton()
    {
        connectionScript.CreateServer();
        connectionScript.JoinServer();
        SwitchToLobby();
    }

    void SwitchToLobby()
    {
        mainMenu.SetActive(false);
        lobbyMenu.SetActive(true);

        GameData.nickname = nicknameField.text; // saving the nickname
    }


}
