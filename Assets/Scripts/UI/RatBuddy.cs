using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Rat Smart on the side of the screen during the first game: talks while the game goes on (no dim, no clicking through).
/// Hint = stays on its last line until ClearHint (the player did the action), comment = goes away by itself.
/// Every line set plays once, {card} in a line is replaced with the card's name
/// </summary>
public class RatBuddy : MonoBehaviour
{
    public static RatBuddy instance;

    [Header("refs")]
    [SerializeField] GameObject bubble; // avatar + text box, nothing in it catches clicks
    [SerializeField] Image ratAvatar;
    [SerializeField] TextMeshProUGUI ratText;

    [Header("settings")]
    [SerializeField] float fadeTime = 0.3f;
    [SerializeField] float lineTime = 1.8f; // every line stays at least this long
    [SerializeField] float lineTimePerChar = 0.04f; // plus this for every letter
    [SerializeField] Vector2 rsPitch = new Vector2(1.1f, 1.4f);
    [SerializeField] float ratSFXvolume = 0.6f;

    [Header("lines (asset name = key)")]
    [SerializeField] RSDialogueObj[] buddyLines;

    class Entry
    {
        public string key;
        public RSDialogueObj dialogue;
        public string cardName;
        public bool isHint;
        public bool cleared;
    }

    Queue<Entry> queue = new Queue<Entry>();
    List<Entry> activeHints = new List<Entry>(); // hints that started and wait for their action
    HashSet<string> shown = new HashSet<string>();
    string currentLine;

    const string commentsPref = "RatComments";
    public bool CommentsOn => PlayerPrefs.GetInt(commentsPref, 1) == 1;

    void Awake()
    {
        instance = this;
        bubble.SetActive(false);
    }

    void Start()
    {
        StartCoroutine(TalkLoop());
    }

    /// <summary>
    /// Shows the hint (once) until ClearHint is called with the same key
    /// </summary>
    public void Hint(string key, Card card = null) => Add(key, card, true);

    /// <summary>
    /// Plays the comment (once), it goes away by itself. Muted when the comments are off
    /// </summary>
    public void Comment(string key, Card card = null)
    {
        if (CommentsOn) Add(key, card, false);
    }

    /// <summary>
    /// The player did what the hint asked, it goes away (or never shows if it's still waiting its turn)
    /// </summary>
    public void ClearHint(params string[] keys)
    {
        foreach (Entry entry in queue) if (Array.IndexOf(keys, entry.key) >= 0) entry.cleared = true;
        foreach (Entry entry in activeHints) if (Array.IndexOf(keys, entry.key) >= 0) entry.cleared = true;
        activeHints.RemoveAll(entry => entry.cleared);
    }

    public bool WasShown(string key) => shown.Contains(key);

    /// <summary>
    /// True while lines are still playing or waiting (a hint holding its last line doesn't count)
    /// </summary>
    public bool IsTalking => playing || queue.Count > 0;
    bool playing;

    /// <summary>
    /// For a settings button: turns the comments on/off (hints always show)
    /// </summary>
    public void ToggleComments()
    {
        PlayerPrefs.SetInt(commentsPref, CommentsOn ? 0 : 1);
    }

    void Add(string key, Card card, bool isHint)
    {
        if (!GameData.firstTimePlaying || shown.Contains(key)) return;

        RSDialogueObj dialogue = Array.Find(buddyLines, x => x.name == key);
        if (dialogue == null || dialogue.line.Count == 0) { Debug.Log("RatBuddy: lines not found for " + key); return; }

        shown.Add(key);
        queue.Enqueue(new Entry { key = key, dialogue = dialogue, cardName = (card != null) ? card.cardName : "", isHint = isHint });
    }

    /// <summary>
    /// Plays queued lines one after another. When nothing is queued, the newest waiting hint stays on its last line
    /// </summary>
    IEnumerator TalkLoop()
    {
        while (true)
        {
            if (queue.Count > 0)
            {
                Entry entry = queue.Dequeue();
                if (entry.cleared) continue;
                if (entry.isHint) activeHints.Add(entry);
                playing = true;

                foreach (string line in entry.dialogue.line)
                {
                    ShowLine(line.Replace("{card}", entry.cardName));

                    float t = 0;
                    float time = lineTime + line.Length * lineTimePerChar;
                    while (t < time && !entry.cleared)
                    {
                        t += Time.deltaTime;
                        yield return null;
                    }
                    if (entry.cleared) break;
                }
                playing = false;
            }
            else if (activeHints.Count > 0)
            {
                Entry hint = activeHints[activeHints.Count - 1];
                List<string> lines = hint.dialogue.line;
                ShowLine(lines[lines.Count - 1].Replace("{card}", hint.cardName));
                yield return null;
            }
            else
            {
                if (currentLine != null) yield return HideBubble();
                else yield return null;
            }
        }
    }

    void ShowLine(string line)
    {
        if (line == currentLine) return;
        currentLine = line;

        bubble.SetActive(true);
        ratText.GetComponent<AutoFade>().fadeInTime = fadeTime;
        ratText.gameObject.SetActive(false);
        ratText.gameObject.SetActive(true); // triggering fading in effect
        ratText.text = line;

        StartCoroutine(Animations.instance.SquashStretch(ratAvatar.transform, Vector3.one, 0.15f, 0.25f));
        AudioManager.instance.PlaySFX("Rat" + UnityEngine.Random.Range(1, 4) + "SFX", ratSFXvolume, UnityEngine.Random.Range(rsPitch.x, rsPitch.y));
    }

    IEnumerator HideBubble()
    {
        currentLine = null;
        ratText.GetComponent<AutoFade>().FadeOut(fadeTime);
        yield return new WaitForSeconds(fadeTime);
        if (currentLine == null) bubble.SetActive(false);
    }
}
