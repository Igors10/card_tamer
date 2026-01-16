using UnityEngine;

public class BillboardEffect : MonoBehaviour
{    void Update()
    {
        // Making sure the sprite always faces the camera
        transform.forward = Camera.main.transform.forward;
    }
}
