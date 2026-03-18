using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class TitleScreen : MonoBehaviour
{
    public static TitleScreen instance;

    [Header("refs")]
    public CardList cardlist;
    public AudioSource titleAudioSource;
    public GameObject[] menus = new GameObject[0];
    [SerializeField] MenuChoice startingCardChoice;
    [SerializeField] MenuChoice startingSpecialChoice;
    [SerializeField] PlayerConfigObj playerConfigObj;

    [Header("menu transition")]
    [SerializeField] LoadingFog loadingFog;
    Coroutine currentTransition = null;
    

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
    /// Plays a soundeffect from title screen's audioSource
    /// </summary>
    /// <param name="audio"></param>
    /// <param name="volume"></param>
    /// <param name="pitch"></param>
    public void PlaySound(AudioClip audio, float volume = 1, float pitch = 1)
    {
        titleAudioSource.clip = audio;
        titleAudioSource.volume = volume;
        titleAudioSource.pitch = pitch;

        titleAudioSource.Play();
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
            AudioManager.instance.PlaySFX("ButtonSound");
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
        AudioManager.instance.PlaySFX("ButtonSound");

        yield return StartCoroutine(loadingFog.ApplyLoadingFog(true));

        // resetting player card config
        playerConfigObj.ResetCardConfig();

        // Saving chosen cards
        for (int i = 0; i < 4; i++)
        {
            CreatureObj chosenCard = startingCardChoice.GetCurrentChoice().GetComponent<MenuDoodle>().doodleData;
            playerConfigObj.startingCards.Add(chosenCard);
        }

        // Saving chosen special card
        CreatureObj chosenSpecial = startingSpecialChoice.GetCurrentChoice().GetComponent<MenuDoodle>().doodleData;
        playerConfigObj.startingCards.Add(chosenSpecial);

        // Flagging match as offline
        playerConfigObj.offlineMatch = true;

        // Starting the match
        SceneManager.LoadScene("Board");
    }

}
