using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class LobbyPanel : MonoBehaviour
{
    public TMP_Text nicknameText;
    public TMP_Text ready;
    public List<Toggle> startingCharacterToggles = new List<Toggle>();
    public List<Sprite> toggleSprites = new List<Sprite>();
    public Image toggleImage;
}
