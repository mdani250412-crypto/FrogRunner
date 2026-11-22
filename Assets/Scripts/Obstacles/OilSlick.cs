using FrogRunner.Core;
using FrogRunner.Player;
using UnityEngine;

namespace FrogRunner.Obstacles
{
    /// <summary>Round 3+: causes temporary slide/loss of control.</summary>
    public class OilSlick : ObstacleBase
    {
        public float slideDuration = 1.1f;
        public float radiusX = 1.2f;
        public float radiusZ = 1.2f;

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;
            var players = gm.Players;
            if (players == null) return;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p == null || !p.gameObject.activeInHierarchy || !p.IsGrounded || p.IsInvulnerable) continue;
                Vector3 d = p.transform.position - transform.position;
                if (Mathf.Abs(d.x) <= radiusX && Mathf.Abs(d.z) <= radiusZ)
                {
                    p.ApplySlide(slideDuration);
                }
            }
        }
    }
}
