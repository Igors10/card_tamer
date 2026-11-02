using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One sticker on a card: the ability and which side it works with (only matters for stickers with a side)
/// </summary>
[System.Serializable]
public class Sticker
{
    public AbilityObj ability;
    public bool onRight;

    public Sticker(AbilityObj ability, bool onRight)
    {
        this.ability = ability;
        this.onRight = onRight;
    }
}

[CreateAssetMenu(fileName = "New Creature", menuName = "Creature")]
public class CreatureObj : ScriptableObject
{
    public const int maxStickers = 3; // base sticker + 2 add-on stickers

    public string creatureName;
    public Sprite[] unitSprite = new Sprite[2];
    public Color secondaryColor;
    public Color abilityColor;
    public List<Sticker> stickers = new List<Sticker>(); // first one is the base sticker

    // doodle varialbes
    public Sprite doodleSprite;
    public AudioClip[] audio = new AudioClip[0];

    /// <summary>
    /// Base sticker decides the card's power, special base stickers make the card special
    /// </summary>
    public AbilityObj BaseSticker => (stickers.Count > 0) ? stickers[0].ability : null;
    public bool IsSpecial => BaseSticker != null && BaseSticker.isSpecial;
    public bool CanAddSticker => stickers.Count < maxStickers;

    public AudioClip DoodleSound()
    {
        int randomSound = Random.Range(0, audio.Length);
        return audio[randomSound];
    }
}
