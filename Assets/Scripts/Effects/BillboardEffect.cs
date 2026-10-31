using UnityEngine;

public class BillboardEffect : MonoBehaviour
{
    [SerializeField] bool onStartOnly;

    private void Start()
    {
        // Making sure the sprite always faces the camera (only on start)
        if (onStartOnly) transform.forward = Camera.main.transform.forward;
    }
    void Update()
    {
        // Making sure the sprite always faces the camera
        if (!onStartOnly)transform.forward = Camera.main.transform.forward;
    }
}
