using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New dialogue", menuName = "Dialogue")]
public class RSDialogueObj : ScriptableObject
{
    public List<string> line = new List<string>();
}