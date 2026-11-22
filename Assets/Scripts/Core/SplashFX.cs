using UnityEngine;

namespace FrogRunner.Core
{
    /// <summary>
    /// One-shot splash burst used when the frog hits deep river water.
    /// TUNING: burst count/lifetime/speed are in Play().
    /// </summary>
    public static class SplashFX
    {
        static Material _material;

        public static void Play(Vector3 position)
        {
            var go = new GameObject("SplashFX");
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.4f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.30f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.70f, 0.92f, 1f, 0.95f));
            main.gravityModifier = 1.4f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)32) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.28f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.sharedMaterial = GetMaterial();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            ps.Play(true);
            Object.Destroy(go, 1.6f);
        }

        static Material GetMaterial()
        {
            if (_material != null) return _material;
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Standard");
            _material = new Material(shader) { name = "MAT_Splash" };
            if (_material.HasProperty("_Color")) _material.color = new Color(0.70f, 0.92f, 1f, 0.9f);
            return _material;
        }
    }
}
