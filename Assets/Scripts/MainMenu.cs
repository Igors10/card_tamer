using UnityEngine;
using System.Collections;
using FishNet;
using FishNet.Managing;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [SerializeField] ConnectionScript connection_script;

    [SerializeField] GameObject main_menu;
    [SerializeField] GameObject lobby_menu;
    [SerializeField] TMP_InputField nickname_field;

    // Join Button
    // Create Button
    // Nickname field
    // Ready button
    // Deck toggle
    // Start the game button

    public void JoinButton()
    {
        connection_script.JoinServer();
        SwitchToLobby();
    }

    public void CreateButton()
    {
        connection_script.CreateServer();
        connection_script.JoinServer();
        SwitchToLobby();
    }

    void SwitchToLobby()
    {
        main_menu.SetActive(false);
        lobby_menu.SetActive(true);

        GameData.nickname = nickname_field.text; // saving the nickname
    }


}
