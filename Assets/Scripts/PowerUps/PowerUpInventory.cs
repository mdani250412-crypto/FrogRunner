using FrogRunner.Core;
using FrogRunner.Player;
using UnityEngine;

namespace FrogRunner.PowerUps
{
    /// <summary>
    /// Per-player power-up state. Handles timed effects and shield visuals.
    /// </summary>
    [DisallowMultipleComponent]
    public class PowerUpInventory : MonoBehaviour
    {
        public float shieldDuration = 9f;
        public float speedBoostDuration = 7f;
        public float speedMultiplier = 1.45f;
        public float jumpMultiplier = 1.25f;
        public float magnetDuration = 8f;
        public float magnetRadius = 8f;
        public int magnetScoreMultiplier = 2;

        public bool ShieldActive => _shieldTimer > 0f;
        public float ShieldRemaining => _shieldTimer;
        public bool SpeedActive => _speedTimer > 0f;
        public float SpeedRemaining => _speedTimer;
        public bool MagnetActive => _magnetTimer > 0f;
        public float MagnetRemaining => _magnetTimer;
        public float CurrentSpeedMultiplier => SpeedActive ? speedMultiplier : 1f;
        public float CurrentJumpMultiplier => SpeedActive ? jumpMultiplier : 1f;
        public int CurrentScoreMultiplier => MagnetActive ? magnetScoreMultiplier : 1;

        FrogController _player;
        float _shieldTimer;
        float _speedTimer;
        float _magnetTimer;
        GameObject _shieldVisual;
        Material _shieldMat;

        void Awake()
        {
            _player = GetComponent<FrogController>();
            BuildShieldVisual();
        }

        void BuildShieldVisual()
        {
            _shieldVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _shieldVisual.name = "ForceField";
            _shieldVisual.transform.SetParent(transform, false);
            _shieldVisual.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            _shieldVisual.transform.localScale = Vector3.one * 1.9f;
            var col = _shieldVisual.GetComponent<Collider>();
            if (col != null) Destroy(col);
            var shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            _shieldMat = new Material(shader);
            _shieldMat.name = "MAT_ForceField";
            if (_shieldMat.HasProperty("_Color")) _shieldMat.color = new Color(0.25f, 0.85f, 1f, 0.22f);
            if (_shieldMat.HasProperty("_Glossiness")) _shieldMat.SetFloat("_Glossiness", 0.8f);
            if (_shieldMat.HasProperty("_Metallic")) _shieldMat.SetFloat("_Metallic", 0f);
            if (_shieldMat.HasProperty("_Mode")) _shieldMat.SetFloat("_Mode", 3f); // transparent
            if (_shieldMat.HasProperty("_SrcBlend")) _shieldMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (_shieldMat.HasProperty("_DstBlend")) _shieldMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (_shieldMat.HasProperty("_ZWrite")) _shieldMat.SetInt("_ZWrite", 0);
            _shieldMat.DisableKeyword("_ALPHATEST_ON");
            _shieldMat.EnableKeyword("_ALPHABLEND_ON");
            _shieldMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            _shieldMat.renderQueue = 3000;
            if (_shieldMat.HasProperty("_EmissionColor"))
            {
                _shieldMat.EnableKeyword("_EMISSION");
                _shieldMat.SetColor("_EmissionColor", new Color(0.15f, 0.7f, 1f) * 0.8f);
            }
            var mr = _shieldVisual.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _shieldMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _shieldVisual.SetActive(false);
        }

        public void Collect(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.ForceField:
                    _shieldTimer = shieldDuration;
                    if (_shieldVisual != null) _shieldVisual.SetActive(true);
                    break;
                case PowerUpType.SpeedBoost:
                    _speedTimer = speedBoostDuration;
                    break;
                case PowerUpType.ScoreMagnet:
                    _magnetTimer = magnetDuration;
                    break;
            }
        }

        public bool ConsumeShieldHit(Vector3 hitSource)
        {
            if (!ShieldActive) return false;
            _shieldTimer = 0f;
            if (_shieldVisual != null) _shieldVisual.SetActive(false);
            if (_player != null)
            {
                _player.SetInvulnerable(0.6f);
                // Safe knock-aside, no life loss.
                Vector3 away = _player.transform.position - hitSource;
                away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = Vector3.right;
                _player.ApplySafeBounce(away.normalized * 4.5f + Vector3.up * 3.5f);
            }
            if (FrogRunner.Audio.AudioManager.Instance != null) FrogRunner.Audio.AudioManager.Instance.PlayShieldBlock();
            return true;
        }

        public void ResetAll()
        {
            _shieldTimer = _speedTimer = _magnetTimer = 0f;
            if (_shieldVisual != null) _shieldVisual.SetActive(false);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (_shieldTimer > 0f)
            {
                _shieldTimer = Mathf.Max(0f, _shieldTimer - dt);
                if (_shieldVisual != null)
                {
                    _shieldVisual.SetActive(true);
                    float pulse = 0.85f + Mathf.Sin(Time.time * 6f) * 0.08f;
                    _shieldVisual.transform.localScale = Vector3.one * (1.9f * pulse);
                    if (_shieldMat != null && _shieldMat.HasProperty("_Color"))
                    {
                        float alpha = _shieldTimer < 1.5f ? Mathf.PingPong(Time.time * 8f, 0.3f) + 0.05f : 0.22f;
                        var c = _shieldMat.color; c.a = alpha; _shieldMat.color = c;
                    }
                }
                if (_shieldTimer <= 0f && _shieldVisual != null) _shieldVisual.SetActive(false);
            }
            if (_speedTimer > 0f) _speedTimer = Mathf.Max(0f, _speedTimer - dt);
            if (_magnetTimer > 0f) _magnetTimer = Mathf.Max(0f, _magnetTimer - dt);
        }
    }
}
