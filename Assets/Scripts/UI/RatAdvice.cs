using TMPro;
using UnityEngine;

public class RatAdvice : MonoBehaviour
{

    [Header("advice parts")] 
    string[] prefixeList = { "ultra", "seriously", "pretty", "slightly", "genuinely", "zoomed in", "poorly drawn", "big", "detailed",
        "very", "extremely", "kind of", "totally", "suspiciously", "mildly", "absurdly", "blurry", "super"};
    string[] adjectiveList = { "metal", "black", "insane", "happy", "crazy", "small", "gigantic", "electronic", "old", "slimy", "rare", "tragic", "legendary", "handsome", "cute", "expressive", "armed", "mutated", 
        "magical", "cyber", "royal", "criminal", "proud", "fat", "sweet", "scary", "anime", "video game", "movie", "famous", "cat", "cool", "office",
        "wooden", "golden", "sleepy", "angry", "fluffy", "hairy", "spiky", "invisible", "ghost", "zombie", "baby", "ancient", "pirate", "ninja", "space", "frozen", "shy",
        "evil", "fancy", "tiny", "stinky", "rubber", "glass", "candy", "punk", "wizard", "hungry", "nervous", "retro", "medieval", "alien", "jelly", "rich"};
    string[] nounList = { "cat", "animal", "person", "robot", "bug", "food", "stickfigure", "monster", "magician", "character", "lizard", "fruit", "crocodile", "camel", "dog", "pig", "villager", "spider", "princess", 
        "rat", "knight",
        "frog", "bird", "fish", "dragon", "snail", "chef", "cowboy", "octopus", "penguin", "mushroom", "vegetable", "plant", "car", "skeleton", "dinosaur", "shark", "owl",
        "bear", "king", "sandwich", "goblin"};
    string[] endingList = { "with wings", "flexing muscles", "with a beard", "with a muscle", "casting a spell", "driving a car", "on fire", "with lazers", "in outer space", "underwater", "with 4 arms", "dancing", 
        "in a cool pose", "in 3D",
        "wearing a hat", "riding a horse", "holding a sword", "eating a sandwich", "with a jetpack", "in a tiny boat", "playing guitar", "with sunglasses", "sleeping",
        "made of cheese", "on a skateboard", "with a pet", "in disguise", "falling from the sky"};

    float prefixChance = 0.1f;
    float endingChance = 0.15f;

    [Header("refs")]
    [SerializeField] GameObject drawingAdviceObj;
    [SerializeField] TextMeshProUGUI adviceText;

    string GetDrawingAdvice()
    {
        string randomAdjective = adjectiveList[Random.Range(0, adjectiveList.Length)];
        string randomNoun = nounList[Random.Range(0, nounList.Length)];
        string randomPrefix = (Random.value < prefixChance) ? prefixeList[Random.Range(0, prefixeList.Length)] + " ": "";
        string randomEnding = (Random.value < endingChance) ? " " + endingList[Random.Range(0, endingList.Length)] : "";

        string drawingAdvice = "Drawing idea: " + randomPrefix + randomAdjective + " " + randomNoun + randomEnding;
        return drawingAdvice;
    }

    public void NewAdvice()
    {
        AudioManager.instance.PlaySFX("PaperSFX");

        drawingAdviceObj.SetActive(false);
        drawingAdviceObj.SetActive(true);
        adviceText.text = GetDrawingAdvice();
    }
}
