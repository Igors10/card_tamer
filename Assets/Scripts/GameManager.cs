using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Managers")]
    public HandManager handManager;
    public CardGenerator cardGenerator;
    public FieldManager fieldManager;
    void Start()
    {
        // Making GameManager accessible from anywhere
        instance = this;
    }
}
