using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class LobbyPanel : MonoBehaviour
{
    public TMP_Text nickname_text;
    public TMP_Text ready;
    public Toggle r_toggle;
    public Toggle g_toggle;
    public List<Image> toggle_images = new List<Image> ();
}
