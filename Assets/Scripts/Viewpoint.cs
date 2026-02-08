using UnityEngine;
using System.Collections;
using UnityEditor.ShaderGraph.Internal;
using TMPro;

public class Viewpoint : MonoBehaviour
{
    Camera cam;

    [Header("State transition text")]
    [SerializeField] TextMeshProUGUI stateTransitionText;
    [SerializeField] GameObject stateTransitionObj;

    [Header("Viewpoint config")]
    // Viewpoints
    Vector3 prevPosition;
    Vector3 prevRotation;
    float prevSize; // field of view
    
    [SerializeField] float viewChangeSpeed;

    private void Start()
    {
        cam = GetComponent<Camera>();

        prevPosition = transform.position;
        prevRotation = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, transform.localEulerAngles.z);
        prevSize = cam.orthographicSize;
    }

    /// <summary>
    /// Set new camera settings (position, rotation, size) and start the animation of camera viewpoint change
    /// </summary>
    /// <param name="gameStateData"></param>
    public void ChangeViewpoint(GameStateData gameStateData)
    { 
        StartCoroutine(TransitionViewpoint(gameStateData.cameraPosition, gameStateData.cameraRotation, gameStateData.cameraSize));
    }

    /// <summary>
    /// Iterates between previous camera viewpoint settings and new state's camera settings
    /// </summary>
    IEnumerator TransitionViewpoint(Vector3 targetPosition, Vector3 targetRotation, float targetSize)
    {
        Debug.Log("Viewpoint: beginning viewpoint transition");

        // enable the big text with next state name
        stateTransitionObj.SetActive(true);
        stateTransitionText.color = new Color(stateTransitionText.color.r, stateTransitionText.color.g, stateTransitionText.color.b, 1f);
        stateTransitionText.text = GameManager.instance.GetState().stateName;

        float t = 0;

        while (t <= 1) // transitioning between old and new viewpoints
        {
            t += Time.deltaTime * viewChangeSpeed;
            float coolT = t * t;

            // moving the camera
            transform.position = Vector3.Lerp(prevPosition, targetPosition, coolT);
            Vector3 currentRotation = Vector3.Lerp(prevRotation, targetRotation, coolT);
            transform.localRotation = Quaternion.Euler(currentRotation.x, currentRotation.y, currentRotation.z);
            cam.orthographicSize = (prevSize + targetSize) * coolT;

            // making state name gradually disapper
            float fadeT = Mathf.Clamp01((t - 0.7f) / 0.3f);
            float alpha = Mathf.Lerp(1f, 0f, fadeT);
            stateTransitionText.color = new Color(stateTransitionText.color.r, stateTransitionText.color.g, stateTransitionText.color.b, alpha);

            yield return null;
        }

        // snapping to correct settings for precision
        transform.position = targetPosition;
        transform.localRotation = Quaternion.Euler(targetRotation.x, targetRotation.y, targetRotation.z);
        cam.orthographicSize = targetSize;

        // saving viewpoint settings
        prevPosition = targetPosition;
        prevRotation = targetRotation;
        prevSize = targetSize;

        // resetting and disabling the big text with next state name
        stateTransitionObj.SetActive(false);
  
        // turning the UI back on
        GameManager.instance.managerUI.EnableUI(true);

        Debug.Log("Viewpoint: viewpoint transition complete");
    }

    public IEnumerator MoveCamera(Vector3 targetPosition, float moveSpeed)
    {
        float t = 0;
        Vector3 startingPosition = transform.position;

        while (t <= 1) 
        {
            t += Time.deltaTime * moveSpeed;
            float coolT = t * t;

            // moving the camera
            transform.position = Vector3.Lerp(startingPosition, targetPosition, coolT);
            yield return null;
        }

        // snapping to correct settings for precision
        transform.position = targetPosition;
    }
}
