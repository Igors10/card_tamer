using UnityEngine;
using FishNet.Managing;
using FishNet.Transporting.Tugboat;
using UnityEngine.UI;
using TMPro;

public class ConnectionScript : MonoBehaviour
{
    public static ConnectionScript connection_manager;
    public int nextID = 0;

    NetworkManager network_manager;
    Tugboat tugboat;
    [SerializeField] TMP_InputField input_field;

    private void Awake()
    {
        connection_manager = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        network_manager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
        tugboat = network_manager.GetComponentInParent<Tugboat>();

        if (network_manager == null) Debug.Log("ConnectionScript: network manager is null");
        if (tugboat == null) Debug.Log("ConnectionScript: tugboat is null");
    }

    public void CreateServer()
    {
        //Creating a server
        network_manager.ServerManager.StartConnection();
    }

    void GetIP()
    {
        if (input_field.text == "")
        {
            tugboat.SetClientAddress("localhost");
        }
        else
        {
            tugboat.SetClientAddress(input_field.text);
        }
    }


    public void JoinServer()
    {
        GetIP();

        //Joining as client
        network_manager.ClientManager.StartConnection();
    }
}
