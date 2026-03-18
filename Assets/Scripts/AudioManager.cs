using System;
using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("refs")]
    public static AudioManager instance;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioSource musicSource;

    [Header("sounds")]
    [SerializeField] AudioClip[] audioClips;
    [SerializeField] AudioClip[] musicClips;
    float savedSoundVolume = 0;
    bool isSoundMuted = false;

    [Header("music")]
    [SerializeField] float musicDelay = 0;
    [SerializeField] bool playMusic = true;
    float savedMusicVolume = 0;
    bool isMusicMuted = false;


    private void Awake()
    {
        instance = this;
        StartCoroutine(PlayMusic());
    }

    IEnumerator PlayMusic()
    {
        if (!playMusic) yield break;

        yield return new WaitForSeconds(musicDelay);
        musicSource.Play();
    }

    public void PlaySFX(string name, float volume = 0, float pitch = 0)
    {
        AudioClip clip = Array.Find(audioClips, x => x.name == name);

        if (clip == null) Debug.Log("Sound Not Found");
        else
        {
            if (volume != 0) audioSource.volume = volume;
            if (pitch != 0) audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip);
        }
    }

    public void PlaySoundtrack(string name)
    {
        AudioClip soundtrack = Array.Find(musicClips, x => x.name == name);

        if (soundtrack == null) Debug.Log("Sound Not Found");
        else
        {
            musicSource.PlayOneShot(soundtrack);
        }
    }

    /// <summary>
    /// Mutes every sound effect in the scene
    /// </summary>
    /// <param name="mute"></param>
    public bool MuteSound(bool mute)
    {
        isSoundMuted = mute;

        if (mute)
        {
            // saving the volume value
            savedSoundVolume = audioSource.volume;

            // setting volume to 0
            audioSource.volume = 0;
        }
        else
        {
            // setting the volume back to pre-muted saved value
            audioSource.volume = savedSoundVolume;
        }

        // returns true if sound was muted
        return isSoundMuted;
    }

    /// <summary>
    /// Mutes music in the scene
    /// </summary>
    /// <param name="mute"></param>
    public bool MuteMusic(bool mute)
    {
        isMusicMuted = mute;

        if (mute)
        {
            // saving the volume value
            savedMusicVolume = musicSource.volume;

            // setting volume to 0
            musicSource.volume = 0;
        }
        else
        {
            // setting the volume back to pre-muted saved value
            musicSource.volume = savedMusicVolume;
        }

        // returns true if music was muted
        return isMusicMuted;
    }
}
