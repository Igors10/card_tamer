using UnityEngine;
using System.Collections;
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

    [Header("zoom")]
    bool zoomedIn;
    
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

    public void ZoomIn(GameObject objToZoomIn, float zoomIntensity, float zoomTime)
    {
        StartCoroutine(ZoomInOn(objToZoomIn, zoomIntensity, zoomTime));
    }

    public IEnumerator ZoomInOn(GameObject zoomObj, float zoomIntensity, float zoomTime)
    {
        Debug.Log($"Viewport [{gameObject.GetInstanceID()}]: Started zoom in");

        // starting values
        Vector3 startingPosition = transform.position;
        Vector3 startingRotation = transform.localEulerAngles;

        // target values
        Vector3 targetPosition = Vector3.Lerp(startingPosition, zoomObj.transform.position, zoomIntensity);
        targetPosition = new Vector3(zoomObj.transform.position.x, targetPosition.y, targetPosition.z);
        Vector3 targetRotation = Vector3.Lerp(startingRotation, new Vector3(startingRotation.x * 0.3f, startingRotation.y, startingRotation.z), zoomIntensity);

        float t = 0;
        zoomedIn = true;

        // zooming in
        while (t < zoomTime * 2/3)
        {
            t += Time.deltaTime;
            float clampedT = t / (zoomTime * 2/3);
            float coolT = Mathf.SmoothStep(0f, 1f, clampedT);

            transform.position = Vector3.Lerp(startingPosition, targetPosition, coolT);
            Vector3 rotation = Vector3.Lerp(startingRotation, targetRotation, coolT);
            transform.localRotation = Quaternion.Euler(rotation.x, rotation.y, rotation.z);

            yield return null;
        }

        // waiting for command to stop zooming in
        while (zoomedIn)
        {
            yield return null;
        }

        Debug.Log("Viewport: in zoomout phase");

        t = 0;

        // zooming out
        while (t < zoomTime * 1/3)
        {
            t += Time.deltaTime;
            float clampedT = t / (zoomTime * 1/3);
            float coolT = Mathf.SmoothStep(0f, 1f, clampedT);

            transform.position = Vector3.Lerp(targetPosition, startingPosition, coolT);
            Vector3 rotation = Vector3.Lerp(targetRotation, startingRotation, coolT);
            transform.localRotation = Quaternion.Euler(rotation.x, rotation.y, rotation.z);

            yield return null;
        }

        // snapping to correct values
        transform.position = startingPosition;
        transform.localRotation = Quaternion.Euler(startingRotation);

        Debug.Log("Viewport: zoom in finished");
    }

    public void ZoomOut()
    {
        Debug.Log($"Viewport [{gameObject.GetInstanceID()}]: Called for a zoom out");
        zoomedIn = false;
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
