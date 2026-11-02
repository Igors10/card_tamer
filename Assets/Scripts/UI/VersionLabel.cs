using TMPro;
using UnityEngine;

// Shows the game version (Project Settings > Player > Version).
[RequireComponent(typeof(TMP_Text))]
public class VersionLabel : MonoBehaviour
{
    void Awake()
    {
        GetComponent<TMP_Text>().text = "v" + Application.version;
    }
}
