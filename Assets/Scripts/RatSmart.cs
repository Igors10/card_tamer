using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class RatSmart : MonoBehaviour
{
    public static RatSmart instance;

    [Header("refs")]
    [SerializeField] GameObject RSobj;
    [SerializeField] Image ratAvatar;
    [SerializeField] TextMeshProUGUI ratText;

    [Header("dialogue settings")]
    [SerializeField] float fadeTime = 0.5f;
    [SerializeField] bool soundOn = true;

    [Header("dialogue list")]
    [SerializeField] List<RSDialogueObj> winDialogues = new List<RSDialogueObj>();
    [SerializeField] List<RSDialogueObj> lostDialogues = new List<RSDialogueObj>();
    [SerializeField] RSDialogueObj[] allDialogues;
    [SerializeField] RSDialogueObj defaultDialogue;

    [Header("completed dialogues")]
    [HideInInspector] public bool discardingExplained;
    [HideInInspector] public bool abilitiesExplained;
    [HideInInspector] public bool drawingExplained;

    bool clicked;

    private void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// Can be called from anywhere to launch a specific dialogue
    /// </summary>
    /// <param name="dialogueName"></param>
    /// <returns></returns>
    public IEnumerator PlayDialogue(string dialogueName = "win")
    {
        RSDialogueObj dialogueToPlay = defaultDialogue;

        switch (dialogueName)
        {
            case "win":
                dialogueToPlay = winDialogues[UnityEngine.Random.Range(0, winDialogues.Count)];
                break;
            case "lose":
                dialogueToPlay = lostDialogues[UnityEngine.Random.Range(0, lostDialogues.Count)];
                break;
            default:
                dialogueToPlay = Array.Find(allDialogues, x => x.name == dialogueName); // finding the dialogue with the matching name
                if (dialogueToPlay == null) { dialogueToPlay = defaultDialogue; Debug.Log("RatSmart: dialogue was not found"); }
                break;
        }

        yield return PlayDialogueObj(dialogueToPlay);
    }

    public IEnumerator PlayDialogueObj(RSDialogueObj dialogue)
    {
        // Enabling the rat
        RSobj.SetActive(true);
        ratText.GetComponent<AutoFade>().fadeInTime = fadeTime;

        for (int i = 0; i < dialogue.line.Count; i++)
        {
            ratText.gameObject.SetActive(false);
            ratText.gameObject.SetActive(true); // triggering fading in effect

            // changing the text
            ratText.text = dialogue.line[i];

            // playing random SFX
            if (soundOn)
            {
                int ratSFXnr = UnityEngine.Random.Range(1, 5);
                string ratSFXname = "Rat" + ratSFXnr.ToString() + "SFX";
                AudioManager.instance.PlaySFX(ratSFXname);
            }

            // pause here until player presses LMB
            do
            {
                yield return null;
            } while (clicked == false);

            ratText.gameObject.GetComponent<AutoFade>().FadeOut(fadeTime);
            yield return new WaitForSeconds(fadeTime);
        }

        StopDialogue();
    }

    public void StopDialogue()
    {
        RSobj.SetActive(false);
    }

    private void Update()
    {
        // registering clicks
        if (Input.GetButtonDown("Fire1"))
        {
            clicked = true;
        }
        if (Input.GetButtonUp("Fire1")) clicked = false;

        // test dialogue
        if (Input.GetKeyDown(KeyCode.D)) StartCoroutine(PlayDialogueObj(defaultDialogue));
    }
}
