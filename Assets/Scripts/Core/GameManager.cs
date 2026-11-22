using System;
using System.Collections;
using System.Collections.Generic;
using FrogRunner.Player;
using FrogRunner.Networking;
using FrogRunner.Traffic;
using FrogRunner.WorldGen;
using UnityEngine;

namespace FrogRunner.Core
{
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        SafeBase,
        RoundLeaderboard,
        BoostShop,
        GameOver,
        Victory
    }

    /// <summary>
    /// Owns the runner loop: menu -> play -> splash respawn -> safe-base round end -> win or game over.
    /// Score is distance * 10 plus near-miss bonuses. Lives are lost to traffic hits and deep water.
    /// TUNING: MaxLives, score multiplier, round goals and respawn delay live here.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Run tuning")]
        [SerializeField] private int maxLives = 3;
        [SerializeField] private float scorePerMeter = 10f;
        [SerializeField] private int nearMissBonus = 25;
        [SerializeField] private int victoryBonus = 5000;
        [SerializeField] private float respawnDelay = 0.7f;
        [SerializeField] private float damageCooldownTime = 1.35f;

        public GameState State { get; private set; } = GameState.Menu;
        public FrogController Player { get; private set; } // primary/local player
        public TrafficSpawner Traffic { get; private set; }
        public readonly List<FrogController> Players = new List<FrogController>();
        public float Distance { get; private set; }
        public int Score { get; private set; }
        public int BestDistance { get; private set; }
        public int Lives { get; private set; } // legacy aggregate for HUD fallback
        public int MaxLives => maxLives;
        public bool IsLocalPlayerDead => _localHealth != null && _localHealth.IsDead;
        PlayerHealth _localHealth;
        public bool IsPlaying => State == GameState.Playing;
        public bool IsSafeBaseActive => State == GameState.SafeBase || State == GameState.RoundLeaderboard || State == GameState.BoostShop;
        // Cutscene lock removed with volcano/lava flow.
        public float SafeBaseTimer { get; private set; }
        public const float SafeBaseDuration = 60f;
        public Vector3 SafeBasePosition { get; private set; }
        public int SafeBaseRound { get; private set; }
        public bool AllPlayersDown { get; private set; }
        public int CoinsThisRun { get; private set; }
        public string ZoneName { get; private set; } = "Highway";
        public string ToastText { get; private set; } = "";
        public float ToastRemaining { get; private set; }

        // Multiplayer round finish / leaderboard
        public readonly Dictionary<int, float> PlayerFinishTimes = new Dictionary<int, float>(); // PlayerIndex -> time
        public readonly Dictionary<int, int> PlayerRoundPoints = new Dictionary<int, int>();
        public readonly List<int> RoundLeaderboardOrder = new List<int>();
        public float LeaderboardTimer { get; private set; }
        public bool BoostPurchasedThisRound { get; private set; }
        public FrogRunner.Rounds.BoostCardOffer[] BoostOffers { get; private set; } = Array.Empty<FrogRunner.Rounds.BoostCardOffer>();

        Vector3 runStart = new Vector3(0f, 0.15f, 4f);
        float finishZ = 500f;
        int bonusScore;
        readonly Dictionary<FrogController, float> _damageCooldowns = new Dictionary<FrogController, float>();
        readonly List<FrogController> _tmpPlayers = new List<FrogController>();
        bool respawning;
        Coroutine respawnRoutine;

        const string BestKey = "FrogRunner_BestDistance";

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            Lives = maxLives;
            BestDistance = PlayerPrefs.GetInt(BestKey, 0);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Configure(FrogController player, TrafficSpawner traffic, Vector3 start, float finish)
        {
            Players.Clear();
            if (player != null) Players.Add(player);
            Player = player;
            Traffic = traffic;
            runStart = start;
            finishZ = finish;
            EnsurePlayerHealth(Player);
            for (int i = 0; i < Players.Count; i++)
            {
                if (Players[i] == null) continue;
                EnsurePlayerHealth(Players[i]);
                Players[i].RespawnAt(runStart);
                Players[i].SetControlEnabled(false);
            }
            if (Traffic != null) Traffic.ClearTraffic();
        }

        void EnsurePlayerHealth(FrogController p)
        {
            if (p == null) return;
            var h = p.GetComponent<PlayerHealth>();
            if (h == null) h = p.gameObject.AddComponent<PlayerHealth>();
            if (p == Player) _localHealth = h;
        }

        public void RegisterPlayer(FrogController player)
        {
            if (player == null || Players.Contains(player)) return;
            Players.Add(player);
            if (Player == null) Player = player;
        }

        public float MaxPlayerZ()
        {
            float z = runStart.z;
            for (int i = 0; i < Players.Count; i++)
            {
                var p = Players[i];
                if (p != null && p.gameObject.activeInHierarchy) z = Mathf.Max(z, p.transform.position.z);
            }
            return z;
        }

        void Update()
        {
            if (ToastRemaining > 0f) ToastRemaining = Mathf.Max(0f, ToastRemaining - Time.unscaledDeltaTime);
            TickDamageCooldowns();

            bool pausePressed = UnityEngine.Input.GetKeyDown(KeyCode.Escape) || UnityEngine.Input.GetKeyDown(KeyCode.P) || UnityEngine.Input.GetButtonDown("Cancel");
            if (pausePressed)
            {
                if (State == GameState.Playing || State == GameState.SafeBase) Pause();
                else if (State == GameState.Paused) Resume();
            }

            if (State == GameState.RoundLeaderboard)
            {
                LeaderboardTimer -= Time.unscaledDeltaTime;
                if (LeaderboardTimer <= 0f) OpenBoostShop();
                return;
            }

            if (State == GameState.SafeBase)
            {
                SafeBaseTimer -= Time.unscaledDeltaTime;
                if (SafeBaseTimer <= 0f)
                {
                    SafeBaseTimer = 0f;
                    ContinueFromSafeBase();
                }
                return;
            }

            if (State == GameState.BoostShop || State == GameState.GameOver || State == GameState.Victory || State == GameState.Menu) return;
            if (State != GameState.Playing || Player == null) return;

            UpdateProgress();
            CheckAllPlayersDown();
        }

        void TickDamageCooldowns()
        {
            if (_damageCooldowns.Count == 0) return;
            _tmpPlayers.Clear();
            _tmpPlayers.AddRange(_damageCooldowns.Keys);
            for (int i = 0; i < _tmpPlayers.Count; i++)
            {
                var p = _tmpPlayers[i];
                if (p == null) { _damageCooldowns.Remove(p); continue; }
                _damageCooldowns[p] = Mathf.Max(0f, _damageCooldowns[p] - Time.deltaTime);
            }
        }

        void UpdateProgress()
        {
            if (Player == null) return;
            float z = MaxPlayerZ();
            Distance = Mathf.Max(Distance, z - runStart.z);
            Score = Mathf.FloorToInt(Distance * scorePerMeter) + bonusScore;

            if (z < CourseBuilder.HighwayEndZ) ZoneName = "Highway";
            else if (z < CourseBuilder.GrassEndZ) ZoneName = "Grass Bank";
            else if (z < CourseBuilder.RiverEndZ) ZoneName = "River Crossing";
            else ZoneName = "Far Bank";

            var rounds = FrogRunner.Rounds.RoundManager.Instance;
            if (rounds == null && z >= finishZ)
            {
                bonusScore += victoryBonus;
                Score = Mathf.FloorToInt(Distance * scorePerMeter) + bonusScore;
                Victory();
                return;
            }

            for (int i = 0; i < Players.Count; i++)
            {
                var p = Players[i];
                if (p == null) continue;
                if (p.transform.position.y < -5f && !p.InWater) PlayerFellInWater(p.transform.position, p);
            }
        }

        public void OnRoundAdvanced(int round)
        {
            ShowToast($"ROUND {round} - New hazards!");
        }

        public void BeginNetworkedStart()
        {
            if (State == GameState.Menu || State == GameState.GameOver || State == GameState.Victory) StartRun();
        }

        public void SetCutsceneLock(bool locked)
        {
            if (Player != null) Player.SetControlEnabled(!locked && State == GameState.Playing);
        }

        public void EnterSafeBase(int round)
        {
            if (State != GameState.Playing) return;
            SafeBaseRound = round;
            SafeBaseTimer = SafeBaseDuration;
            Time.timeScale = 1f;
            var net = FrogRunner.Networking.LanNetworkManager.Instance;
            if (net != null) net.ResetVotes();

            float goalZ = FrogRunner.Rounds.RoundManager.Instance != null ? FrogRunner.Rounds.RoundManager.Instance.CurrentGoalZ : 350f;
            Vector3 basePos = new Vector3(0f, 0.5f, goalZ + 4f);
            SafeBasePosition = basePos;
            for (int i = 0; i < Players.Count; i++)
            {
                var p = Players[i];
                if (p == null || !p.gameObject.activeInHierarchy) continue;
                p.SetControlEnabled(false);
                p.RespawnAt(basePos + new Vector3((i - (Players.Count - 1) * 0.5f) * 1.5f, 0f, 0f));
            }

            bool isMultiplayer = Players.Count > 1 || (net != null && net.InRoom);
            if (isMultiplayer)
            {
                BuildRoundLeaderboard();
                State = GameState.RoundLeaderboard;
                LeaderboardTimer = 4.5f;
                ShowToast($"ROUND {round} COMPLETE! Leaderboard");
            }
            else
            {
                PrepareBoostOffers();
                State = GameState.SafeBase;
                ShowToast($"ROUND {round} COMPLETE! Safe base - 60s");
            }
        }

        public void ContinueFromSafeBase()
        {
            if (State != GameState.SafeBase) return;
            var rounds = FrogRunner.Rounds.RoundManager.Instance;
            State = GameState.Playing;
            Time.timeScale = 1f;
            rounds?.NextRound();
            ResetRoundTransientState();
            if (Player != null) Player.SetControlEnabled(true);
        }

        public void OpenBoostShop()
        {
            if (State != GameState.RoundLeaderboard && State != GameState.SafeBase) return;
            PrepareBoostOffers();
            State = GameState.BoostShop;
            Time.timeScale = 0f;
        }

        public void ContinueAfterBoostShop()
        {
            if (State != GameState.BoostShop) return;
            Time.timeScale = 1f;
            State = GameState.SafeBase;
            SafeBaseTimer = SafeBaseDuration; // still give safe base window after shop in solo; in mp can continue
            ShowToast("Boost selected. Continue when ready.");
        }

        public void EndGameFromSafeBase()
        {
            if (State != GameState.SafeBase && State != GameState.BoostShop) return;
            Time.timeScale = 1f;
            Victory();
        }

        public void VictoryAllRounds()
        {
            Time.timeScale = 1f;
            Victory();
        }

        public void SaveCurrentGame(int slotIndex)
        {
            var save = FrogRunner.SaveSystem.SaveSystemManager.Instance;
            if (save == null) { ShowToast("Save system missing"); return; }
            var h = Player != null ? Player.GetComponent<PlayerHealth>() : null;
            var rounds = FrogRunner.Rounds.RoundManager.Instance;
            var profile = save.Profile;
            save.CreateOrOverwriteSlot(slotIndex, new FrogRunner.SaveSystem.SaveSlotData
            {
                hasData = true,
                playerName = profile != null ? profile.playerName : "Frog",
                round = rounds != null ? rounds.Round : 1,
                distance = Distance,
                score = Score,
                hearts = h != null ? h.Hearts : Lives,
                playerX = Player != null ? Player.transform.position.x : runStart.x,
                playerY = Player != null ? Player.transform.position.y : runStart.y,
                playerZ = Player != null ? Player.transform.position.z : runStart.z
            });
            ShowToast("Game saved (slot " + (slotIndex + 1) + ")");
        }

        public bool LoadGameFromSlot(int slotIndex)
        {
            var save = FrogRunner.SaveSystem.SaveSystemManager.Instance;
            if (save == null) return false;
            var slot = save.GetSlot(slotIndex);
            if (slot == null || !slot.hasData) { ShowToast("Empty save slot"); return false; }
            StartRun();
            var rounds = FrogRunner.Rounds.RoundManager.Instance;
            if (rounds != null) rounds.ForceSetRound(slot.round);
            Distance = slot.distance;
            Score = slot.score;
            if (Player != null)
            {
                Player.RespawnAt(new Vector3(slot.playerX, slot.playerY, slot.playerZ));
                var h = Player.GetComponent<PlayerHealth>();
                if (h != null) { h.ResetHealth(); int dmg = Mathf.Max(0, h.MaxHearts - slot.hearts); for (int i = 0; i < dmg; i++) h.TakeDamage(1); Lives = h.Hearts; }
            }
            ShowToast("Loaded slot " + (slotIndex + 1));
            return true;
        }

        // Checkpoints removed for continuous 10-round racing.

        public void StartRun()
        {
            StopRespawnRoutine();
            Time.timeScale = 1f;
            State = GameState.Playing;
            Lives = maxLives;
            bonusScore = 0;
            Distance = 0f;
            Score = 0;
            CoinsThisRun = 0;
            _damageCooldowns.Clear();
            respawning = false;
            SafeBasePosition = runStart;
            SafeBaseTimer = 0f;
            ZoneName = "Highway";
            AllPlayersDown = false;
            PlayerFinishTimes.Clear();
            PlayerRoundPoints.Clear();
            RoundLeaderboardOrder.Clear();
            BoostPurchasedThisRound = false;
            BoostOffers = Array.Empty<FrogRunner.Rounds.BoostCardOffer>();

            var rounds = FrogRunner.Rounds.RoundManager.Instance;
            if (rounds != null) rounds.ResetRounds();

            for (int i = 0; i < Players.Count; i++)
            {
                var p = Players[i];
                if (p == null) continue;
                p.gameObject.SetActive(true);
                EnsurePlayerHealth(p);
                var h = p.GetComponent<PlayerHealth>(); if (h != null) h.ResetHealth();
                if (p == Player) Lives = h != null ? h.Hearts : maxLives;
                var cos = p.GetComponent<PlayerCosmetics>();
                if (cos != null && FrogRunner.SaveSystem.SaveSystemManager.Instance != null)
                    cos.ApplyFromProfile(FrogRunner.SaveSystem.SaveSystemManager.Instance.Profile);
                cos?.Reapply();
                p.RespawnAt(runStart);
                p.SetControlEnabled(true);
                p.BeginRun();
                var inv = p.GetComponent<FrogRunner.PowerUps.PowerUpInventory>();
                if (inv != null) inv.ResetAll();
            }
            if (Traffic != null) Traffic.ResetTraffic();
            var spawner = UnityEngine.Object.FindAnyObjectByType<FrogRunner.PowerUps.PowerUpSpawner>();
            if (spawner != null) spawner.ResetSpawns(runStart.z);
            var obstacles = UnityEngine.Object.FindAnyObjectByType<FrogRunner.Obstacles.ObstacleSpawner>();
            if (obstacles != null) obstacles.ResetSpawns(runStart.z);
            var police = UnityEngine.Object.FindAnyObjectByType<FrogRunner.Police.AnimalPoliceChaser>();
            if (police != null) police.ResetChase(runStart.z);

            ShowToast("GO! Round 1 - Race to the finish!");
        }

        GameState _stateBeforePause = GameState.Playing;

        public void Pause()
        {
            if (State != GameState.Playing && State != GameState.SafeBase) return;
            _stateBeforePause = State;
            State = GameState.Paused;
            Time.timeScale = 0f;
            if (Player != null) Player.SetControlEnabled(false);
            MobileInput.Clear();
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            State = _stateBeforePause == GameState.SafeBase ? GameState.SafeBase : GameState.Playing;
            Time.timeScale = 1f;
            if (Player != null && State == GameState.Playing)
            {
                Player.SetControlEnabled(true);
                Player.SetInvulnerable(0.5f);
            }
        }

        public void RestartRun()
        {
            StartRun();
        }

        public void ReturnToMenu()
        {
            StopRespawnRoutine();
            Time.timeScale = 1f;
            State = GameState.Menu;
            Lives = maxLives;
            Distance = 0f;
            Score = 0;
            bonusScore = 0;
            respawning = false;
            ZoneName = "Highway";
            ToastRemaining = 0f;
            for (int i = 0; i < Players.Count; i++)
            {
                var p = Players[i];
                if (p == null) continue;
                p.gameObject.SetActive(true);
                p.RespawnAt(runStart);
                p.SetControlEnabled(false);
                var h = p.GetComponent<PlayerHealth>(); if (h != null) h.ResetHealth();
                var inv = p.GetComponent<FrogRunner.PowerUps.PowerUpInventory>();
                if (inv != null) inv.ResetAll();
            }
            if (Traffic != null) Traffic.ClearTraffic();
            var spawner = UnityEngine.Object.FindAnyObjectByType<FrogRunner.PowerUps.PowerUpSpawner>();
            if (spawner != null) spawner.DespawnAll();
            var obstacles = UnityEngine.Object.FindAnyObjectByType<FrogRunner.Obstacles.ObstacleSpawner>();
            if (obstacles != null) obstacles.DespawnAll();
        }

        public void OnTrafficHit(Vector3 sourcePosition, FrogController victim = null)
        {
            FrogController target = victim != null ? victim : Player;
            if (State != GameState.Playing || target == null || target.IsInvulnerable || target.IsEliminatedThisRound) return;
            float cd = 0f;
            _damageCooldowns.TryGetValue(target, out cd);
            if (cd > 0f) return;

            var inv = target.GetComponent<FrogRunner.PowerUps.PowerUpInventory>();
            if (inv != null && inv.ConsumeShieldHit(sourcePosition))
            {
                ShowToast("FORCE FIELD BLOCK!");
                return;
            }

            _damageCooldowns[target] = damageCooldownTime;
            target.ApplyHit(sourcePosition);
            ApplyDamage(target, "Ouch! -1 heart");
        }

        public void OnNearMiss(Vector3 sourcePosition, FrogController player = null)
        {
            if (State != GameState.Playing) return;
            int mult = 1;
            FrogController p = player != null ? player : Player;
            var inv = p != null ? p.GetComponent<FrogRunner.PowerUps.PowerUpInventory>() : null;
            if (inv != null) mult = inv.CurrentScoreMultiplier;
            bonusScore += nearMissBonus * mult;
            Score = Mathf.FloorToInt(Distance * scorePerMeter) + bonusScore;
            ShowToast($"Near miss +{nearMissBonus * mult}");
        }

        public void AddBonusScore(int amount, FrogController player = null)
        {
            if (State != GameState.Playing) return;
            bonusScore += amount;
            Score = Mathf.FloorToInt(Distance * scorePerMeter) + bonusScore;
            ShowToast($"+{amount}");
        }

        public void AddCoin(int amount = 1)
        {
            CoinsThisRun += Mathf.Max(0, amount);
            var bank = CoinBank.Instance;
            if (bank != null) bank.Add(amount);
        }

        public void PlayerFellInWater(Vector3 worldPosition, FrogController victim = null)
        {
            FrogController target = victim != null ? victim : Player;
            if (State != GameState.Playing) return;
            float waterY = CourseBuilder.Instance != null ? CourseBuilder.Instance.WaterLevel : -0.8f;
            SplashFX.Play(new Vector3(worldPosition.x, waterY, worldPosition.z));
            if (FrogRunner.Audio.AudioManager.Instance != null) FrogRunner.Audio.AudioManager.Instance.PlaySplash();
            if (target != null)
            {
                target.EnterWaterDeath();
                target.SetInvulnerable(3f);
            }
            ApplyDamage(target, "SPLASH! -1 heart", true, HazardType.Water);
        }

        enum HazardType { Generic, Water }

        void ApplyDamage(FrogController target, string msg, bool doRespawn = false, HazardType hazard = HazardType.Generic)
        {
            if (State == GameState.SafeBase || State == GameState.BoostShop || State == GameState.RoundLeaderboard) return;
            if (target != null && target.IsEliminatedThisRound) return;

            var h = target != null ? target.GetComponent<PlayerHealth>() : _localHealth;
            if (h == null) { LoseLife(); if (Lives <= 0) GameOver(); return; }
            h.TakeDamage(1);
            if (target == Player) Lives = h.Hearts;

            if (h.IsDead)
            {
                bool solo = Players.Count <= 1;
                if (solo && target == Player)
                {
                    ShowToast("You wiped out!");
                    GameOver();
                }
                else
                {
                    target.MarkEliminatedThisRound(msg);
                    ShowToast(target == Player ? "Down! Wait for round end." : "A player is down!");
                    target.gameObject.SetActive(false);
                    CheckAllPlayersDown();
                }
                return;
            }

            if (target == Player)
            {
                ShowToast(msg);
                if (doRespawn && hazard == HazardType.Water)
                {
                    respawning = true;
                    respawnRoutine = StartCoroutine(RespawnAfterSplash());
                }
            }
        }

        IEnumerator RespawnAfterSplash()
        {
            yield return new WaitForSeconds(respawnDelay);
            if (State != GameState.Playing)
            {
                respawning = false;
                respawnRoutine = null;
                yield break;
            }
            if (Player != null && !Player.IsEliminatedThisRound)
            {
                    Vector3 respawn = new Vector3(0f, 0.2f, Mathf.Max(runStart.z, Player.transform.position.z - 6f));
                Player.RespawnAt(respawn);
                Player.SetControlEnabled(true);
            }
            respawning = false;
            respawnRoutine = null;
            ShowToast("Back on dry land.");
        }

        void CheckAllPlayersDown()
        {
            if (State != GameState.Playing) return;
            int active = 0;
            int alive = 0;
            for (int i = 0; i < Players.Count; i++)
            {
                var p = Players[i];
                if (p == null) continue;
                active++;
                if (p.gameObject.activeInHierarchy && !p.IsEliminatedThisRound) alive++;
            }
            if (active > 0 && alive == 0)
            {
                AllPlayersDown = true;
                State = GameState.GameOver; // UI shows RESTART/LEAVE
                Time.timeScale = 0f;
                for (int i = 0; i < Players.Count; i++) if (Players[i] != null) Players[i].SetControlEnabled(false);
                ShowToast("All players down! Restart or Leave.");
            }
        }

        void ResetRoundTransientState()
        {
            AllPlayersDown = false;
            PlayerFinishTimes.Clear();
            RoundLeaderboardOrder.Clear();
            BoostPurchasedThisRound = false;
            respawning = false;
            for (int i = 0; i < Players.Count; i++)
            {
                var p = Players[i];
                if (p == null) continue;
                p.ClearRoundElimination();
                p.gameObject.SetActive(true);
                var h = p.GetComponent<PlayerHealth>();
                if (h != null) h.ResetHealth();
                var cos = p.GetComponent<PlayerCosmetics>();
                if (cos != null && FrogRunner.SaveSystem.SaveSystemManager.Instance != null)
                    cos.ApplyFromProfile(FrogRunner.SaveSystem.SaveSystemManager.Instance.Profile);
                cos?.Reapply();
                p.RespawnAt(SafeBasePosition + new Vector3((i - (Players.Count - 1) * 0.5f) * 1.5f, 0f, -2.5f));
                p.SetControlEnabled(true);
            }
            var police = UnityEngine.Object.FindAnyObjectByType<FrogRunner.Police.AnimalPoliceChaser>();
            if (police != null) police.ResetChase(SafeBasePosition.z - 8f);
        }

        public void BuildRoundLeaderboard()
        {
            RoundLeaderboardOrder.Clear();
            PlayerRoundPoints.Clear();
            // For now: local player first, others by distance; multiplayer network sync can override.
            var list = new List<FrogController>(Players);
            list.Sort((a, b) => (b != null ? b.transform.position.z : 0f).CompareTo(a != null ? a.transform.position.z : 0f));
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                if (p == null) continue;
                RoundLeaderboardOrder.Add(p.PlayerIndex);
                int pts = i == 0 ? 10 : i == 1 ? 5 : i == 2 ? 3 : 1;
                PlayerRoundPoints[p.PlayerIndex] = pts;
            }
        }

        public void PrepareBoostOffers()
        {
            if (BoostOffers != null && BoostOffers.Length == 3) return;
            var rng = new System.Random(UnityEngine.Random.Range(0, int.MaxValue));
            BoostOffers = FrogRunner.Rounds.BoostCardOffer.CreateThree(rng);
            BoostPurchasedThisRound = false;
        }

        public bool BuyBoostOffer(int index)
        {
            if (BoostPurchasedThisRound) { ShowToast("Only one boost per round end."); return false; }
            if (BoostOffers == null || index < 0 || index >= BoostOffers.Length) return false;
            var offer = BoostOffers[index];
            var bank = CoinBank.Instance;
            if (bank == null || !bank.Spend(offer.cost)) { ShowToast("Not enough coins."); return false; }
            offer.Apply(Player);
            BoostPurchasedThisRound = true;
            ShowToast("Bought " + offer.title);
            return true;
        }

        void LoseLife()
        {
            Lives = Mathf.Max(0, Lives - 1);
            if (Lives < 0) Lives = 0;
        }

        void GameOver()
        {
            StopRespawnRoutine();
            State = GameState.GameOver;
            Time.timeScale = 0f;
            if (Player != null) Player.SetControlEnabled(false);
            SaveBest();
        }

        void Victory()
        {
            State = GameState.Victory;
            Time.timeScale = 0f;
            if (Player != null) Player.SetControlEnabled(false);
            SaveBest();
        }

        public void ShowToast(string text)
        {
            ToastText = text;
            ToastRemaining = 2.2f;
        }

        void SaveBest()
        {
            int meters = Mathf.FloorToInt(Distance);
            if (meters > BestDistance)
            {
                BestDistance = meters;
                PlayerPrefs.SetInt(BestKey, BestDistance);
                PlayerPrefs.Save();
            }
        }

        void StopRespawnRoutine()
        {
            if (respawnRoutine != null)
            {
                StopCoroutine(respawnRoutine);
                respawnRoutine = null;
            }
            respawning = false;
        }

        void OnApplicationQuit()
        {
            SaveBest();
        }
    }
}
