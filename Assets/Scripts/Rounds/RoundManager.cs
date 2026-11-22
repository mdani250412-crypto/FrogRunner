using FrogRunner.Core;
using UnityEngine;

namespace FrogRunner.Rounds
{
    /// <summary>10-round progression with distinct map theme per round.</summary>
    [DisallowMultipleComponent]
    public class RoundManager : MonoBehaviour
    {
        public static RoundManager Instance { get; private set; }

        public struct MapTheme
        {
            public string Name;
            public Color Sky;
            public Color Fog;
            public Color Ground;
            public Color Accent;
        }

        public static readonly MapTheme[] Themes =
        {
            new MapTheme { Name="Highway & River",     Sky=new Color(0.58f,0.80f,0.93f), Fog=new Color(0.70f,0.82f,0.90f), Ground=new Color(0.22f,0.45f,0.20f), Accent=new Color(0.35f,0.35f,0.38f) },
            new MapTheme { Name="Sunset Canyon",       Sky=new Color(0.95f,0.55f,0.35f), Fog=new Color(0.90f,0.62f,0.45f), Ground=new Color(0.62f,0.45f,0.30f), Accent=new Color(0.75f,0.35f,0.20f) },
            new MapTheme { Name="Neon Night City",     Sky=new Color(0.08f,0.08f,0.18f), Fog=new Color(0.12f,0.10f,0.22f), Ground=new Color(0.18f,0.18f,0.22f), Accent=new Color(0.2f,0.9f,0.9f) },
            new MapTheme { Name="Autumn Forest",       Sky=new Color(0.75f,0.72f,0.58f), Fog=new Color(0.78f,0.70f,0.55f), Ground=new Color(0.48f,0.35f,0.18f), Accent=new Color(0.85f,0.45f,0.15f) },
            new MapTheme { Name="Snowy Ice Crossing",  Sky=new Color(0.82f,0.90f,0.96f), Fog=new Color(0.86f,0.92f,0.98f), Ground=new Color(0.85f,0.90f,0.95f), Accent=new Color(0.55f,0.75f,0.95f) },
            new MapTheme { Name="Volcanic Ashlands",   Sky=new Color(0.35f,0.18f,0.15f), Fog=new Color(0.40f,0.22f,0.18f), Ground=new Color(0.25f,0.20f,0.18f), Accent=new Color(1f,0.35f,0.1f) },
            new MapTheme { Name="Jungle Swamp",        Sky=new Color(0.45f,0.65f,0.45f), Fog=new Color(0.40f,0.55f,0.40f), Ground=new Color(0.20f,0.35f,0.18f), Accent=new Color(0.35f,0.65f,0.25f) },
            new MapTheme { Name="Coastal Boardwalk",   Sky=new Color(0.55f,0.78f,0.95f), Fog=new Color(0.68f,0.84f,0.92f), Ground=new Color(0.76f,0.68f,0.52f), Accent=new Color(0.2f,0.55f,0.85f) },
            new MapTheme { Name="Highland Ruins",      Sky=new Color(0.62f,0.68f,0.72f), Fog=new Color(0.66f,0.70f,0.74f), Ground=new Color(0.45f,0.45f,0.40f), Accent=new Color(0.65f,0.58f,0.45f) },
            new MapTheme { Name="Final Summit Gauntlet",Sky=new Color(0.50f,0.55f,0.75f), Fog=new Color(0.55f,0.58f,0.72f), Ground=new Color(0.40f,0.42f,0.48f), Accent=new Color(0.95f,0.85f,0.35f) },
        };

        [SerializeField] private int round = 1;

        // Clear, visible round goals on safe ground (never mid-river). River is ~232-305.
        static readonly float[] RoundGoalZ =
        {
            320f,  // R1: far bank after river
            420f,  // R2
            520f,  // R3
            620f,  // R4
            720f,  // R5
            820f,  // R6
            920f,  // R7
            1020f, // R8
            1120f, // R9
            1220f  // R10: final summit
        };

        public int Round => round;
        public int MaxRounds => 10;
        public float CurrentGoalZ => RoundGoalZ[Mathf.Clamp(round - 1, 0, RoundGoalZ.Length - 1)];
        public string BannerText { get; private set; } = "";
        public float BannerTimer { get; private set; }
        public MapTheme CurrentTheme => Themes[Mathf.Clamp(round - 1, 0, Themes.Length - 1)];

        Transform _goalMarker;

        public bool HasCrocodiles => round >= 2;
        public bool HasRoadHazards => round >= 3;
        public bool HasRollingBoulders => round >= 5;
        public bool HasExpressLane => round >= 5;
        public bool HasTraps => round >= 5;
        public float ObstacleDensityMultiplier => 1f + Mathf.FloorToInt((round - 1) / 5f) * 0.12f;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public void ResetRounds()
        {
            round = 1;
            ApplyTheme();
            ShowBanner("ROUND 1: " + CurrentTheme.Name);
        }

        void Start() { UpdateGoalMarker(); }

