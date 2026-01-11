using UnityEngine;
using FishNet.Managing;
using FishNet.Transporting.Tugboat;
using UnityEngine.UI;
using TMPro;

public class ConnectionScript : MonoBehaviour
{
    public static ConnectionScript connectionManager;
    public int nextID = 0;

    NetworkManager networkManager;
    Tugboat tugboat;
    [SerializeField] TMP_InputField inputField;

    private void Awake()
    {
        connectionManager = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
        tugboat = networkManager.GetComponentInParent<Tugboat>();

        if (networkManager == null) Debug.Log("ConnectionScript: network manager is null");
        if (tugboat == null) Debug.Log("ConnectionScript: tugboat is null");
    }

    public void CreateServer()
    {
        //Creating a server
        networkManager.ServerManager.StartConnection();
    }

    void GetIP()
    {
        if (inputField.text == "")
        {
            tugboat.SetClientAddress("localhost");
        }
        else
        {
            tugboat.SetClientAddress(inputField.text);
        }
    }


    public void JoinServer()
    {
        GetIP();

        //Joining as client
        networkManager.ClientManager.StartConnection();
    }
}
