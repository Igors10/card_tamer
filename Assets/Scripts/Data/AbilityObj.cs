using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(fileName = "New Ability", menuName = "Ability")]
public class AbilityObj : ScriptableObject
{
    public int power;
    public string abilityDescription;
    [Tooltip("Special abilities make the card special: purple power counter, standout entrance, 2 dice when rolling, 1 per player")]
    public bool isSpecial;

    [Header("sticker")]
    [Tooltip("Color of the sticker strip on the card")]
    public Color stickerColor = Color.white;
    [Tooltip("Sticker has a left and a right version, picked at random when it's offered. Write {side} in the description")]
    public bool hasSide;
    [Tooltip("Icon on the card's sticker strip and on the workshop note, tinted with IconColor")]
    public Sprite icon;

    /// <summary>
    /// Icon tint: the sticker's color, a bit stronger so it shows on the strip
    /// </summary>
    public Color IconColor
    {
        get
        {
            Color.RGBToHSV(stickerColor, out float h, out float s, out float v);
            return Color.HSVToRGB(h, Mathf.Clamp01(s * 1.8f), v * 0.9f);
        }
    }

    [SerializeReference, EffectPicker] public List<AbilityEffect> effects = new List<AbilityEffect>();

    /// <summary>
    /// Power value shown on cards and notes, rolling abilities show the range they can roll (like 1-6)
    /// </summary>
    public string GetPowerText()
    {
        if (!effects.Exists(x => x is RollPowerEffect)) return power.ToString();

        int numberOfDice = isSpecial ? 2 : 1; // same as Unit.RollPower
        return (power + numberOfDice) + "-" + (power + 6 * numberOfDice);
    }

    public string GetAbilityDesc(Color abilityColor, bool onRight = false)
    {
        string abilityText = abilityDescription;

        // coloring the power modifier numbers, buffs green and debuffs red
        abilityText = Regex.Replace(abilityText, @"\+\d+", "<color=#2DB043>$0</color>");
        abilityText = Regex.Replace(abilityText, @"\-\d+", "<color=red>$0</color>");
        abilityText = Regex.Replace(abilityText, @"\b[Bb]uffs?\b", "<color=#2DB043>$0</color>");

        // coloring the 'color' of abilities that work with specific color
        string hexColor = ColorUtility.ToHtmlStringRGB(abilityColor);
        abilityText = abilityText.Replace("{color}", $"<color=#{hexColor}>color</color>");

        // left or right version of the sticker
        abilityText = abilityText.Replace("{side}", onRight ? "right" : "left");

        return abilityText;
    }
}
