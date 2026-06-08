using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
public class DeathAnim : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] Image sprite;
    [SerializeField] List<Sprite> frame = new List<Sprite>(); // 0, 1 - thinking; 2- pointing

    [Header("settings")]
    [SerializeField] float animIntervals;
    bool pointing = false; 

    private void OnEnable()
    {
        StartCoroutine(IdleAnim());
    }

    /// <summary>
    /// Loop of death thinking animation
    /// </summary>
    /// <returns></returns>
    IEnumerator IdleAnim()
    {
        float t = 0;

        while (true)
        {
            yield return null;

            if (pointing) continue; // pausing when death is pointing

            t += Time.deltaTime;
            if (t > animIntervals)
            {
                t = 0;
                sprite.sprite = (sprite.sprite == frame[0]) ? frame[1] : frame[0];
            }
        }
    }

    /// <summary>
    /// Making death point finger for a specified amount of time
    /// </summary>
    /// <param name="length"></param>
    /// <returns></returns>
    public IEnumerator PointAnim(float length)
    {
        pointing = true;
        sprite.sprite = frame[2];
        yield return new WaitForSeconds(length);

        pointing = false;
        sprite.sprite = frame[0];
    }
}
