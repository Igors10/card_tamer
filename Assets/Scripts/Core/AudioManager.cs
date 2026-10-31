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

    [Header("pitch variation")]
    [SerializeField] float pitchVariation = 0.07f; // random +- pitch offset for sounds played without a set pitch
    [SerializeField] int sfxSourceCount = 8;
    AudioSource[] sfxSources; // each sound gets its own source so changing pitch doesn't affect sounds already playing
    int nextSfxSource = 0;

    [Header("music")]
    [SerializeField] float musicDelay = 0;
    [SerializeField] bool playMusic = true;
    float savedMusicVolume = 0;
    bool isMusicMuted = false;


    private void Awake()
    {
        instance = this;
        CreateSfxSources();
        StartCoroutine(PlayMusic());
    }

    void CreateSfxSources()
    {
        sfxSources = new AudioSource[sfxSourceCount];
        for (int i = 0; i < sfxSourceCount; i++)
        {
            sfxSources[i] = audioSource.gameObject.AddComponent<AudioSource>();
            sfxSources[i].playOnAwake = false;
        }
    }

    AudioSource GetFreeSfxSource()
    {
        // preferring a source that is not playing anything
        for (int i = 0; i < sfxSources.Length; i++)
        {
            AudioSource source = sfxSources[(nextSfxSource + i) % sfxSources.Length];
            if (!source.isPlaying)
            {
                nextSfxSource = (nextSfxSource + i + 1) % sfxSources.Length;
                return source;
            }
        }

        // all sources are busy, reusing the next one in line
        AudioSource oldest = sfxSources[nextSfxSource];
        nextSfxSource = (nextSfxSource + 1) % sfxSources.Length;
        return oldest;
    }

    IEnumerator PlayMusic()
    {
        if (!playMusic) yield break;

        yield return new WaitForSeconds(musicDelay);
        musicSource.Play();
    }

    /// <summary>
    /// Plays a sound effect. Volume is relative to the main sound source volume. 
    /// When pitch is not set the sound gets a slightly random pitch so repeated sounds don't feel samey
    /// </summary>
    public void PlaySFX(string name, float volume = 0, float pitch = 0)
    {
        AudioClip clip = Array.Find(audioClips, x => x.name == name);

        if (clip == null) Debug.Log("Sound Not Found");
        else
        {
            AudioSource source = GetFreeSfxSource();

            // copying settings from the main sound source so muting and volume still work the same way
            source.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
            source.volume = audioSource.volume;
            source.mute = audioSource.mute;
            source.spatialBlend = audioSource.spatialBlend;
            source.priority = audioSource.priority;

            source.pitch = (pitch != 0) ? pitch : audioSource.pitch * (1f + UnityEngine.Random.Range(-pitchVariation, pitchVariation));
            source.PlayOneShot(clip, (volume != 0) ? volume : 1f);
        }
    }

    public void PlaySoundtrack(string name)
    {
        AudioClip soundtrack = Array.Find(musicClips, x => x.name == name);

        if (soundtrack == null) Debug.Log("Sound Not Found");
        else
        {
            musicSource.Stop();
            musicSource.clip = soundtrack;
            musicSource.Play();
        }
    }

    /// <summary>
    /// Mutes every sound effect in the scene
    /// </summary>
    /// <param name="mute"></param>
    public bool MuteSound(bool mute)
    {
        audioSource.mute = mute;
        foreach (AudioSource source in sfxSources) source.mute = mute;

        // returns true if sound was muted
        return audioSource.mute;
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
