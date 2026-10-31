using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// 빛나는 근경 식물(담쟁이 등)의 빛 자리마다 작은 빛 가루가 반짝이며 천천히 떨어진다.
///
/// 근경 프리팹 안의 "Glow Light" 빛(Light2D)을 찾아 그 자리에 파티클을 붙인다. 색은 그 빛 색을 따른다.
/// 프리팹·씬 파일은 고치지 않는다. 씬이 열릴 때 스스로 붙는다 (실행 중에만 보인다).
/// 빛 하나당 1초에 1~2개 정도라 가볍다.
/// </summary>
public class FoliageGlowMotes : MonoBehaviour
{
    private const float RatePerLight = 1.6f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        for (int i = 0; i < SceneManager.sceneCount; i++) Attach(SceneManager.GetSceneAt(i));
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Attach(scene);

    private static void Attach(Scene scene)
    {
        if (!scene.isLoaded) return;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Light2D l in root.GetComponentsInChildren<Light2D>(true))
            {
                if (!l.name.StartsWith("Glow Light")) continue;
                if (l.GetComponent<FoliageGlowMotes>() != null) continue;
                l.gameObject.AddComponent<FoliageGlowMotes>();
            }
    }

    private void Start()
    {
        Light2D light = GetComponent<Light2D>();
        if (light == null) { enabled = false; return; }

        // 같은 근경 그림의 그리기 순서 바로 앞에
        SpriteRenderer art = transform.parent != null ? transform.parent.GetComponentInChildren<SpriteRenderer>() : null;

        GameObject go = new GameObject("Glow Motes");
        go.transform.SetParent(transform, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        float radius = Mathf.Clamp(light.pointLightOuterRadius * 0.35f, 0.3f, 1.4f);
        Color c = light.color; c.a = 1f;

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.duration = 4f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.11f);
        main.startColor = new ParticleSystem.MinMaxGradient(c, Color.Lerp(c, Color.white, 0.6f));
        main.maxParticles = 24;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = RatePerLight;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;

        // 잎에서 떨어지듯 천천히 아래로, 살랑살랑
        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.35f, -0.08f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.2f;
        noise.frequency = 0.5f;
        noise.quality = ParticleSystemNoiseQuality.Low;

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        // 반짝임
        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.5f), new Keyframe(0.2f, 1f), new Keyframe(0.4f, 0.6f),
            new Keyframe(0.6f, 1f), new Keyframe(0.8f, 0.6f), new Keyframe(1f, 0.3f)));

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sharedMaterial = VfxResources.MaterialFor(BossFxArt.SoftDot.texture);
        if (art != null)
        {
            r.sortingLayerID = art.sortingLayerID;
            r.sortingOrder = art.sortingOrder + 1;
        }

        ps.Play();
    }
}
