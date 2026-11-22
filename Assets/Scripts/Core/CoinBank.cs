using UnityEngine;

namespace FrogRunner.Core
{
    /// <summary>Tracks player coins for end-of-round boost card purchases. Lightweight, persisted in PlayerPrefs.</summary>
    [DisallowMultipleComponent]
    public class CoinBank : MonoBehaviour
    {
        public static CoinBank Instance { get; private set; }
        const string Key = "FrogRunner_Coins";
        public int Coins { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Coins = PlayerPrefs.GetInt(Key, 0);
        }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            Coins += amount;
            PlayerPrefs.SetInt(Key, Coins);
        }

        public bool Spend(int amount)
        {
            if (amount <= 0) return true;
            if (Coins < amount) return false;
            Coins -= amount;
            PlayerPrefs.SetInt(Key, Coins);
            return true;
        }
    }
}