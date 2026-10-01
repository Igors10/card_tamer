using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using TMPro;
public class RatSmart : MonoBehaviour
{
    public static RatSmart instance;

    [Header("refs")]
    [SerializeField] GameObject RSobj;
    [SerializeField] Image ratAvatar;
    [SerializeField] TextMeshProUGUI ratText;

    [Header("dialogue settings")]
    [SerializeField] float fadeTime = 0.5f;

    [Header("dialogue list")]
    [SerializeField] List<RSDialogueObj> winDialogues = new List<RSDialogueObj>();
    [SerializeField] List<RSDialogueObj> lostDialogues = new List<RSDialogueObj>();
    [SerializeField] RSDialogueObj defaultDialogue;

    bool clicked;

    private void Awake()
    {
        instance = this;
    }

    public IEnumerator PlayThemeDialogue(string dialogueType = "win")
    {
        RSDialogueObj dialogueToPlay = defaultDialogue;

        switch (dialogueType)
        {
            case "win":
                dialogueToPlay = winDialogues[Random.Range(0, winDialogues.Count)];
                break;
            case "lose":
                dialogueToPlay = lostDialogues[Random.Range(0, lostDialogues.Count)];
                break;
        }

        yield return PlayDialogue(dialogueToPlay);
    }

    public IEnumerator PlayDialogue(RSDialogueObj dialogue)
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
        if (Input.GetKeyDown(KeyCode.D)) StartCoroutine(PlayDialogue(defaultDialogue));
    }
}
