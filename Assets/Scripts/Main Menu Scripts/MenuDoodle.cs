using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuDoodle : MonoBehaviour, IPointerClickHandler
{
    public float angle = 10f;       // How far it tilts left/right
    public float duration = 0.15f;  // How fast each swing is
    public int swings = 2;          // How many back-and-forths

    Image sprite;
    RectTransform rect;
    Coroutine currentAnim;

    public CreatureObj doodleData;


    void Start()
    {
        sprite = GetComponent<Image>();
        rect = GetComponent<RectTransform>();
        AssignDoodleData();
        
    }

    void AssignDoodleData()
    {
        // Picking random doodle if not assigned
        if (doodleData == null) doodleData = TitleScreen.instance.cardlist.cardList[Random.Range(0, TitleScreen.instance.cardlist.cardList.Count)];

        // Sprite
        sprite.sprite = doodleData.doodleSprite;
    }

    void PlayDoodleSound()
    {
        // picking random sound from creature's sounds
        AudioClip clipToPlay = doodleData.DoodleSound();

        // plays the sound
        TitleScreen.instance.PlaySound(clipToPlay);
    }

    // =======================
    // Doodle Animation
    // =======================

    /// <summary>
    /// Starts shaking (rocking) back and forth animation
    /// </summary>
    public void StartShakeAnim()
    {
        if (currentAnim != null)
            StopCoroutine(currentAnim);

        currentAnim = StartCoroutine(ShakeAnim());
    }

    IEnumerator ShakeAnim()
    {
        Quaternion original = rect.localRotation;

        for (int i = 0; i < swings; i++)
        {
            yield return RotateTo(angle, duration);
            yield return RotateTo(-angle, duration);
        }

        rect.localRotation = original;
    }

    IEnumerator RotateTo(float targetAngle, float time)
    {
        float t = 0f;
        float start = rect.localEulerAngles.z;
        if (start > 180) start -= 360;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / time;
            float eased = Mathf.SmoothStep(0, 1, t);
            float angle = Mathf.Lerp(start, targetAngle, eased);
            rect.localRotation = Quaternion.Euler(0, 0, angle);
            yield return null;
        }
    }

    // =================
    // Input
    // =================

    public void OnPointerClick(PointerEventData eventData)
    {
        StartShakeAnim();
        PlayDoodleSound();
    }
}