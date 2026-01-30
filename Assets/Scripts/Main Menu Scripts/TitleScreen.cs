using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreen : MonoBehaviour
{
    public static TitleScreen instance;

    [Header("refs")]
    public CardList cardlist;
    public AudioSource titleAudioSource;
    public GameObject[] menus = new GameObject[0];


    private void Awake()
    {
        instance = this;
        titleAudioSource = GetComponent<AudioSource>();
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
        for (int i = 0; i < menus.Length; i++) menus[i].SetActive(i == menuID);
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
    public void StartOfflineMatch()
    {
        SceneManager.LoadScene("Board");
    }
}
