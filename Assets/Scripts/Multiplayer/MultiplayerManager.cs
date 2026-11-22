using System.Collections.Generic;
using FrogRunner.Core;
using FrogRunner.Input;
using FrogRunner.Player;
using UnityEngine;

namespace FrogRunner.Multiplayer
{
    public enum MultiplayerMode
    {
        Single = 0,
        LocalCoopRace = 1
    }

    /// <summary>
    /// Local split/shared-screen multiplayer manager. Designed so a future Netcode layer
    /// can drive PlayerState/InputSource for remote players without rewriting gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    public class MultiplayerManager : MonoBehaviour
    {
        public static MultiplayerManager Instance { get; private set; }

        [SerializeField] private MultiplayerMode mode = MultiplayerMode.Single;
        public MultiplayerMode Mode => mode;
        public readonly List<PlayerState> States = new List<PlayerState>();
        public readonly List<FrogController> Players = new List<FrogController>();

        public bool IsMultiplayer => mode != MultiplayerMode.Single && Players.Count > 1;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void ConfigureSingle(FrogController p1)
        {
            mode = MultiplayerMode.Single;
            Players.Clear(); States.Clear();
            AddPlayer(p1, 0, "P1", new Color(0.35f, 1f, 0.45f), new KeyboardPlayerInput { useMobileFallback = true });
            ApplyCameraLayout();
        }

        // Split-screen removed: every player uses their own full-screen device in LAN mode.
        public void ConfigureLocalTwoPlayer(FrogController p1, FrogController p2)
        {
            ConfigureSingle(p1);
            if (p2 != null) p2.gameObject.SetActive(false);
        }

        void AddPlayer(FrogController p, int index, string name, Color color, IPlayerInput input)
        {
            if (p == null) return;
            p.PlayerIndex = index;
            p.InputSource = input;
            var inv = p.GetComponent<FrogRunner.PowerUps.PowerUpInventory>();
            if (inv == null) p.gameObject.AddComponent<FrogRunner.PowerUps.PowerUpInventory>();
            Players.Add(p);
            States.Add(new PlayerState { playerIndex = index, displayName = name, accentColor = color, lives = 3 });
            GameManager.Instance?.RegisterPlayer(p);
        }

        public void ApplyCameraLayout()
        {
            // Full-screen only. Split-screen removed.
            var p2 = GameObject.Find("P2Camera");
            if (p2 != null) Destroy(p2);
            var cam = FindFirstObjectByType<Camera>();
            if (cam == null) return;
            cam.rect = new Rect(0, 0, 1, 1);
            var chase = cam.GetComponent<ChaseCamera>();
            if (chase != null && Players.Count > 0)
            {
                chase.target = Players[0].transform;
                chase.controller = Players[0];
            }
        }

        public void RefreshStates()
        {
            for (int i = 0; i < Players.Count && i < States.Count; i++)
            {
                var p = Players[i];
                var s = States[i];
                if (p == null) continue;
                s.distance = Mathf.Max(s.distance, p.transform.position.z);
                var gm = GameManager.Instance;
                s.score = gm != null ? gm.Score : s.score;
                s.lives = gm != null ? gm.Lives : s.lives;
            }
        }

        public PlayerState GetWinner()
        {
            RefreshStates();
            PlayerState best = null;
            for (int i = 0; i < States.Count; i++)
            {
                if (best == null || States[i].distance > best.distance || (Mathf.Approximately(States[i].distance, best.distance) && States[i].score > best.score))
                    best = States[i];
            }
            return best;
        }
    }
}
