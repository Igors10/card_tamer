using UnityEngine;

[CreateAssetMenu(fileName = "newState", menuName = "GameState")]
public class GameStateData : ScriptableObject 
{
    public string defaultHintText;

    // Button
    public string buttonText;
    public Color buttonColor;
    public string pressedButtonText;
    public Color pressedButtonColor;
}
