using FrogRunner.Core;
using FrogRunner.RiverGlider;
using UnityEngine;
using UnityEngine.Rendering;

namespace FrogRunner.WorldGen
{
    /// <summary>
    /// Builds the whole procedural course: highway, grass bank, river/log crossings, and long
    /// multi-round finish stretches. Everything is runtime geometry, so no scene assets to wire.
    /// TUNING: the Z constants below are the level layout. Keep round goals on safe ground.
    /// </summary>
    [DisallowMultipleComponent]
    public partial class CourseBuilder : MonoBehaviour
    {
        public const float HighwayStartZ = -45f;
        public const float HighwayEndZ = 175f;
        public const float GrassEndZ = 232f;
        public const float RiverStartZ = 232f;
        public const float RiverEndZ = 305f;
        public const float FinishZ = 1265f;
        public const float CourseEndZ = 1300f;
        public const float WaterSurfaceY = -0.8f;
        public const float RoadHalfWidth = 9.6f;
        public const float PlayableHalfWidth = 12f;

        public static CourseBuilder Instance { get; private set; }
        public float WaterLevel => WaterSurfaceY;

        Material _roadMaterial;
        Material _waterMaterial;

        public static CourseBuilder Build(Transform parent)
        {
            var root = new GameObject("Course");
            root.transform.SetParent(parent, false);
            var builder = root.AddComponent<CourseBuilder>();
            builder.Construct();
            return builder;
        }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Construct()
        {
            BuildAtmosphere();
            BuildRoad();
            BuildGrassBank();
            BuildRiver();
            BuildFarBank();
            BuildMarkings();
            BuildDecorations();
            BuildClouds();
        }

