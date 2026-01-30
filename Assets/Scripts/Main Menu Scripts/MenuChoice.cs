using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using TMPro;
using FishNet.Configuring;

public class MenuChoice : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] TextMeshProUGUI description;

    [Header("attributes")]
    [SerializeField] List<GameObject> options = new List<GameObject>();
    [SerializeField] List<string> descTexts = new List<string>();
    [SerializeField] List<AudioClip> audioClips = new List<AudioClip>();
    [SerializeField] int currentOption;

    private void Start()
    {
        options[currentOption].SetActive(true);
        description.text = descTexts[currentOption];
    }

    /// <summary>
    /// Set current option to previous option
    /// </summary>
    public void PrevChoice()
    {
        options[currentOption].SetActive(false);
        currentOption = (currentOption == 0) ? options.Count - 1 : currentOption - 1;
        NewChoice();
    }

    /// <summary>
    /// Set current option to next option
    /// </summary>
    public void NextChoice()
    {
        options[currentOption].SetActive(false);
        currentOption = (currentOption == options.Count - 1) ? 0 : currentOption + 1;
        NewChoice();
    }

    /// <summary>
    /// Enables new current option
    /// </summary>
    void NewChoice()
    {
        // enables it
        options[currentOption].SetActive(true);

        // sets the description
        description.text = descTexts[currentOption];

        // plays an audio effect if there is any
        if (audioClips.Count > 0) TitleScreen.instance.PlaySound(audioClips[currentOption]);
    }

    public GameObject GetCurrentChoice()
    {
        return options[currentOption];
    }
    
}
