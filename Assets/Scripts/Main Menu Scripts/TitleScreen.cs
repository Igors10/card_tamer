using UnityEngine;

public class TitleScreen : MonoBehaviour
{
    public static TitleScreen instance;
    public CardList cardlist;
    public AudioSource titleAudioSource;

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

}
