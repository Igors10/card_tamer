using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class OrderMarker : MonoBehaviour
{
    public Image markerBackground;
    public TextMeshProUGUI number;
    [SerializeField] List<Color> colors = new List<Color>();

    public void SetNumber(int newNumber)
    {
        number.text = newNumber.ToString();

        markerBackground.color = colors[newNumber];
    }
    
}
