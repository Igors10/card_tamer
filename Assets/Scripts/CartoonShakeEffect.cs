using UnityEngine;
using System.Collections;

public class CartoonShakeEffect : MonoBehaviour
{
    [Header("shake anim")]
    [SerializeField] float idleRotateInterval = 1.5f;
    [SerializeField] float idleRotationIntensity = 3f;
    Coroutine anim;

    private void OnEnable()
    {
        anim = StartCoroutine(IdleAnim(transform.localEulerAngles));
    }

    private void OnDisable()
    {
        StopCoroutine(anim);
    }
    IEnumerator IdleAnim(Vector3 startingRotation)
    {
        // determine first rotation randomly
        bool doodleRotateLeft = Random.Range(0, 2) == 0;

        float t = 0;

        while (true)
        {
            t += Time.deltaTime;

            // rotating the doodle
            if (t > idleRotateInterval)
            {
                t = 0;
                doodleRotateLeft = !doodleRotateLeft;
                transform.localRotation = Quaternion.Euler(startingRotation.x, startingRotation.y, startingRotation.z);
                float doodleRotationAngle = (doodleRotateLeft) ? -idleRotationIntensity : idleRotationIntensity;
                transform.localRotation = Quaternion.Euler(startingRotation.x, startingRotation.y, startingRotation.z + doodleRotationAngle);
            }

            yield return null;
        }
    }

}
