using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [SerializeField] GameObject battleUI;
    int currentLine = -1;

    void ResetBattle()
    {
        currentLine = -1;
    }

    /// <summary>
    /// Switches to next battle line
    /// </summary>
    public void NextLine()
    {
        currentLine++;
        if (currentLine < GameManager.instance.player.fields.Length) InitBattleLine();
        // Ending the battle phase after finished with last line
        else
        {
            GameManager.instance.player.endStateReady = true;
            GameManager.instance.opponent.endStateReady = true;
            GameManager.instance.EndTurn();
        }
    }

    /// <summary>
    /// Focuses camera and visuals on current battle line
    /// </summary>
    public void InitBattleLine()
    {
        Player player = GameManager.instance.player;
        Player opponent = GameManager.instance.opponent;
        Field field = player.fields[currentLine];
        Field oppField = opponent.fields[currentLine];


        // Moving the camera
        Vector3 posBetweenFields = Vector3.Lerp(field.transform.position, oppField.transform.position, 0.5f);
        Vector3 targetPos = new Vector3(posBetweenFields.x, Camera.main.transform.position.y, Camera.main.transform.position.z);
        StartCoroutine(Camera.main.GetComponent<Viewpoint>().MoveCamera(targetPos, 0.6f));

        // Fading all lines, except current line fields
        for (int i = 0; i < player.fields.Length; i++)
        {
            player.fields[i].FadeOut(i != currentLine);
            opponent.fields[i].FadeOut(i != currentLine);
        }
    }
}
