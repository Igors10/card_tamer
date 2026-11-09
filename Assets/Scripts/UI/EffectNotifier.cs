using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pops up before a sticker of a card that isn't fighting triggers (Cheerer, Guard, Joker, Scout...):
/// shows the card's creature on its owner's color and the sticker, stays a moment, then the effect plays.
/// The local player's notifier appears where it's placed in the scene, the opponent's mirrored on the right.
/// A fighting card's own sticker pops up under its unit instead
/// </summary>
[RequireComponent(typeof(AutoFade))]
public class EffectNotifier : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] Image portraitBG; // owner player's color, lighter
    [SerializeField] UnitSprite unitSprite;
    [SerializeField] GameObject questionMark; // instead of the creature when the local player doesn't know the card
    [SerializeField] Ability sticker;

    [Header("timing")]
    [SerializeField] float showTime = 0.8f; // on the screen this long before the effect triggers
    [SerializeField] float fadeOutTime = 0.3f;

    Vector3 playerPosition;
    bool playerPositionSaved;
    Unit stickerUnit; // a fighting unit showing its own sticker under itself instead of the notifier (null: the notifier is used)

    /// <summary>
    /// Shows the notifier for the sticker strip's card, done after the show time (the effect triggers next).
    /// customText replaces the sticker description (short text about what the sticker is doing)
    /// </summary>
    public IEnumerator Show(Ability strip, string customText = null)
    {
        if (strip == null || strip.card == null) yield break;

        Card owner = strip.card;
        Player p = owner.player;

        // a fighting card's own sticker pops up under its unit instead (the unit is right there)
        if (owner.onDisplay && owner.unit != null && !owner.unit.knockedOut)
        {
            stickerUnit = owner.unit;
            AudioManager.instance.PlaySFX("EffectSFX");
            stickerUnit.ShowEffectSticker(strip, customText);
            yield break;
        }
        stickerUnit = null;

        // the opponent's one is mirrored to the right side of the screen (the parent is centered on the screen)
        if (!playerPositionSaved) { playerPosition = transform.localPosition; playerPositionSaved = true; }
        transform.localPosition = (p == GameManager.instance.player) ? playerPosition : new Vector3(-playerPosition.x, playerPosition.y, playerPosition.z);

        portraitBG.color = Colors.Lighten(p.playerColor);
        // an unknown card (opponent's face down) shows a question mark instead of its creature
        bool unknown = owner.HiddenFromLocalPlayer;
        unitSprite.gameObject.SetActive(!unknown);
        questionMark.SetActive(unknown);
        if (!unknown) unitSprite.RefreshSprite(owner.cardData.unitSprite, p.playerColor, owner.cardData.secondaryColor);
        sticker.InitAbility(new Sticker(strip.abilityData, strip.onRight), owner);
        if (!string.IsNullOrEmpty(customText)) sticker.SetText(customText);

        // turning it off first restarts the fade in (and stops a fade out left from the previous effect)
        gameObject.SetActive(false);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        AudioManager.instance.PlaySFX("EffectSFX");

        yield return new WaitForSeconds(showTime);
    }

    /// <summary>
    /// Fades out after the effect
    /// </summary>
    public void Hide()
    {
        if (stickerUnit != null)
        {
            stickerUnit.HideEffectSticker();
            stickerUnit = null;
            return;
        }

        if (gameObject.activeInHierarchy) GetComponent<AutoFade>().FadeOut(fadeOutTime);
    }
}
