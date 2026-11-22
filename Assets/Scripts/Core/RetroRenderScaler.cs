using UnityEngine;

namespace FrogRunner.Core
{
    /// <summary>
    /// True low-resolution rendering for the PS1/CRT crunch. The gameplay camera renders into a
    /// point-filtered RenderTexture, which this component paints full-screen through IMGUI before
    /// the HUD. That keeps 3D cost down (0.5x on low-end phones) while the UI stays crisp.
    /// TUNING: 0.5 is very crunchy, 0.75 is balanced, 1.0 renders directly to the screen.
    /// </summary>
    [DefaultExecutionOrder(-2000)]
    [RequireComponent(typeof(Camera))]
    public class RetroRenderScaler : MonoBehaviour
    {
        [Range(0.35f, 1f)]
        public float renderScale = 0.75f;

        public static RetroRenderScaler Current { get; private set; }
        public RenderTexture CurrentRT => _lowRes;

        Camera _camera;
        RenderTexture _lowRes;
        int _lastWidth;
        int _lastHeight;

        void Awake()
        {
            _camera = GetComponent<Camera>();
            if (Current == null) Current = this;
        }

        void OnEnable()
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            SyncTarget();
        }

        void OnDisable()
        {
            if (_camera != null) _camera.targetTexture = null;
            ReleaseTarget();
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
            if (_camera != null) _camera.targetTexture = null;
            ReleaseTarget();
        }

        public void SetScale(float scale)
        {
            renderScale = Mathf.Clamp(scale, 0.35f, 1f);
            SyncTarget();
        }

        void Update()
        {
            SyncTarget();
        }

        void SyncTarget()
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            if (_camera == null) return;

            if (renderScale >= 0.999f)
            {
                if (_camera.targetTexture != null) _camera.targetTexture = null;
                ReleaseTarget();
                return;
            }

            int width = Mathf.Max(160, Mathf.RoundToInt(Screen.width * renderScale));
            int height = Mathf.Max(90, Mathf.RoundToInt(Screen.height * renderScale));
            if (width == _lastWidth && height == _lastHeight && _lowRes != null)
            {
                if (_camera.targetTexture != _lowRes) _camera.targetTexture = _lowRes;
                return;
            }

            ReleaseTarget();
            _lastWidth = width;
            _lastHeight = height;
            _lowRes = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = "FrogRunner_LowRes",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false,
                hideFlags = HideFlags.HideAndDontSave
            };
            _lowRes.Create();
            _camera.targetTexture = _lowRes;
        }

        // Intentionally no OnGUI blit: blitting here covers IMGUI UI drawn by other scripts.
        // The low-res render is presented by RetroUI before it draws the HUD/menus.

        void ReleaseTarget()
        {
            _lastWidth = 0;
            _lastHeight = 0;
            if (_lowRes == null) return;
            if (_camera != null && _camera.targetTexture == _lowRes) _camera.targetTexture = null;
            _lowRes.Release();
            if (Application.isPlaying) Destroy(_lowRes);
            else DestroyImmediate(_lowRes);
            _lowRes = null;
        }
    }
}
