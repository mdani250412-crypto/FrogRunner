using FrogRunner.Core;
using UnityEngine;

namespace FrogRunner.Obstacles
{
    /// <summary>Round 5+: periodic pop-up bollard/trap.</summary>
    public class PopUpTrap : ObstacleBase
    {
        public float upInterval = 2.2f;
        public float upDuration = 1.0f;
        public float hitRadius = 0.7f;

        float _timer;
        bool _up;
        Vector3 _basePos;

        void Awake() { _basePos = transform.localPosition; }

        public override void OnSpawned()
        {
            _timer = Random.Range(0f, upInterval);
            _up = false;
            ApplyPose();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;
            _timer += dt;
            if (!_up && _timer >= upInterval) { _up = true; _timer = 0f; ApplyPose(); }
            else if (_up && _timer >= upDuration) { _up = false; _timer = 0f; ApplyPose(); }
            if (_up) TryHitPlayer(hitRadius, 1.2f, transform.position + Vector3.up * 0.4f, "TRAP!");
        }

        void ApplyPose()
        {
            var p = _basePos;
            p.y = _up ? 0.35f : -0.45f;
            transform.localPosition = p;
        }
    }
}
