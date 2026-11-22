using FrogRunner.Core;
using FrogRunner.Player;
using FrogRunner.Rounds;
using UnityEngine;

namespace FrogRunner.Police
{
    /// <summary>
    /// Lightweight animal police pursuer: blocky pixel-art cop that chases the player from behind.
    /// Applies damage/knockback on contact (force field blocks). Recycles if stuck/out of range.
    /// </summary>
    [DisallowMultipleComponent]
    public class AnimalPoliceChaser : MonoBehaviour
    {
        public static AnimalPoliceChaser Instance { get; private set; }

        [SerializeField] float baseSpeed = 9.5f;
        [SerializeField] float catchDistance = 1.3f;
        [SerializeField] float maxBehindDistance = 45f;
        [SerializeField] float maxAheadDistance = 8f;
        [SerializeField] float lateralLerp = 3.5f;

        float _stuckTimer;
        Vector3 _lastPos;
        float _hitCooldown;

        void Awake()
        {
            Instance = this;
            BuildVisual();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void ResetChase(float playerZ)
        {
            transform.position = new Vector3(0f, 0f, playerZ - 12f);
            _stuckTimer = 0f;
            _hitCooldown = 0f;
            _lastPos = transform.position;
            gameObject.SetActive(true);
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing || gm.Player == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            _hitCooldown = Mathf.Max(0f, _hitCooldown - dt);

            var target = gm.Player;
            if (target == null || target.IsEliminatedThisRound) return;

            int round = RoundManager.Instance != null ? RoundManager.Instance.Round : 1;
            float roundMult = 1f + (round - 1) * 0.03f; // fair escalation
            float speed = baseSpeed * roundMult;
            // Slight rubber-banding: if far behind, speed up a bit; if close, ease off.
            float dzToTarget = target.transform.position.z - transform.position.z;
            if (dzToTarget > 20f) speed *= 1.25f;
            else if (dzToTarget < 6f) speed *= 0.85f;

            Vector3 pos = transform.position;
            Vector3 targetPos = target.transform.position;

            // Pursue along course; clamp to playable area.
            float step = speed * dt;
            pos.z = Mathf.MoveTowards(pos.z, targetPos.z - 1.2f, step);
            pos.x = Mathf.Lerp(pos.x, Mathf.Clamp(targetPos.x, -10f, 10f), dt * lateralLerp);
            pos.y = 0f;
            transform.position = pos;

            Vector3 look = targetPos - pos; look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look.normalized), dt * 6f);

            // Recycle if too far behind or too far ahead of action.
            float dz = targetPos.z - pos.z;
            if (dz > maxBehindDistance || dz < -maxAheadDistance)
            {
                RecycleNear(targetPos.z);
                return;
            }

            // Stuck detection
            if ((pos - _lastPos).sqrMagnitude < 0.0005f) _stuckTimer += dt;
            else _stuckTimer = 0f;
            _lastPos = pos;
            if (_stuckTimer > 2.5f)
            {
                RecycleNear(targetPos.z);
                return;
            }

            // Contact damage/knockback (force field handled by GameManager damage path)
            float dist = Vector3.Distance(new Vector3(pos.x, 0, pos.z), new Vector3(targetPos.x, 0, targetPos.z));
            if (dist < catchDistance && _hitCooldown <= 0f)
            {
                _hitCooldown = 1.2f;
                gm.OnTrafficHit(transform.position + Vector3.up * 0.5f, target);
                gm.ShowToast("BUSTED! Animal Police hit!");
                // small pushback/recycle to keep it fair
                pos.z = targetPos.z - 10f;
                transform.position = pos;
            }
        }

        void RecycleNear(float targetZ)
        {
            transform.position = new Vector3(Random.Range(-4f, 4f), 0f, targetZ - 14f);
            _stuckTimer = 0f;
        }

