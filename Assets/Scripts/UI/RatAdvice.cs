using TMPro;
using UnityEditor.Analytics;
using UnityEngine;

public class RatAdvice : MonoBehaviour
{

    [Header("advice parts")] 
    string[] prefixeList = { "ultra", "seriously", "pretty", "slightly", "genuinely", "zoomed in", "poorly drawn", "big", "detailed"};
    string[] adjectiveList = { "metal", "black", "insane", "happy", "crazy", "small", "gigantic", "electronic", "old", "slimy", "rare", "tragic", "legendary", "handsome", "cute", "expressive", "armed", "mutated", 
        "magical", "cyber", "royal", "criminal", "proud", "fat", "sweet", "scary", "anime", "video game", "movie", "famous", "cat", "cool", "office"};
    string[] nounList = { "cat", "animal", "person", "robot", "bug", "food", "stickfigure", "monster", "magician", "character", "lizard", "fruit", "crocodile", "camel", "dog", "pig", "villager", "spider", "princess", 
        "rat", "knight"};
    string[] endingList = { "with wings", "flexing muscles", "with a beard", "with a muscle", "casting a spell", "driving a car", "on fire", "with lazers", "in outer space", "underwater", "with 4 arms", "dancing", 
        "in a cool pose", "in 3D"};

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
        drawingAdviceObj.SetActive(false);
        drawingAdviceObj.SetActive(true);
        adviceText.text = GetDrawingAdvice();
    }
}
