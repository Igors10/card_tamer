using System;
using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    [Header("list of particle systems")]
    [SerializeField] ParticleSystem[] VFXs;

    [Header("refs")]
    public static ParticleManager instance;

    private void Start()
    {
        instance = this;
    }

    public void SpawnVFX(Vector3 position, string name)
    {
        ParticleSystem vfxPrefab = Array.Find(VFXs, x => x.name == name);
        if (vfxPrefab == null) { Debug.Log("ParticleManager: could not find particle effect"); return; }

        ParticleSystem newVFX = Instantiate(vfxPrefab, position, Quaternion.identity);
    }
}
