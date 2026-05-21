using System.Collections;
using UnityEngine;

public static class ImpactEffectFactory
{
    const float DefaultSingleRadius = 3f;

    public static void Spawn(Vector3 position, float effectRadius)
    {
        float radius = effectRadius > 0f ? effectRadius : DefaultSingleRadius;
        var root = new GameObject(effectRadius > 0f ? "AoEImpactEffect" : "ImpactEffect");
        root.transform.position = position;

        var explosion = root.AddComponent<ImpactExplosionEffect>();
        explosion.Initialize(radius, effectRadius > 0f);

        CreateParticleSystem(root.transform, radius, effectRadius > 0f);
    }

    static void CreateParticleSystem(Transform parent, float radius, bool isAreaEffect)
    {
        var particleObject = new GameObject("ImpactParticles");
        particleObject.transform.SetParent(parent, false);

        var particles = particleObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = isAreaEffect ? 0.55f : 0.28f;
        main.startLifetime = isAreaEffect ? 0.55f : 0.22f;
        main.startSpeed = Mathf.Max(4f, radius * (isAreaEffect ? 0.35f : 1.2f));
        main.startSize = Mathf.Max(0.2f, radius * (isAreaEffect ? 0.08f : 0.18f));
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.85f, 0.18f, 0.9f),
            new Color(1f, 0.25f, 0.05f, 0.75f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)(isAreaEffect ? 96 : 28))
        });

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = Mathf.Max(0.1f, radius * 0.12f);

        var color = particles.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.yellow, 0f),
                new GradientColorKey(new Color(1f, 0.1f, 0f), 0.45f),
                new GradientColorKey(Color.black, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.5f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = gradient;

        particles.Play();
    }
}

public class ImpactExplosionEffect : MonoBehaviour
{
    static Material sharedMaterial;

    public float duration = 0.65f;
    public float alpha = 0.6f;
    public int minShardCount = 3;
    public int maxShardCount = 5;

    readonly Color yellow = new(1f, 0.85f, 0.05f, 0.6f);
    readonly Color red = new(1f, 0.05f, 0f, 0.45f);
    readonly Color black = new(0f, 0f, 0f, 0f);

    Transform baseBox;
    Transform[] shards;
    Vector3[] shardDirections;
    Vector3[] shardStartPositions;
    float radius;

    public void Initialize(float effectRadius, bool isAreaEffect)
    {
        radius = Mathf.Max(0.1f, effectRadius);
        baseBox = CreateBox("ExplosionCore", transform, true);
        baseBox.localScale = Vector3.one * radius * 2f;

        int shardCount = Random.Range(minShardCount, maxShardCount + 1);
        shards = new Transform[shardCount];
        shardDirections = new Vector3[shardCount];
        shardStartPositions = new Vector3[shardCount];

        for (int i = 0; i < shardCount; i++)
        {
            shards[i] = CreateBox("ExplosionShard", transform, false);
            shards[i].localScale = Vector3.one * radius * Random.Range(0.45f, 0.8f);
            shardDirections[i] = Random.onUnitSphere;
            shardDirections[i].y = Mathf.Abs(shardDirections[i].y) * 0.35f;
            shardDirections[i].Normalize();
            shardStartPositions[i] = shardDirections[i] * radius * Random.Range(0.05f, 0.18f);
            shards[i].localPosition = shardStartPositions[i];
            shards[i].rotation = Random.rotation;
        }

        StartCoroutine(Animate());
    }

    Transform CreateBox(string objectName, Transform parent, bool addCollider)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = objectName;
        box.transform.SetParent(parent, false);
        box.GetComponent<MeshRenderer>().material = GetMaterial();

        var collider = box.GetComponent<BoxCollider>();
        if (addCollider)
        {
            collider.isTrigger = true;
        }
        else
        {
            Destroy(collider);
        }

        return box.transform;
    }

    Material GetMaterial()
    {
        if (sharedMaterial != null)
            return new Material(sharedMaterial);

        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        sharedMaterial = new Material(shader);
        sharedMaterial.color = yellow;
        sharedMaterial.SetFloat("_Surface", 1f);
        sharedMaterial.SetFloat("_Blend", 0f);
        sharedMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        sharedMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        sharedMaterial.SetInt("_ZWrite", 0);
        sharedMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        sharedMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return new Material(sharedMaterial);
    }

    IEnumerator Animate()
    {
        float t = 0f;
        while (t < duration)
        {
            float normalized = t / duration;
            Color color = normalized < 0.45f
                ? Color.Lerp(yellow, red, normalized / 0.45f)
                : Color.Lerp(red, black, (normalized - 0.45f) / 0.55f);
            color.a *= alpha / 0.6f;

            SetColor(baseBox, color);
            baseBox.localPosition = Vector3.zero;
            baseBox.localScale = Vector3.one * radius * 2f * Mathf.Lerp(0.92f, 1.08f, normalized);

            for (int i = 0; i < shards.Length; i++)
            {
                SetColor(shards[i], color);
                shards[i].localPosition = shardStartPositions[i] + shardDirections[i] * radius * 0.28f * normalized;
                shards[i].localScale *= 1f + Time.deltaTime * 0.5f;
            }

            t += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    void SetColor(Transform target, Color color)
    {
        if (target == null) return;
        if (!target.TryGetComponent(out MeshRenderer renderer)) return;

        renderer.material.color = color;
    }
}
