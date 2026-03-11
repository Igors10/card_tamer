using System;
using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    [Header("refs")]
    public static AudioManager instance;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioSource musicSource;

    [Header("sounds")]
    [SerializeField] AudioClip[] audioClips;
    [SerializeField] AudioClip[] musicClips;

    [Header("music")]
    [SerializeField] float musicDelay = 0;
    [SerializeField] bool playMusic = true;


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
            audioSource.PlayOneShot(soundtrack);
        }
    }
}
