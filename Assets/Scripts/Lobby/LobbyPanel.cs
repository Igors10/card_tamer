using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// One player's card in the lobby. Shows nickname and ready state, or "waiting for player" when the seat is empty
/// </summary>
public class LobbyPanel : MonoBehaviour
{
    [Header("refs")]
    public TMP_Text roleText;
    public TMP_Text nicknameText;
    public TMP_Text statusText;
    public GameObject youTag; // little "you" note on your own card
    public GameObject waitingSpinner;
    [SerializeField] CanvasGroup contentGroup; // faded when the seat is empty

    [Header("colors")]
    [SerializeField] Color readyColor = new Color(0.2f, 0.6f, 0.25f);
    [SerializeField] Color notReadyColor = new Color(0.45f, 0.45f, 0.45f);

    [Header("juice")]
    [SerializeField] float popScale = 1.12f;
    [SerializeField] float popTime = 0.3f;
    [SerializeField] float spinnerSpeed = -180f;

    // remembering last shown state, so the card only pops when something actually changes
    bool shownOccupied = false;
    bool shownReady = false;
    Vector3 restScale;
    Coroutine popRoutine;

    private void Awake()
    {
        restScale = transform.localScale;
    }

    private void Update()
    {
        if (waitingSpinner.activeSelf) waitingSpinner.transform.Rotate(0f, 0f, spinnerSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Shows given player on this card, null client = empty seat
    /// </summary>
    public void Show(LobbyClient client, bool isLocal)
    {
        bool occupied = client != null;
        bool isReady = occupied && client.ready.Value;

        contentGroup.alpha = occupied ? 1f : 0.45f;
        waitingSpinner.SetActive(!occupied);
        youTag.SetActive(occupied && isLocal);

        if (occupied)
        {
            nicknameText.text = client.nickname.Value;
            statusText.text = isReady ? "Ready!" : "Not ready";
            statusText.color = isReady ? readyColor : notReadyColor;
        }
        else
        {
            nicknameText.text = "Waiting for player...";
            statusText.text = "";
        }

        // popping when a player joins or presses ready
        if ((occupied && !shownOccupied) || (isReady && !shownReady)) Pop();
        shownOccupied = occupied;
        shownReady = isReady;
    }

    void Pop()
    {
        if (!gameObject.activeInHierarchy) return;
        if (popRoutine != null) StopCoroutine(popRoutine);
        popRoutine = StartCoroutine(PopAnim());
    }

    IEnumerator PopAnim()
    {
        float t = 0;
        while (t < popTime)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / popTime);
            float wobble = Mathf.Sin(clampedT * Mathf.PI) * (1 - clampedT * 0.5f);
            transform.localScale = restScale * Mathf.Lerp(1f, popScale, wobble);
            yield return null;
        }
        transform.localScale = restScale;
        popRoutine = null;
    }
}
