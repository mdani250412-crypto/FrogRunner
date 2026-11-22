using FrogRunner.Core;
using FrogRunner.Player;
using FrogRunner.WorldGen;
using UnityEngine;

namespace FrogRunner.Obstacles
{
    /// <summary>Round 2+: patrols river lanes, snaps/submerges periodically. Hurts frog on contact.</summary>
    public class Crocodile : ObstacleBase
    {
        public float speed = 1.8f;
        public float patrolHalfWidth = 8f;
        public int direction = 1;
        public float snapInterval = 2.8f;
        public float snapDuration = 0.9f;
        public float baseY = -0.45f;

        float _timer;
        bool _submerged;
        Vector3 _baseScale;
        bool _visualBuilt;

        void Awake() { _baseScale = transform.localScale; }

        public override void OnSpawned()
        {
            EnsureVisual();
            _timer = Random.Range(0f, snapInterval);
            _submerged = false;
            ApplyPose();
        }

        void EnsureVisual()
        {
            if (_visualBuilt) return;
            _visualBuilt = true;
            // Replace smooth capsule look with blocky low-poly/pixel croc parts.
            var mr = GetComponent<MeshRenderer>(); if (mr != null) mr.enabled = false;
            var mf = GetComponent<MeshFilter>(); if (mf != null) mf.sharedMesh = null;

            var skin = RetroPixelMat(new Color(0.22f, 0.45f, 0.18f));
            var belly = RetroPixelMat(new Color(0.45f, 0.58f, 0.28f));
            var eye = RetroPixelMat(new Color(0.9f, 0.2f, 0.1f));
            Part("Body", PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(1.0f, 0.35f, 2.2f), skin);
            Part("Snout", PrimitiveType.Cube, new Vector3(0f, -0.02f, 1.25f), new Vector3(0.7f, 0.22f, 0.8f), skin);
            Part("Jaw", PrimitiveType.Cube, new Vector3(0f, -0.12f, 1.1f), new Vector3(0.65f, 0.08f, 0.7f), belly);
            Part("Tail", PrimitiveType.Cube, new Vector3(0f, 0.02f, -1.35f), new Vector3(0.45f, 0.18f, 0.9f), skin);
            Part("EyeL", PrimitiveType.Cube, new Vector3(-0.22f, 0.2f, 0.75f), new Vector3(0.12f, 0.08f, 0.12f), eye);
            Part("EyeR", PrimitiveType.Cube, new Vector3(0.22f, 0.2f, 0.75f), new Vector3(0.12f, 0.08f, 0.12f), eye);
            Part("Ridge1", PrimitiveType.Cube, new Vector3(0f, 0.22f, -0.2f), new Vector3(0.18f, 0.1f, 0.18f), skin);
            Part("Ridge2", PrimitiveType.Cube, new Vector3(0f, 0.22f, 0.2f), new Vector3(0.18f, 0.1f, 0.18f), skin);
        }

        void Part(string n, PrimitiveType t, Vector3 lp, Vector3 ls, Material m)
        {
            var go = GameObject.CreatePrimitive(t);
            go.name = n; go.transform.SetParent(transform, false);
            go.transform.localPosition = lp; go.transform.localScale = ls;
            var c = go.GetComponent<Collider>(); if (c != null) Destroy(c);
            go.GetComponent<Renderer>().sharedMaterial = m;
        }

        static Material RetroPixelMat(Color c)
        {
            var sh = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            var m = new Material(sh) { enableInstancing = true };
            if (m.HasProperty("_Color")) m.color = c;
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.04f);
            var tex = FrogRunner.Core.RetroAssets.WhiteTexture;
            if (tex != null && m.HasProperty("_MainTex")) { m.mainTexture = tex; m.mainTextureScale = Vector2.one; }
            return m;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;

            Vector3 pos = transform.position;
            pos.x += direction * speed * dt;
            if (Mathf.Abs(pos.x) > patrolHalfWidth) { pos.x = Mathf.Sign(pos.x) * patrolHalfWidth; direction *= -1; }
            transform.position = pos;

            _timer += dt;
            if (!_submerged && _timer >= snapInterval)
            {
                _submerged = true;
                _timer = 0f;
                ApplyPose();
            }
            else if (_submerged && _timer >= snapDuration)
            {
                _submerged = false;
                _timer = 0f;
                ApplyPose();
            }

            // Active hazard: bites damage frog (shield blocks via normal damage path).
            if (!_submerged)
            {
                TryHitPlayer(1.1f, 1.4f, transform.position + Vector3.up * 0.5f, "CROC BITE! -1 heart");
            }
        }

        void ApplyPose()
        {
            var p = transform.position;
            p.y = _submerged ? baseY - 0.55f : baseY;
            transform.position = p;
            transform.localScale = _submerged ? new Vector3(_baseScale.x, _baseScale.y * 0.45f, _baseScale.z) : _baseScale;
        }
    }
}
