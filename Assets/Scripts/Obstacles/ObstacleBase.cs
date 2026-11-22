using FrogRunner.Core;
using FrogRunner.Player;
using UnityEngine;

namespace FrogRunner.Obstacles
{
    /// <summary>Base for round obstacles. Uses simple proximity checks to stay cheap.</summary>
    public abstract class ObstacleBase : MonoBehaviour
    {
        public virtual void OnSpawned() { }
        public virtual void OnDespawned() { }

        protected bool TryHitPlayer(float radius, float yMax, Vector3 center, string toast = null)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return false;
            var players = gm.Players;
            if (players == null) return false;
            bool hitAny = false;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p == null || !p.gameObject.activeInHierarchy || p.InWater || p.IsInvulnerable) continue;
                Vector3 pp = p.transform.position + Vector3.up * 0.7f;
                float dx = Mathf.Abs(pp.x - center.x);
                float dz = Mathf.Abs(pp.z - center.z);
                if (dx <= radius && dz <= radius && pp.y <= yMax)
                {
                    gm.OnTrafficHit(center, p);
                    if (!string.IsNullOrEmpty(toast)) gm.ShowToast(toast);
                    hitAny = true;
                }
            }
            return hitAny;
        }
    }
}
