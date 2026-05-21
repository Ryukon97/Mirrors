using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class DestroyOnParticleEnds : MonoBehaviour
{
    ParticleSystem particle;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        particle = GetComponent<ParticleSystem>();
    }
    void Start()
    {
        particle.Play();
        Destroy(gameObject, particle.main.duration);
    }

}
