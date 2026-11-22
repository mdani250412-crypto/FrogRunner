using UnityEngine;

namespace FrogRunner.Multiplayer
{
    /// <summary>
    /// Lightweight per-player state for local/online-ready multiplayer.
    /// Network layer can mirror/sync these fields without touching gameplay.
    /// </summary>
    [System.Serializable]
    public class PlayerState
    {
        public int playerIndex;
        public string displayName = "Player";
        public float distance;
        public int score;
        public int lives;
        public bool finished;
        public Color accentColor = Color.white;
    }
}