        void BuildAtmosphere()
        {
            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = new Material(skyShader) { name = "MAT_Sky" };
                if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", new Color(0.36f, 0.62f, 0.94f));
                if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", new Color(0.56f, 0.66f, 0.50f));
                if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", 0.55f);
                if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 1.35f);
                if (sky.HasProperty("_SunSize")) sky.SetFloat("_SunSize", 0.045f);
                RenderSettings.skybox = sky;
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.50f, 0.60f, 0.70f);
            RenderSettings.reflectionIntensity = 0.55f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.58f, 0.80f, 0.93f);

            Camera cam = FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.backgroundColor = new Color(0.58f, 0.80f, 0.93f);
            }

            BuildLights();
        }

        void BuildLights()
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            Light sun = null;
            Light fill = null;
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type != LightType.Directional) continue;
                if (sun == null) sun = lights[i];
                else if (fill == null) fill = lights[i];
            }

            if (sun == null)
            {
                var sunGo = new GameObject("Directional Light");
                sunGo.transform.SetParent(transform, false);
                sun = sunGo.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            if (fill == null)
            {
                var fillGo = new GameObject("Fill Light");
                fillGo.transform.SetParent(transform, false);
                fill = fillGo.AddComponent<Light>();
                fill.type = LightType.Directional;
            }

            sun.color = new Color(1f, 0.94f, 0.80f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.72f;
            sun.shadowBias = 0.03f;
            sun.transform.rotation = Quaternion.Euler(46f, -34f, 0f);

            fill.color = new Color(0.55f, 0.72f, 1f);
            fill.intensity = 0.32f;
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(28f, 142f, 0f);

            RenderSettings.sun = sun;
        }

        void BuildRoad()
        {
            _roadMaterial = RetroAssets.Surface(RetroAssets.Road, 5f, 55f);
            float length = HighwayEndZ - HighwayStartZ;
            float middle = (HighwayEndZ + HighwayStartZ) * 0.5f;

            RetroBuilder.Box("Road", transform, new Vector3(0f, -0.10f, middle),
                new Vector3(RoadHalfWidth * 2f, 0.20f, length), _roadMaterial, true);

            // Narrow dirt shoulders give the edge of the asphalt a readable pixel border.
            RetroBuilder.Box("ShoulderL", transform, new Vector3(-(RoadHalfWidth + 0.55f), -0.10f, middle),
                new Vector3(1.1f, 0.19f, length), RetroAssets.Dirt, true);
            RetroBuilder.Box("ShoulderR", transform, new Vector3(RoadHalfWidth + 0.55f, -0.10f, middle),
                new Vector3(1.1f, 0.19f, length), RetroAssets.Dirt, true);

            // Grass verge. It is walkable up to the playable clamp (12 m), then purely visual.
            RetroBuilder.Box("GrassL", transform, new Vector3(-(RoadHalfWidth + 17.2f), -0.12f, middle),
                new Vector3(33.2f, 0.24f, length), RetroAssets.Grass, true);
            RetroBuilder.Box("GrassR", transform, new Vector3(RoadHalfWidth + 17.2f, -0.12f, middle),
                new Vector3(33.2f, 0.24f, length), RetroAssets.Grass, true);

            // A checker start line under the frog so the opening shot reads as a race.
            RetroBuilder.Box("StartLine", transform, new Vector3(0f, 0.012f, 0f),
                new Vector3(RoadHalfWidth * 2f, 0.015f, 1.1f), RetroAssets.Checker, false, false, true);
        }

        void BuildGrassBank()
        {
            float length = GrassEndZ - HighwayEndZ;
            float middle = (GrassEndZ + HighwayEndZ) * 0.5f;
            RetroBuilder.Box("GrassBank", transform, new Vector3(0f, -0.10f, middle),
                new Vector3(72f, 0.20f, length), RetroAssets.Grass, true);

            // Mown strips and a few flower patches break up the huge green slab.
            for (int i = 0; i < 5; i++)
            {
                float z = HighwayEndZ + 8f + i * 10.5f;
                RetroBuilder.Box("MownStripe" + i, transform, new Vector3(0f, 0.004f, z),
                    new Vector3(64f, 0.01f, 1.4f), RetroAssets.Leaf2, false, false, true);
            }

            BuildSafeBase();
        }

        void BuildSafeBase()
        {
            // Visual rest pad near mid-course; end-of-round safe base is positioned by GameManager.
            float z = 196f;
            RetroBuilder.Box("SafeBasePlatform", transform, new Vector3(0f, 0.08f, z),
                new Vector3(18f, 0.35f, 12f), RetroAssets.Solid("MAT_SafeBase", new Color(0.22f, 0.35f, 0.22f), 0.05f), true, false, true);
            RetroBuilder.Box("SafeBasePad", transform, new Vector3(0f, 0.28f, z),
                new Vector3(12f, 0.12f, 6f), RetroAssets.Metal, false, false, true);
        }

        void BuildRiver()
        {
            float length = RiverEndZ - RiverStartZ;
            float middle = (RiverEndZ + RiverStartZ) * 0.5f;

            _waterMaterial = RetroAssets.Surface(RetroAssets.Water, 8f, 13f);
            GameObject water = RetroBuilder.Box("River", transform,
                new Vector3(0f, WaterSurfaceY - 0.12f, middle),
                new Vector3(72f, 0.24f, length), _waterMaterial, false, false, true);
            water.AddComponent<WaterScroller>();

            RetroBuilder.Box("RiverBed", transform, new Vector3(0f, -3.4f, middle),
                new Vector3(72f, 0.25f, length), RetroAssets.Solid("MAT_RiverBed", new Color(0.08f, 0.24f, 0.30f), 0.05f),
                false, false, false);

            // Dirt cliffs at both banks. They also stop the water plane edge from looking like paper.
            RetroBuilder.Box("RiverCliffNear", transform, new Vector3(0f, -1.5f, RiverStartZ - 0.75f),
                new Vector3(72f, 3f, 1.5f), RetroAssets.Dirt, false);
            RetroBuilder.Box("RiverCliffFar", transform, new Vector3(0f, -1.5f, RiverEndZ + 0.75f),
                new Vector3(72f, 3f, 1.5f), RetroAssets.Dirt, false);

            // White foam lines at the shoreline.
            RetroBuilder.Box("FoamNear", transform, new Vector3(0f, WaterSurfaceY + 0.005f, RiverStartZ + 0.9f),
                new Vector3(64f, 0.012f, 0.35f), RetroAssets.White, false, false, true);
            RetroBuilder.Box("FoamFar", transform, new Vector3(0f, WaterSurfaceY + 0.005f, RiverEndZ - 0.9f),
                new Vector3(64f, 0.012f, 0.35f), RetroAssets.White, false, false, true);

            // Trigger volume sits just below the visual surface so gliding over it is safe.
            var waterVolumeGo = new GameObject("WaterVolume");
            waterVolumeGo.transform.SetParent(transform, false);
            waterVolumeGo.transform.position = new Vector3(0f, WaterSurfaceY - 1.65f, middle);
            var volume = waterVolumeGo.AddComponent<WaterVolume>();
            volume.WaterLevel = WaterSurfaceY;
            var box = waterVolumeGo.GetComponent<BoxCollider>();
            box.size = new Vector3(72f, 3.0f, length);
            box.center = Vector3.zero;

            for (int i = 0; i < 12; i++)
            {
                float z = RiverStartZ + 10f + i * 5.8f;
                float x = ((i % 3) - 1) * 5.2f + (i % 2 == 0 ? 1.2f : -1.2f);
                FloatingLog.Create(transform,
                    new Vector3(x, WaterSurfaceY + 0.35f, z),
                    1.1f + (i % 4) * 0.15f, 0.32f + (i % 5) * 0.03f, i * 1.37f);
            }
        }

        void BuildFarBank()
        {
            float length = CourseEndZ - RiverEndZ;
            float middle = (CourseEndZ + RiverEndZ) * 0.5f;
            RetroBuilder.Box("FarBank", transform, new Vector3(0f, -0.10f, middle),
                new Vector3(72f, 0.20f, length), RetroAssets.Grass, true);

            // Long multi-round course: alternating grass runs and road strips for 10 rounds.
            for (int i = 0; i < 10; i++)
            {
                float segStart = RiverEndZ + 20f + i * 95f;
                float segMid = segStart + 35f;
                if (segMid > CourseEndZ - 20f) break;
                RetroBuilder.Box($"RunRoad{i}", transform, new Vector3(0f, -0.08f, segMid),
                    new Vector3(RoadHalfWidth * 2f, 0.18f, 70f), _roadMaterial != null ? _roadMaterial : RetroAssets.Road, true);
                RetroBuilder.Box($"RunPath{i}", transform, new Vector3(0f, 0.006f, segStart + 75f),
                    new Vector3(6.4f, 0.02f, 18f), RetroAssets.Dirt, false, false, true);
            }
        }

        public bool IsOverRiver(Vector3 position)
        {
            return position.z > RiverStartZ && position.z < RiverEndZ;
        }
    }
}