        void BuildVisual()
        {
            // Blocky low-poly retro cop with point-filtered materials.
            Material navy = PixelMat(new Color(0.12f, 0.16f, 0.35f));
            Material skin = PixelMat(new Color(0.55f, 0.38f, 0.25f)); // animal brown
            Material badge = PixelMat(new Color(1f, 0.85f, 0.2f));
            Material red = PixelMat(new Color(1f, 0.15f, 0.15f), true);
            Material blue = PixelMat(new Color(0.2f, 0.45f, 1f), true);
            Material dark = PixelMat(new Color(0.08f, 0.08f, 0.1f));

            Part("Body", PrimitiveType.Cube, new Vector3(0f, 0.75f, 0f), new Vector3(0.9f, 1.0f, 0.5f), navy);
            Part("Head", PrimitiveType.Cube, new Vector3(0f, 1.45f, 0.05f), new Vector3(0.6f, 0.55f, 0.55f), skin);
            Part("Snout", PrimitiveType.Cube, new Vector3(0f, 1.32f, 0.35f), new Vector3(0.35f, 0.2f, 0.25f), skin);
            Part("EarL", PrimitiveType.Cube, new Vector3(-0.22f, 1.78f, 0f), new Vector3(0.12f, 0.2f, 0.12f), skin);
            Part("EarR", PrimitiveType.Cube, new Vector3(0.22f, 1.78f, 0f), new Vector3(0.12f, 0.2f, 0.12f), skin);
            Part("Hat", PrimitiveType.Cube, new Vector3(0f, 1.78f, 0.02f), new Vector3(0.72f, 0.18f, 0.72f), navy);
            Part("HatBrim", PrimitiveType.Cube, new Vector3(0f, 1.68f, 0.18f), new Vector3(0.75f, 0.06f, 0.5f), navy);
            Part("Badge", PrimitiveType.Cube, new Vector3(0f, 0.95f, 0.27f), new Vector3(0.18f, 0.18f, 0.03f), badge);
            Part("LegL", PrimitiveType.Cube, new Vector3(-0.2f, 0.25f, 0f), new Vector3(0.22f, 0.5f, 0.22f), dark);
            Part("LegR", PrimitiveType.Cube, new Vector3(0.2f, 0.25f, 0f), new Vector3(0.22f, 0.5f, 0.22f), dark);
            Part("ArmL", PrimitiveType.Cube, new Vector3(-0.55f, 0.75f, 0.1f), new Vector3(0.18f, 0.55f, 0.18f), navy);
            Part("ArmR", PrimitiveType.Cube, new Vector3(0.55f, 0.75f, 0.1f), new Vector3(0.18f, 0.55f, 0.18f), navy);

            // Simple siren bar
            var siren = new GameObject("Siren").transform;
            siren.SetParent(transform, false);
            siren.localPosition = new Vector3(0f, 1.95f, 0f);
            var r = PartOn(siren, "Red", PrimitiveType.Cube, new Vector3(-0.12f, 0f, 0f), new Vector3(0.22f, 0.12f, 0.12f), red);
            var b = PartOn(siren, "Blue", PrimitiveType.Cube, new Vector3(0.12f, 0f, 0f), new Vector3(0.22f, 0.12f, 0.12f), blue);
            var flasher = siren.gameObject.AddComponent<SirenFlasher>();
            flasher.red = r; flasher.blue = b;
        }

        static Material PixelMat(Color c, bool emissive = false)
        {
            var sh = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            var m = new Material(sh) { enableInstancing = true };
            if (m.HasProperty("_Color")) m.color = c;
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.05f);
            var tex = RetroAssets.WhiteTexture;
            if (tex != null && m.HasProperty("_MainTex")) { m.mainTexture = tex; m.mainTextureScale = Vector2.one; }
            if (emissive && m.HasProperty("_EmissionColor")) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 1.8f); }
            return m;
        }

        void Part(string n, PrimitiveType t, Vector3 lp, Vector3 ls, Material m) => PartOn(transform, n, t, lp, ls, m);
        static Renderer PartOn(Transform p, string n, PrimitiveType t, Vector3 lp, Vector3 ls, Material m)
        {
            var go = GameObject.CreatePrimitive(t); go.name = n;
            go.transform.SetParent(p, false); go.transform.localPosition = lp; go.transform.localScale = ls;
            var c = go.GetComponent<Collider>(); if (c != null) Destroy(c);
            var r = go.GetComponent<Renderer>(); r.sharedMaterial = m; return r;
        }
    }

    /// <summary>Cheap alternating siren flash, no lights.</summary>
    public class SirenFlasher : MonoBehaviour
    {
        public Renderer red, blue;
        void Update()
        {
            bool on = Mathf.FloorToInt(Time.time * 6f) % 2 == 0;
            SetEmissive(red, on);
            SetEmissive(blue, !on);
        }
        static void SetEmissive(Renderer r, bool on)
        {
            if (r == null) return;
            var m = r.sharedMaterial;
            if (m != null && m.HasProperty("_EmissionColor"))
                m.SetColor("_EmissionColor", (m.HasProperty("_Color") ? m.color : Color.white) * (on ? 2.2f : 0.15f));
        }
    }
}