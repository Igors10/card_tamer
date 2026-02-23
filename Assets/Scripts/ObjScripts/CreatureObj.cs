using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Creature", menuName = "Creature")]
public class CreatureObj : ScriptableObject
{
    public string creatureName;
    public int health;
    public Sprite cardSprite;
    public Sprite unitSprite;
    public FoodType foodType;
    public int cost;
    public AbilityObj[] ability = new AbilityObj[2];

    // doodle varialbes
    public Sprite doodleSprite;
    public AudioClip[] audio = new AudioClip[0];

    public AudioClip DoodleSound()
    {
        int randomSound = Random.Range(0, audio.Length);
        return audio[randomSound];
    }
}
