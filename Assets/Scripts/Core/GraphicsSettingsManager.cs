using UnityEngine;

namespace FrogRunner.Core
{
    /// <summary>
    /// Central performance/visual settings, persisted in PlayerPrefs and exposed to the pause menu.
    /// Simple Low/Medium/High presets control render scale, shadows and draw distance together for
    /// smooth FPS on lower-spec laptops. FPS cap and touch controls are independent toggles.
    /// TUNING: adjust the preset arrays below to rebalance quality vs performance.
    /// </summary>
    [DisallowMultipleComponent]
    public class GraphicsSettingsManager : MonoBehaviour
    {
        public static GraphicsSettingsManager Instance { get; private set; }

        public static readonly string[] PresetLabels = { "Low", "Medium", "High" };
        // Low: max performance. Medium: balanced. High: sharpest/longest view.
        static readonly float[] PresetRenderScale = { 0.5f, 0.75f, 1f };
        static readonly int[] PresetShadowIndex = { 0, 1, 2 }; // 0=Off,1=Low,2=Medium
        static readonly float[] PresetDrawDistance = { 110f, 180f, 280f };
        static readonly float[] PresetTrafficSpawnDistance = { 55f, 85f, 120f };
        static readonly float[] PresetFogDensity = { 0.009f, 0.005f, 0.0028f };

        public static readonly string[] ShadowLabels = { "Off", "Low", "Medium" };
        public static readonly string[] FpsLabels = { "30 FPS", "60 FPS", "Unlimited" };

        [SerializeField] private int presetIndex = 1;
        [SerializeField] private int fpsIndex = 1;
        [SerializeField] private bool touchControlsEnabled;

        public int PresetIndex => presetIndex;
        public string PresetLabel => PresetLabels[presetIndex];
        public int FpsIndex => fpsIndex;
        public bool TouchControlsEnabled => touchControlsEnabled;
        public string ShadowLabel => ShadowLabels[PresetShadowIndex[presetIndex]];
        public string FpsLabel => FpsLabels[fpsIndex];
        public float RenderScale => PresetRenderScale[presetIndex];
        public float DrawDistance => PresetDrawDistance[presetIndex];
        public float TrafficSpawnDistance => PresetTrafficSpawnDistance[presetIndex];

        void Awake()
        {
            Instance = this;
            Load();
            if (Application.isMobilePlatform || UnityEngine.Input.touchSupported) touchControlsEnabled = true;
            if (Application.isMobilePlatform) presetIndex = 0; // default Low on mobile
            ApplyAll();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void CyclePreset(int direction)
        {
            presetIndex = Wrap(presetIndex + direction, PresetLabels.Length);
            ApplyAll();
            Save();
        }

        public void CycleFps(int direction)
        {
            fpsIndex = Wrap(fpsIndex + direction, FpsLabels.Length);
            ApplyAll();
            Save();
        }

        public void ToggleTouchControls()
        {
            touchControlsEnabled = !touchControlsEnabled;
            ApplyAll();
            Save();
        }

        public void ApplyAll()
        {
            int shadowIndex = PresetShadowIndex[presetIndex];
            QualitySettings.antiAliasing = 0;
            QualitySettings.vSyncCount = 0;
            QualitySettings.shadows = shadowIndex == 0 ? ShadowQuality.Disable : shadowIndex == 1 ? ShadowQuality.HardOnly : ShadowQuality.All;
            QualitySettings.shadowDistance = shadowIndex == 0 ? 0f : shadowIndex == 1 ? 25f : 55f;
            QualitySettings.pixelLightCount = shadowIndex == 0 ? 0 : 1;

            switch (fpsIndex)
            {
                case 0: Application.targetFrameRate = 30; break;
                case 1: Application.targetFrameRate = 60; break;
                default: Application.targetFrameRate = -1; break;
            }

            // Fog + far clip sell the draw distance, keeping traffic and trees in budget.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = PresetFogDensity[presetIndex];

            Camera cam = FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                cam.farClipPlane = DrawDistance;
                var scaler = cam.GetComponent<RetroRenderScaler>();
                if (scaler != null) scaler.SetScale(RenderScale);
            }
        }

        void Load()
        {
            // Migrate old individual keys to a single preset where possible.
            if (PlayerPrefs.HasKey("FR_GfxPreset"))
            {
                presetIndex = Mathf.Clamp(PlayerPrefs.GetInt("FR_GfxPreset", presetIndex), 0, PresetLabels.Length - 1);
            }
            else
            {
                int oldDraw = PlayerPrefs.GetInt("FR_DrawDistance", presetIndex);
                presetIndex = Mathf.Clamp(oldDraw, 0, PresetLabels.Length - 1);
            }
            fpsIndex = Mathf.Clamp(PlayerPrefs.GetInt("FR_Fps", fpsIndex), 0, FpsLabels.Length - 1);
            touchControlsEnabled = PlayerPrefs.GetInt("FR_Touch", touchControlsEnabled ? 1 : 0) == 1;
        }

        void Save()
        {
            PlayerPrefs.SetInt("FR_GfxPreset", presetIndex);
            PlayerPrefs.SetInt("FR_Fps", fpsIndex);
            PlayerPrefs.SetInt("FR_Touch", touchControlsEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        static int Wrap(int value, int length)
        {
            if (length <= 0) return 0;
            return ((value % length) + length) % length;
        }
    }
}
