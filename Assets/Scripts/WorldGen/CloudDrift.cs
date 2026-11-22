using FrogRunner.Core;
using UnityEngine;

namespace FrogRunner.WorldGen
{
    /// <summary>
    /// Recycles a cloud cluster around the runner so the sky never ends. There is no gameplay value:
    /// it just sells forward motion at the horizon.
    /// TUNING: wrapDistance and driftSpeed.
    /// </summary>
    public class CloudDrift : MonoBehaviour
    {
        public float driftSpeed = 0.9f;
        public float wrapBehind = 90f;
        public float wrapAhead = 330f;

        Transform _player;

        void Update()
        {
            if (_player == null)
            {
                GameManager gm = GameManager.Instance;
                if (gm != null && gm.Player != null) _player = gm.Player.transform;
            }

            float playerZ = _player != null ? _player.transform.position.z : 0f;
            Vector3 p = transform.localPosition;
            p.x -= driftSpeed * Time.deltaTime;
            p.z += driftSpeed * 0.08f * Time.deltaTime;
            if (p.x < -90f) p.x += 180f;
            if (p.z < playerZ - wrapBehind) p.z += wrapAhead + wrapBehind;
            transform.localPosition = p;
        }
    }
}
