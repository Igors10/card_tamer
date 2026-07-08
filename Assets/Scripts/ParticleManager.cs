using System;
using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    [Header("list of particle systems")]
    [SerializeField] ParticleSystem[] VFXs;

    [Header("refs")]
    public static ParticleManager instance;
    [SerializeField] GameObject canvasObj;

    private void Start()
    {
        instance = this;
    }

    /// <summary>
    /// Instantiates chosen VFXs on a given position
    /// </summary>
    /// <param name="position"></param>
    /// <param name="name"></param>
    /// <param name="onUI"></param>
    public void SpawnVFX(Vector3 position, string name, bool onUI = false)
    {
        ParticleSystem vfxPrefab = Array.Find(VFXs, x => x.name == name);
        if (vfxPrefab == null) { Debug.Log("ParticleManager: could not find particle effect"); return; }
        else Debug.Log("ParticleManager: spawning particle effect " + name + " at " + position);

        if (onUI)
        {
            ParticleSystem newVFX = Instantiate(vfxPrefab, canvasObj.transform);
            position.z = -1f;
            newVFX.transform.position = position;
            newVFX.transform.localScale = Vector3.one * 100f;
        }
        else
        {
            ParticleSystem newVFX = Instantiate(vfxPrefab, position, Quaternion.identity);
        }           
    }
}
