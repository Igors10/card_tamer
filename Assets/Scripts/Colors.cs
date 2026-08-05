using UnityEngine;
using System.Collections.Generic;

public class Colors : MonoBehaviour
{
    public static Colors instance;
    public List<Color> secondaryColorList = new List<Color>();
    public int secondaryColorAmount = 3;
    public List<Color> allColorList = new List<Color>();

    void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        allColorList = GameManager.instance.cardDatabase.allPlayerColors;
    }

    // Blends provided color with white
    public Color BlendColor(Color color, float blendCoof)
    {
        Color colorToReturn = Color.Lerp(Color.white, color, blendCoof);
        return colorToReturn;
    }

    /// <summary>
    /// Picks 3 random colors that neither player has as their primary color
    /// </summary>
    public void GenerateRandomSecondaryColors()
    {
        for (int i = 0; i < secondaryColorAmount; i++)
        {
            // picking random color from the list
            int randomColorID = 0;

            do
            {
                randomColorID = Random.Range(0, allColorList.Count);
            } while (ColorAlreadyInUse(allColorList[randomColorID]));

            // if it's not in use add it to the secondary list  color
            secondaryColorList.Add(allColorList[randomColorID]);

            Debug.Log("Colors: new secondary color -> " + secondaryColorList[i]);
        }
    }

    /// <summary>
    /// Checks if color is already used somewhere
    /// </summary>
    /// <param name="colorToCheck"></param>
    /// <returns></returns>
    bool ColorAlreadyInUse(Color colorToCheck)
    {
        // checking if color is already a main color of one of the players
        if (GameManager.instance.player.playerColor == colorToCheck) return true;
        else if (GameManager.instance.opponent.playerColor == colorToCheck) return true;

        // checking if the color is already one of the secondary colors
        for (int a = 0; a < secondaryColorList.Count; a++)
        {
            if (colorToCheck == secondaryColorList[a]) return true;
        }

        return false;
    }

    public Color GetRandomSecondaryColor() => secondaryColorList[Random.Range(0, secondaryColorList.Count)];
}
