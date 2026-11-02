using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class TitleScreen : MonoBehaviour
{
    public static TitleScreen instance;

    [Header("refs")]
    public CardList cardlist;
    public AudioSource titleAudioSource;
    public GameObject[] menus = new GameObject[0];
    [SerializeField] MenuChoice startingCardChoice;
    [SerializeField] MenuChoice startingSpecialChoice;
    public PlayerConfigObj playerConfigObj;
    public CardDatabase cardDatabase;
    public UnitSprite[] doodles;

    [Header("menu transition")]
    [SerializeField] LoadingFog loadingFog;
    Coroutine currentTransition = null;

    [Header("play mode screen")]
    [SerializeField] RectTransform menuPan; // holds the title screen and the play mode screen below it, moving it up looks like the camera moving down
    [SerializeField] float panTime = 0.9f;
    [SerializeField] TMP_InputField nicknameField; // used for both bot and online matches
    Coroutine panRoutine;

    [Header("gallery screen")]
    [SerializeField] GameObject galleryButton; // hidden until the player has saved a drawing
    [SerializeField] GalleryMenu galleryMenu; // sits one screen width to the right of the title screen

    [Header("join lobby code")]
    [SerializeField] GameObject joinLobbyButton; // swapped for the code row when pressed
    [SerializeField] GameObject joinCodeRow;
    [SerializeField] TMP_InputField joinCodeField;
    [SerializeField] Button joinCodeGoButton;
    [SerializeField] Button joinCodeCancelButton;
    [SerializeField] TMP_Text joinCodeMessage; // errors, like a wrong code
    [SerializeField] float joinRowPopTime = 0.25f;

    [Header("doodle respawn")]
    [SerializeField] RectTransform doodleArea; // knocked out doodles respawn inside this area
    [SerializeField] RectTransform[] doodleKeepClear; // and not on top of these (logo, buttons)
    [SerializeField] float doodleAreaMargin = 60f;
    [SerializeField] float doodleSpacing = 30f;


    private void Awake()
    {
        instance = this;
        titleAudioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        // coming back from an online match, closing the connection and removing the NetworkManager
        if (ConnectionScript.connectionManager != null) ConnectionScript.connectionManager.Shutdown();

        // nickname from last time
        GameData.nickname = PlayerPrefs.GetString(GameData.nicknamePrefKey, GameData.defaultNickname);
        nicknameField.characterLimit = GameData.maxNicknameLength;
        nicknameField.text = GameData.nickname;
        nicknameField.onValueChanged.AddListener(OnNicknameChanged);

        // join code row: letters and numbers only, typed letters become capitals, Enter joins
        joinCodeField.characterLimit = 10;
        joinCodeField.onValidateInput = (text, index, c) => char.IsLetterOrDigit(c) ? char.ToUpperInvariant(c) : '\0';
        joinCodeField.onSubmit.AddListener(_ => JoinWithCodeButton());
        joinCodeGoButton.onClick.AddListener(JoinWithCodeButton);
        joinCodeCancelButton.onClick.AddListener(() => CloseJoinCodeRow(true));
        CloseJoinCodeRow(false);

        galleryButton.SetActive(Gallery.HasPlayerDrawings());

        // the lobby sent us back with a problem (wrong code, host left...), showing it next to the code row
        if (GameData.menuMessage != "")
        {
            float screenHeight = ((RectTransform)menuPan.parent).rect.height;
            menuPan.anchoredPosition = new Vector2(0f, screenHeight);
            joinCodeField.text = GameData.joinCode; // keeping the typed code, in case it only had a typo
            OpenJoinCodeRow(GameData.menuMessage);
            GameData.menuMessage = "";
        }

        // loading fog fade away effect at the beginning

        loadingFog.gameObject.SetActive(true);
        StartCoroutine(loadingFog.ApplyLoadingFog(false));
    }
   
    
    /// <summary>
    /// Opens chosen menu, closes previous one
    /// </summary>
    /// <param name="menuID"></param>
    public void OpenMenu(int menuID)
    {
        if (currentTransition == null)
        {
            // beginning to switch menus
            currentTransition = StartCoroutine(TransitionToMenu(menuID));

            // playing soundeffect
            AudioManager.instance.PlaySFX("ButtonSFX");
        }
    }

    IEnumerator TransitionToMenu(int menuID)
    {
        yield return StartCoroutine(loadingFog.ApplyLoadingFog(true));
        for (int i = 0; i < menus.Length; i++) menus[i].SetActive(i == menuID);
        yield return StartCoroutine(loadingFog.ApplyLoadingFog(false));

        currentTransition = null;
    }

    // ===============
    // PLAY MODE SCREEN
    // ===============

    /// <summary>
    /// Moves the view down to the play mode screen (bot, join lobby, create match)
    /// </summary>
    public void OpenPlayModeScreen() => PanMenu(new Vector2(0f, ScreenSize.y));

    /// <summary>
    /// Moves the view back up to the title screen
    /// </summary>
    public void ClosePlayModeScreen()
    {
        if (panRoutine != null || currentTransition != null) return;
        CloseJoinCodeRow(false);
        PanMenu(Vector2.zero);
    }

    // ===============
    // GALLERY SCREEN
    // ===============

    /// <summary>
    /// Moves the view right to the gallery
    /// </summary>
    public void OpenGalleryScreen()
    {
        if (panRoutine != null || currentTransition != null) return;
        galleryMenu.Open();
        PanMenu(new Vector2(-ScreenSize.x, 0f));
    }

    /// <summary>
    /// Moves the view back to the title screen (the button hides if every drawing got deleted)
    /// </summary>
    public void CloseGalleryScreen()
    {
        if (panRoutine != null || currentTransition != null) return;
        galleryButton.SetActive(galleryMenu.HasDrawings);
        PanMenu(Vector2.zero);
    }

    Vector2 ScreenSize => ((RectTransform)menuPan.parent).rect.size;

    /// <summary>
    /// Saving the nickname as it's typed, so it's there for the match and next time the game starts
    /// </summary>
    void OnNicknameChanged(string text)
    {
        GameData.nickname = text.Trim();
        PlayerPrefs.SetString(GameData.nicknamePrefKey, GameData.nickname);
    }

    /// <summary>
    /// Moves the pan so another screen is in view. Play mode screen sits one screen height below the title screen, the gallery one screen width to the right
    /// </summary>
    void PanMenu(Vector2 targetPos)
    {
        if (panRoutine != null || currentTransition != null) return;

        AudioManager.instance.PlaySFX("ButtonSFX");
        panRoutine = StartCoroutine(PanMenuAnim(targetPos));
    }

    IEnumerator PanMenuAnim(Vector2 targetPos)
    {
        Vector2 startPos = menuPan.anchoredPosition;

        float t = 0;
        while (t < panTime)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / panTime);
            float coolT = clampedT * clampedT * (3f - 2f * clampedT); // ease in and out, like a camera starting and stopping
            menuPan.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, coolT);
            yield return null;
        }
        menuPan.anchoredPosition = targetPos;

        panRoutine = null;
    }

    /// <summary>
    /// Swaps the "Join Lobby" button for a small field to type the host's lobby code in
    /// </summary>
    public void JoinOnlineLobbyButton()
    {
        if (currentTransition != null || panRoutine != null) return;
        AudioManager.instance.PlaySFX("ButtonSFX");
        OpenJoinCodeRow("");
    }

    void OpenJoinCodeRow(string message)
    {
        joinLobbyButton.SetActive(false);
        joinCodeRow.SetActive(true);
        joinCodeMessage.text = message;

        joinCodeField.ActivateInputField();
        StartCoroutine(PopAnim(joinCodeRow.transform));
    }

    void CloseJoinCodeRow(bool playSound)
    {
        if (playSound) AudioManager.instance.PlaySFX("ButtonSFX");
        joinCodeRow.SetActive(false);
        joinLobbyButton.SetActive(true);
    }

    void JoinWithCodeButton()
    {
        if (currentTransition != null || panRoutine != null) return;

        string code = ConnectionScript.NormalizeCode(joinCodeField.text);
        if (code == "")
        {
            joinCodeMessage.text = "Type in the lobby code from the host";
            joinCodeField.ActivateInputField();
            return;
        }

        // the lobby scene joins with this code as soon as it loads
        GameData.joinCode = code;
        OpenLobby(false);
    }

    IEnumerator PopAnim(Transform target)
    {
        float t = 0;
        while (t < joinRowPopTime)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / joinRowPopTime);
            float overshoot = 1f + 0.12f * Mathf.Sin(clampedT * Mathf.PI); // grows past full size and settles
            target.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, 1 - (1 - clampedT) * (1 - clampedT)) * overshoot;
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    /// <summary>
    /// Goes to the lobby scene to host a match
    /// </summary>
    public void CreateOnlineMatchButton() => OpenLobby(true);

    void OpenLobby(bool host)
    {
        if (currentTransition != null || panRoutine != null) return;

        GameData.hostingLobby = host;
        currentTransition = StartCoroutine(GoToLobby());
    }

    IEnumerator GoToLobby()
    {
        AudioManager.instance.PlaySFX("ButtonSFX");
        yield return StartCoroutine(loadingFog.ApplyLoadingFog(true));
        SceneManager.LoadScene("Lobby");
    }

    /// <summary>
    /// Finds a random spot for a doodle that doesn't cover the logo, buttons or other doodles. Returns local position in doodle's parent space
    /// </summary>
    public Vector3 GetFreeDoodlePosition(RectTransform doodle, Vector3 doodleScale)
    {
        Transform space = doodle.parent;
        Vector2 size = Vector2.Scale(doodle.rect.size, doodleScale) + Vector2.one * doodleSpacing;

        // area where the doodle fits fully
        Rect area = GetRectIn(doodleArea, space);
        float minX = area.xMin + doodleAreaMargin + size.x / 2, maxX = area.xMax - doodleAreaMargin - size.x / 2;
        float minY = area.yMin + doodleAreaMargin + size.y / 2, maxY = area.yMax - doodleAreaMargin - size.y / 2;

        Vector2 candidate = Vector2.zero;
        for (int attempt = 0; attempt < 50; attempt++)
        {
            candidate = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
            Rect candidateRect = new Rect(candidate - size / 2, size);

            if (!OverlapsAny(candidateRect, doodle, space)) break;
        }

        return new Vector3(candidate.x, candidate.y, doodle.localPosition.z);
    }

    bool OverlapsAny(Rect candidateRect, RectTransform doodle, Transform space)
    {
        foreach (RectTransform keepClear in doodleKeepClear)
        {
            if (keepClear != null && keepClear.gameObject.activeInHierarchy && candidateRect.Overlaps(GetRectIn(keepClear, space))) return true;
        }

        // other doodles
        foreach (Transform other in space)
        {
            if (other == doodle || !other.gameObject.activeInHierarchy) continue;
            if (candidateRect.Overlaps(GetRectIn((RectTransform)other, space))) return true;
        }

        return false;
    }

    /// <summary>
    /// Bounding rect of a RectTransform in another transform's local space
    /// </summary>
    Rect GetRectIn(RectTransform rt, Transform space)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
        foreach (Vector3 corner in corners)
        {
            Vector2 local = space.InverseTransformPoint(corner);
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    /// <summary>
    /// Closes the game
    /// </summary>
    public void ExitGame()
    {
        Application.Quit();

        // stops play mode in the editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    /// <summary>
    /// Starts transition to "Board" scene
    /// </summary>
    public void StartOfflineMatchButton()
    {
        StartCoroutine(StartOfflineMatch());
    }

    IEnumerator StartOfflineMatch()
    {
        // playing soundeffect
        AudioManager.instance.PlaySFX("ButtonSFX");
        AudioManager.instance.PlaySFX("StartGameSFX");

        yield return StartCoroutine(loadingFog.ApplyLoadingFog(true));

        // resetting player card config
        playerConfigObj.ResetCardConfig();

        // Flagging match as offline
        playerConfigObj.offlineMatch = true;

        // Starting the match
        SceneManager.LoadScene("Board");
    }

}
