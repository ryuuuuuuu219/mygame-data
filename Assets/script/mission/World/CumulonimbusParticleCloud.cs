using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class CumulonimbusParticleCloud : MonoBehaviour
{
    [Header("Cloud Shape")]
    public int particleCount = 2000;
    public float height = 80f;
    public float baseRadius = 45f;
    public float topRadius = 35f;
    public float bulge = 30f;

    [Header("Noise")]
    public float noiseScale = 0.08f;
    public float densityThreshold = 0.45f;

    [Header("Particle")]
    public float minSize = 8f;
    public float maxSize = 22f;

    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;

    private void Start()
    {
        GenerateCloud();
    }

    public void GenerateCloud()
    {
        ps = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[particleCount];

        int index = 0;
        int safety = particleCount * 20;

        while (index < particleCount && safety-- > 0)
        {
            Vector3 p = new Vector3(
                Random.Range(-baseRadius, baseRadius),
                Random.Range(0f, height),
                Random.Range(-baseRadius, baseRadius)
            );

            float t = p.y / height;
            float allowedRadius = Mathf.Lerp(baseRadius, topRadius, t) + Mathf.Sin(t * Mathf.PI) * bulge;

            if (t < 0.25f)
            {
                allowedRadius *= 1.2f;
            }

            float horizontalDistance = new Vector2(p.x, p.z).magnitude;
            if (horizontalDistance > allowedRadius)
            {
                continue;
            }

            float edgeFade = 1f - horizontalDistance / allowedRadius;
            float noise = Noise3D(p.x * noiseScale, p.y * noiseScale, p.z * noiseScale);
            float density = noise * edgeFade;

            if (density < densityThreshold)
            {
                continue;
            }

            float size = Mathf.Lerp(minSize, maxSize, density);

            particles[index].position = p;
            particles[index].startSize = size;
            particles[index].startColor = new Color(1f, 1f, 1f, Mathf.Lerp(0.25f, 0.8f, density));
            particles[index].remainingLifetime = float.MaxValue;
            particles[index].startLifetime = float.MaxValue;

            index++;
        }

        ps.SetParticles(particles, index);
    }

    private float Noise3D(float x, float y, float z)
    {
        float xy = Mathf.PerlinNoise(x, y);
        float yz = Mathf.PerlinNoise(y, z);
        float xz = Mathf.PerlinNoise(x, z);
        return (xy + yz + xz) / 3f;
    }
}
