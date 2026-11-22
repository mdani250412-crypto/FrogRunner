using FrogRunner.Core;
using UnityEngine;

namespace FrogRunner.Obstacles
{
    /// <summary>Round 3+: narrows safe lanes; collision hurts like traffic hit.</summary>
    public class ConstructionBarrier : ObstacleBase
    {
        public float hitRadius = 0.9f;
        public float hitHeight = 1.4f;

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;
            TryHitPlayer(hitRadius, hitHeight, transform.position + Vector3.up * 0.5f);
        }
    }
}
