using FrogRunner.Core;
using UnityEngine;

namespace FrogRunner.Obstacles
{
    /// <summary>Round 5+: rolls across grass/highway edges; fair speed, big warning.</summary>
    public class RollingBoulder : ObstacleBase
    {
        public float speed = 5.5f;
        public int direction = 1;
        public float maxX = 13f;
        public float radius = 1.0f;

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;

            transform.position += Vector3.right * (direction * speed * dt);
            transform.Rotate(Vector3.forward, -direction * speed * 90f * dt, Space.World);
            if (Mathf.Abs(transform.position.x) > maxX) direction *= -1;

            TryHitPlayer(radius, 1.6f, transform.position, "BOULDER!");
        }
    }
}
