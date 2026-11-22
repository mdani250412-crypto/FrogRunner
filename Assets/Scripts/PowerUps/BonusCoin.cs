using FrogRunner.Core;
using FrogRunner.Player;
using UnityEngine;

namespace FrogRunner.PowerUps
{
    /// <summary>
    /// Bonus coin/collectible pulled by magnet power-up and awarding score.
    /// </summary>
    public class BonusCoin : MonoBehaviour
    {
        public int scoreValue = 50;
        public float bobAmplitude = 0.12f;
        public float bobSpeed = 3.2f;
        public float rotateSpeed = 140f;
        public float pickupRadius = 1.1f;
        public float magnetPullSpeed = 12f;

        Vector3 _basePos;
        float _seed;
        bool _magnetized;
        Transform _magnetTarget;

        public void Initialize(Vector3 pos)
        {
            _basePos = pos;
            _seed = Random.value * 10f;
            _magnetized = false;
            _magnetTarget = null;
            transform.position = pos;
            gameObject.SetActive(true);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;

            FrogController nearest = null;
            float nearestDist = float.MaxValue;
            var players = gm.Players;
            if (players != null)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    var p = players[i];
                    if (p == null || !p.gameObject.activeInHierarchy || p.InWater) continue;
                    float d = Vector3.Distance(p.transform.position + Vector3.up * 0.7f, transform.position);
                    if (d < nearestDist) { nearestDist = d; nearest = p; }
                    var inv = p.GetComponent<PowerUpInventory>();
                    if (inv != null && inv.MagnetActive && d <= inv.magnetRadius)
                    {
                        _magnetized = true;
                        _magnetTarget = p.transform;
                    }
                }
            }

            if (_magnetized && _magnetTarget != null)
            {
                Vector3 target = _magnetTarget.position + Vector3.up * 0.8f;
                transform.position = Vector3.MoveTowards(transform.position, target, magnetPullSpeed * dt);
            }
            else
            {
                _seed += dt;
                transform.position = _basePos + Vector3.up * (Mathf.Sin(_seed * bobSpeed) * bobAmplitude);
            }
            transform.Rotate(0f, rotateSpeed * dt, 0f, Space.World);

            if (nearest != null)
            {
                float d = Vector3.Distance(nearest.transform.position + Vector3.up * 0.7f, transform.position);
                if (d <= pickupRadius)
                {
                    int mult = 1;
                    var inv = nearest.GetComponent<PowerUpInventory>();
                    if (inv != null) mult = inv.CurrentScoreMultiplier;
                    gm.AddBonusScore(scoreValue * mult, nearest);
                    var bank = CoinBank.Instance;
                    if (bank != null) bank.Add(1);
                    if (FrogRunner.Audio.AudioManager.Instance != null) FrogRunner.Audio.AudioManager.Instance.PlayPowerUp();
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
