using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class LobbyPanel : MonoBehaviour
{
    public TMP_Text nickname_text;
    public TMP_Text ready;
    public List<Toggle> starting_character_toggles = new List<Toggle>();
    public List<Sprite> toggle_sprites = new List<Sprite>();
    public Image toggle_image;
}
