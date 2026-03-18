using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
public class SettingsMenu : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] GameObject settingsWindow;
    [SerializeField] GameObject exitConfirmWindow;
    [SerializeField] GameObject mutedSoundIcon;
    [SerializeField] GameObject mutedMusicIcon;

    public void MuteSoundButton()
    {
        mutedSoundIcon.SetActive(AudioManager.instance.MuteSound(!mutedSoundIcon.activeSelf));
    }

    public void MuteMusicButton()
    {
        mutedMusicIcon.SetActive(AudioManager.instance.MuteMusic(!mutedMusicIcon.activeSelf));
    }

    /// <summary>
    /// Exits tomain menu
    /// </summary>
    public void ExitGameButton()
    {
        // first the use will be asked to confirm that they want to exit
        if (!exitConfirmWindow.activeSelf) 
        {
            // playing soundeffect
            AudioManager.instance.PlaySFX("PaperSFX");

            // asks the player for confirmation
            exitConfirmWindow.SetActive(true);
        }
        // actually exiting if user confirms they want to exit
        else 
        {
            StartCoroutine(ExitGame());
        }
    }

    IEnumerator ExitGame()
    {
        // masking the transition with loading fog
        yield return StartCoroutine(GameManager.instance.loadingFog.ApplyLoadingFog(true));

        // changing the scene to main menu
        SceneManager.LoadScene("Main Menu");
    }

    /// <summary>
    /// Closes chosen window
    /// </summary>
    /// <param name="windowToClose"></param>
    public void CloseWindow(GameObject windowToClose)
    {
        windowToClose.SetActive(false);
    }

    /// <summary>
    /// Opens the settings window
    /// </summary>
    public void OpenSettings()
    {
        // playing soundeffect
        AudioManager.instance.PlaySFX("PaperSFX");

        settingsWindow.SetActive(true);
        exitConfirmWindow.SetActive(false);
    }
}
