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

    // Moving camera angles??

    //Battle camera- (Position[0, 8, -7], Rotation[40, 0, 0])
}
