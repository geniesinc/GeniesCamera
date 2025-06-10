using UnityEngine;

public class TeleportParticlesController : MonoBehaviour
{
    private ParticleSystem[] _scalableParticles;

    public void Initialize(float particleScale)
    {
        _scalableParticles = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < _scalableParticles.Length; i++)
        {
            _scalableParticles[i].transform.localScale *= particleScale;
        }   
    }

    private void Update()
    {
        // Do nothing while even one child is alive
        for (int i = 0; i < _scalableParticles.Length; i++)
        {
            if (_scalableParticles[i].IsAlive())
            {
                return;
            }
        }

        // No child is alive. Destroy object.
        Destroy(gameObject);
    }
}
