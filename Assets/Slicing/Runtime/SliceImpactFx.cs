using UnityEngine;

namespace Slicing
{
    public static class SliceImpactFx
    {
        public static GameObject CreateRuntimeImpactPrefab()
        {
            var go = new GameObject("SliceImpact_RT");

            var ps = go.AddComponent<ParticleSystem>();
            // Stop the freshly-added system so we can mutate immutable-while-playing properties.
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1.0f, 0.85f, 0.45f), new Color(1.0f, 0.55f, 0.18f));
            main.gravityModifier = 0.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.burstCount = 1;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 32));

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.05f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.4f, 0.1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = grad;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.1f));

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(SliceShaders.ParticleUnlit()) { name = "SliceImpactMat_RT" };
            SliceShaders.SetTint(mat, new Color(1f, 0.7f, 0.3f, 1f));
            renderer.material = mat;

            // Light burst
            var lightGo = new GameObject("Flash");
            lightGo.transform.SetParent(go.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.7f, 0.4f);
            light.intensity = 4f;
            light.range = 4f;
            lightGo.AddComponent<SliceImpactLightFade>();

            return go;
        }
    }

    internal sealed class SliceImpactLightFade : MonoBehaviour
    {
        public float duration = 0.35f;
        Light _light;
        float _t;
        float _i0;

        void Awake()
        {
            _light = GetComponent<Light>();
            if (_light != null) _i0 = _light.intensity;
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(1f - _t / duration);
            if (_light != null) _light.intensity = _i0 * k * k;
            if (_t >= duration && transform.parent != null)
                gameObject.SetActive(false);
        }
    }
}
