using UnityEngine;

namespace FrogRunner.Player
{
    /// <summary>
    /// Per-player health. Never shared between players.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private int maxHearts = 3;
        public int MaxHearts => maxHearts;
        public int Hearts { get; private set; }
        public bool IsDead => Hearts <= 0;

        public System.Action<PlayerHealth> OnChanged;

        void Awake()
        {
            Hearts = Mathf.Max(1, maxHearts);
        }

        public void ResetHealth()
        {
            Hearts = Mathf.Max(1, maxHearts);
            OnChanged?.Invoke(this);
        }

        public void TakeDamage(int amount = 1)
        {
            if (IsDead) return;
            Hearts = Mathf.Max(0, Hearts - amount);
            OnChanged?.Invoke(this);
        }

        public void Heal(int amount = 1)
        {
            if (IsDead) return;
            Hearts = Mathf.Clamp(Hearts + amount, 0, maxHearts);
            OnChanged?.Invoke(this);
        }

        public void Kill()
        {
            if (IsDead) return;
            Hearts = 0;
            OnChanged?.Invoke(this);
        }
    }
}
