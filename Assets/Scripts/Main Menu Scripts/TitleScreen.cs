using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

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
