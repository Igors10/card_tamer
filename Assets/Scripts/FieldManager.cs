using UnityEngine;

public class FieldManager : MonoBehaviour
{
    public Field[] fields = new Field[4];

    public void EnableSpawnPoints(bool enable)
    {
        for (int i = 0;  i < fields.Length; i++)
        {
            fields[i].EnableSpawnPoint(enable);
        }
    }

    /// <summary>
    /// Places a unit on a field from played card
    /// </summary>
    public void PlayCard()
    {
        for (int i = 0;i < fields.Length;i++)
        {
            fields[i].PlayCard(GameManager.instance.handManager.activeCard);
        }
        EnableSpawnPoints(false);
    }

}
