using FrogRunner.Core;
using FrogRunner.Player;
using UnityEngine;

namespace FrogRunner.PowerUps
{
    /// <summary>
    /// Floating collectible pickup. Uses cheap distance checks against players.
    /// </summary>
    public class PowerUp : MonoBehaviour
    {
        public PowerUpType type;
        public float bobAmplitude = 0.18f;
        public float bobSpeed = 2.4f;
        public float rotateSpeed = 90f;
        public float pickupRadius = 1.2f;

        Vector3 _basePos;
        float _seed;

        public void Initialize(PowerUpType t, Vector3 pos)
        {
            type = t;
            _basePos = pos;
            _seed = Random.value * 10f;
            transform.position = pos;
            gameObject.SetActive(true);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            _seed += dt;
            transform.position = _basePos + Vector3.up * (Mathf.Sin(_seed * bobSpeed) * bobAmplitude);
            transform.Rotate(0f, rotateSpeed * dt, 0f, Space.World);

            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;

            // Check all active players.
            var players = gm.Players;
            if (players == null) return;
            for (int i = 0; i < players.Count; i++)
            {
                FrogController p = players[i];
                if (p == null || !p.gameObject.activeInHierarchy || p.InWater) continue;
                float d = Vector3.Distance(p.transform.position + Vector3.up * 0.7f, transform.position);
                if (d <= pickupRadius)
                {
                    var inv = p.GetComponent<PowerUpInventory>();
                    if (inv != null) inv.Collect(type);
                    if (FrogRunner.Audio.AudioManager.Instance != null) FrogRunner.Audio.AudioManager.Instance.PlayPowerUp();
                    gameObject.SetActive(false);
                    return;
                }
            }
        }
    }
}