        public void NextRound()
        {
            if (round >= MaxRounds)
            {
                var gm = GameManager.Instance;
                if (gm != null) gm.VictoryAllRounds();
                return;
            }
            round++;
            ApplyTheme();
            ShowBanner($"ROUND {round}: {CurrentTheme.Name}");
            var g = GameManager.Instance;
            if (g != null) g.OnRoundAdvanced(round);
        }

        public void ApplyTheme()
        {
            var t = CurrentTheme;
            var mainCam = FindFirstObjectByType<Camera>();
            if (mainCam != null)
            {
                mainCam.backgroundColor = t.Sky;
            }
            RenderSettings.fog = true;
            RenderSettings.fogColor = t.Fog;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.ambientLight = Color.Lerp(t.Sky, Color.white, 0.35f);
            ApplyGroundTheme(t);
            UpdateGoalMarker();
        }

        void ApplyGroundTheme(MapTheme t)
        {
            var grounds = GameObject.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < grounds.Length; i++)
            {
                var r = grounds[i];
                if (r == null) continue;
                string n = r.gameObject.name;
                bool isGround = n.Contains("Grass") || n.Contains("FarBank") || n.Contains("MownStripe") || n.Contains("SafeBasePlatform");
                if (!isGround) continue;
                var m = r.sharedMaterial;
                if (m == null) continue;
                if (m.HasProperty("_Color")) m.color = Color.Lerp(m.color, t.Ground, 0.65f);
            }
        }

        void EnsureGoalMarker()
        {
            if (_goalMarker != null) return;
            var root = new GameObject("RoundGoalMarker");
            _goalMarker = root.transform;
            _goalMarker.SetParent(transform, false);
            var mat = FrogRunner.Core.RetroAssets.Solid("MAT_Goal", new Color(0.2f, 1f, 0.35f), 0.05f, true);
            if (mat.HasProperty("_MainTex")) { mat.mainTexture = FrogRunner.Core.RetroAssets.WhiteTexture; mat.mainTextureScale = Vector2.one; }
            var postMat = FrogRunner.Core.RetroAssets.Solid("MAT_GoalPost", new Color(0.9f, 0.9f, 0.85f), 0.05f);
            if (postMat.HasProperty("_MainTex")) { postMat.mainTexture = FrogRunner.Core.RetroAssets.WhiteTexture; postMat.mainTextureScale = Vector2.one; }
            MakeMarkerPart(_goalMarker, "PostL", PrimitiveType.Cube, new Vector3(-8f, 1.8f, 0f), new Vector3(0.35f, 3.6f, 0.35f), postMat);
            MakeMarkerPart(_goalMarker, "PostR", PrimitiveType.Cube, new Vector3(8f, 1.8f, 0f), new Vector3(0.35f, 3.6f, 0.35f), postMat);
            MakeMarkerPart(_goalMarker, "Beam", PrimitiveType.Cube, new Vector3(0f, 3.7f, 0f), new Vector3(16.4f, 0.35f, 0.35f), postMat);
            MakeMarkerPart(_goalMarker, "Banner", PrimitiveType.Cube, new Vector3(0f, 3.0f, 0f), new Vector3(14f, 1.1f, 0.08f), mat);
        }

        static void MakeMarkerPart(Transform p, string n, PrimitiveType t, Vector3 lp, Vector3 ls, Material m)
        {
            var go = GameObject.CreatePrimitive(t); go.name = n;
            go.transform.SetParent(p, false); go.transform.localPosition = lp; go.transform.localScale = ls;
            var c = go.GetComponent<Collider>(); if (c != null) Destroy(c);
            var r = go.GetComponent<Renderer>(); r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void UpdateGoalMarker()
        {
            EnsureGoalMarker();
            if (_goalMarker == null) return;
            _goalMarker.position = new Vector3(0f, 0f, CurrentGoalZ);
            _goalMarker.gameObject.SetActive(true);
        }

        public void ShowBanner(string text)
        {
            BannerText = text;
            BannerTimer = 2.8f;
        }

        void Update()
        {
            if (BannerTimer > 0f) BannerTimer = Mathf.Max(0f, BannerTimer - Time.unscaledDeltaTime);
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;
            if (gm.IsSafeBaseActive) return;

            float goalZ = CurrentGoalZ;
            // Only trigger when an alive, non-eliminated player actually reaches the goal line on safe footing.
            for (int i = 0; i < gm.Players.Count; i++)
            {
                var p = gm.Players[i];
                if (p == null || !p.gameObject.activeInHierarchy || p.IsEliminatedThisRound) continue;
                if (p.transform.position.z < goalZ) continue;
                if (p.InWater) continue;
                // Require grounded/on-log or safe base area so mid-air/glide doesn't falsely trigger.
                if (!p.IsGrounded && !p.IsOnLog) continue;
                gm.EnterSafeBase(round);
                return;
            }
        }

        public void ForceSetRound(int value)
        {
            round = Mathf.Clamp(value, 1, MaxRounds);
            ApplyTheme();
            ShowBanner($"ROUND {round}: {CurrentTheme.Name}");
        }
    }
}
