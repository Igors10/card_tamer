using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One of the player's hearts in the player UI. A lost heart disappears (PlayerUI plays the broken heart VFX over it)
/// </summary>
public class PlayerHeart : MonoBehaviour
{
    [SerializeField] GameObject fill;
    [SerializeField] Image outline;

    public bool IsFull => fill.activeSelf;

    public void SetFull(bool full)
    {
        fill.SetActive(full);
        outline.gameObject.SetActive(full);
    }
}
