using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuDoodle : MonoBehaviour, IPointerClickHandler
{
    [Header("shake anim")]
    public float angle = 10f;       // How far it tilts left/right
    public float duration = 0.15f;  // How fast each swing is
    public int swings = 2;          // How many back-and-forths
    bool shaking = false;

    [Header("knock out anim")]
    [SerializeField] bool knockOutOnClick = false; // background doodles get knocked off the screen, starting choice doodles only shake
    [SerializeField] float flyXSpeed = 900f; // same arc as a knocked out unit, converted to menu pixels
    [SerializeField] float flyUpSpeed = 1400f;
    [SerializeField] float flyGravity = 4000f;
    [SerializeField] float flyRotationSpeed = 540f;
    [SerializeField] float maxFlyTime = 3f; // the doodle flies until it leaves the screen, this is only a safety limit
    [SerializeField] float respawnDelay = 0.4f;
    [SerializeField] float popTime = 0.3f;
    [SerializeField] float popIntensity = 0.4f;
    bool knockedOut = false;
    Vector3 restScale;
    Quaternion restRotation;

    [Header("refs")]
    [SerializeField] UnitSprite sprite;
    RectTransform rect;
    Coroutine currentAnim;

    public UnitPreset doodleData;


    void Start()
    {
        rect = GetComponent<RectTransform>();
        restScale = rect.localScale;
        restRotation = rect.localRotation;
        AssignDoodleData();
    }

    void AssignDoodleData()
    {
        // Picking random doodle if not assigned
        if (doodleData == null) doodleData = TitleScreen.instance.cardDatabase.GetRandomBasicPreset();

        // Sprite
        sprite.RefreshSprite(doodleData.sprite, TitleScreen.instance.playerConfigObj.playerColor, TitleScreen.instance.playerConfigObj.playerColor);
    }

    void PlayDoodleSound()
    {
        // plays the sound
        //AudioManager.instance.PlaySFX("DoodleSFX");
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
        shaking = true;

        for (int i = 0; i < swings; i++)
        {
            yield return RotateTo(angle, duration);
            yield return RotateTo(-angle, duration);
        }

        rect.localRotation = original;
        shaking = false;
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

    /// <summary>
    /// Knocks the doodle off the screen like a knocked out unit, then a new doodle pops up somewhere else
    /// </summary>
    IEnumerator KnockOutAndRespawn()
    {
        knockedOut = true;

        // stopping other animations that rotate the doodle
        if (currentAnim != null) StopCoroutine(currentAnim);
        CartoonShakeEffect[] idleShakes = GetComponents<CartoonShakeEffect>();
        foreach (CartoonShakeEffect idleShake in idleShakes) idleShake.StopAnim(true);

        //AudioManager.instance.PlaySFX("HitSFX");

        // flying off the screen (speeds are in canvas pixels, so they are converted to world space for the shared animation)
        float canvasScale = rect.parent.lossyScale.x;
        Vector3 velocity = new Vector3(Random.Range(-flyXSpeed, flyXSpeed), flyUpSpeed, 0f) * canvasScale;
        Vector3 gravity = new Vector3(0f, flyGravity, 0f) * canvasScale;
        Coroutine flight = StartCoroutine(Animations.instance.KnockOutFly(transform, velocity, gravity, flyRotationSpeed, maxFlyTime));

        // keeps flying until it's fully off the screen
        float t = 0f;
        while (t < maxFlyTime && !IsOffScreen())
        {
            t += Time.deltaTime;
            yield return null;
        }
        StopCoroutine(flight);

        yield return new WaitForSeconds(respawnDelay);

        // new doodle in a free spot
        rect.localScale = restScale;
        rect.localRotation = restRotation;
        rect.localPosition = TitleScreen.instance.GetFreeDoodlePosition(rect, restScale);
        doodleData = PickNewDoodle();
        AssignDoodleData();

        // popping in
        Animations.instance.PopAnim(gameObject, popTime, popIntensity);
        yield return new WaitForSeconds(popTime);

        foreach (CartoonShakeEffect idleShake in idleShakes) idleShake.StopAnim(false);
        knockedOut = false;
    }

    /// <summary>
    /// True when the doodle has left the screen through the bottom or the sides (menu canvas is screen space overlay, so world space is screen pixels)
    /// </summary>
    bool IsOffScreen()
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        Rect doodleRect = Rect.MinMaxRect(corners[0].x, corners[0].y, corners[0].x, corners[0].y);
        foreach (Vector3 corner in corners)
        {
            doodleRect.xMin = Mathf.Min(doodleRect.xMin, corner.x);
            doodleRect.yMin = Mathf.Min(doodleRect.yMin, corner.y);
            doodleRect.xMax = Mathf.Max(doodleRect.xMax, corner.x);
            doodleRect.yMax = Mathf.Max(doodleRect.yMax, corner.y);
        }

        // not checking the top, the doodle is still coming back down from there
        return doodleRect.yMax < 0f || doodleRect.xMax < 0f || doodleRect.xMin > Screen.width;
    }

    /// <summary>
    /// Picks a random doodle that is different from the current one
    /// </summary>
    UnitPreset PickNewDoodle()
    {
        UnitPreset newDoodle = TitleScreen.instance.cardDatabase.GetRandomBasicPreset();
        for (int i = 0; i < 10 && newDoodle == doodleData; i++) newDoodle = TitleScreen.instance.cardDatabase.GetRandomBasicPreset();
        return newDoodle;
    }

    // =================
    // Input
    // =================

    public void OnPointerClick(PointerEventData eventData)
    {
        if (knockedOut) return;

        if (knockOutOnClick) StartCoroutine(KnockOutAndRespawn());
        else
        {
            StartShakeAnim();
            PlayDoodleSound();
        }
    }
}
