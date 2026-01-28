using UnityEngine;

public class VFXManager : MonoBehaviour
{
    [SerializeField] bool enableVFX;
    public void PlayVFX(Vector3 position, ParticleSystem VFX)
    {
        if (!enableVFX) return;
        ParticleSystem playedVFX = Instantiate(VFX, position, Quaternion.identity);
    }
}
