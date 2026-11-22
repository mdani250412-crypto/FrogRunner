using System.Collections.Generic;
using FrogRunner.Core;
using FrogRunner.Player;
using UnityEngine;

namespace FrogRunner.Traffic
{
    /// <summary>
    /// Pooled traffic director. Five lanes, one oncoming lane, and cars that live only in the band
    /// between the frog and the graphics draw distance. Spawn gaps shrink as the runner speeds up.
    /// TUNING: LaneX/LaneDirection and the speed ranges below define the whole traffic feel.
    /// </summary>
    [DisallowMultipleComponent]
    public class TrafficSpawner : MonoBehaviour
    {
        public const int LaneCount = 5;
        public static readonly float[] LaneX = { -7.68f, -3.84f, 0f, 3.84f, 7.68f };
        static readonly int[] LaneDirection = { -1, 1, 1, 1, 1 };
        static readonly float[] LaneMinSpeed = { 11f, 7f, 8f, 9f, 10.5f }; // fast from Round 1
        static readonly float[] LaneMaxSpeed = { 17f, 11f, 12.5f, 14f, 15.5f };
        static readonly Color[] Palette =
        {
            new Color(0.86f, 0.20f, 0.16f),
            new Color(0.20f, 0.42f, 0.82f),
            new Color(0.94f, 0.76f, 0.18f),
            new Color(0.20f, 0.68f, 0.52f),
            new Color(0.62f, 0.30f, 0.74f),
            new Color(0.92f, 0.92f, 0.88f)
        };

        [SerializeField] private int poolSize = 34;
        [SerializeField] private float minGap = 24f;
        [SerializeField] private float maxGap = 42f;
        [SerializeField] private float firstSpawnOffset = 24f;

        FrogController _player;
        readonly List<Vehicle> _pool = new List<Vehicle>();
        readonly float[] _laneNextZ = new float[LaneCount];
        Material[] _paints;

        public int ActiveCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _pool.Count; i++) if (_pool[i].gameObject.activeSelf) count++;
                return count;
            }
        }

        public void Configure(FrogController player)
        {
            _player = player;
            if (_pool.Count == 0) BuildPool();
        }

        void BuildPool()
        {
            _paints = new Material[Palette.Length];
            for (int i = 0; i < Palette.Length; i++)
                _paints[i] = RetroAssets.Tinted("MAT_TrafficPaint" + i, Palette[i], 0.16f);

            for (int i = 0; i < poolSize; i++)
            {
                bool truck = i % 4 == 3;
                Material paint = _paints[i % _paints.Length];
                Vehicle vehicle = Vehicle.Create(transform, paint, truck, i * 13 + 7);
                _pool.Add(vehicle);
            }
        }

        void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing || _player == null) return;

            float spawnDistance = GraphicsSettingsManager.Instance != null
                ? GraphicsSettingsManager.Instance.TrafficSpawnDistance
                : 85f;
            float highwayLimit = FrogRunner.WorldGen.CourseBuilder.HighwayEndZ - 6f;
            int round = FrogRunner.Rounds.RoundManager.Instance != null ? FrogRunner.Rounds.RoundManager.Instance.Round : 1;
            float roundSpeedMult = 1f + (round - 1) * 0.06f; // Round 1 fast, escalates to 10

            for (int lane = 0; lane < LaneCount; lane++)
            {
                if (float.IsInfinity(_laneNextZ[lane])) _laneNextZ[lane] = _player.transform.position.z + firstSpawnOffset;
                int guard = 0;
                while (_laneNextZ[lane] < _player.transform.position.z + spawnDistance && guard < 8)
                {
                    guard++;
                    if (_laneNextZ[lane] > highwayLimit)
                    {
                        // Recycle lane spawn to behind player so traffic keeps flowing on highway segment.
                        _laneNextZ[lane] = _player.transform.position.z - Random.Range(10f, 22f);
                        continue;
                    }

                    Vehicle vehicle = GetFreeVehicle();
                    if (vehicle == null) break;

                    float progress = Mathf.Clamp01(gm.Distance / 300f);
                    float gap = Mathf.Lerp(maxGap, minGap * 0.85f, progress) + Random.Range(-3f, 4f);
                    float speed = Random.Range(LaneMinSpeed[lane], LaneMaxSpeed[lane]) * roundSpeedMult;
                    vehicle.Spawn(LaneDirection[lane], speed, LaneX[lane], _laneNextZ[lane]);
                    _laneNextZ[lane] += Mathf.Max(14f, gap);
                }
            }

            // Recycle out-of-band or forbidden-zone cars; they become available for respawn.
            for (int i = 0; i < _pool.Count; i++)
            {
                Vehicle vehicle = _pool[i];
                if (!vehicle.gameObject.activeSelf) continue;
                float dz = vehicle.transform.position.z - _player.transform.position.z;
                bool outOfBand = vehicle.Direction > 0 ? dz > spawnDistance + 55f : dz < -35f;
                bool inForbiddenZone = vehicle.transform.position.z > highwayLimit;
                if (outOfBand || inForbiddenZone) vehicle.Despawn();
            }
        }

        Vehicle GetFreeVehicle()
        {
            for (int i = 0; i < _pool.Count; i++)
                if (!_pool[i].gameObject.activeSelf) return _pool[i];
            return null;
        }

        public void ResetTraffic()
        {
            if (_player == null) return;
            if (_pool.Count == 0) BuildPool();
            ClearTraffic();
            float startZ = _player.transform.position.z;
            float spawnDistance = GraphicsSettingsManager.Instance != null
                ? GraphicsSettingsManager.Instance.TrafficSpawnDistance
                : 85f;
            int round = FrogRunner.Rounds.RoundManager.Instance != null ? FrogRunner.Rounds.RoundManager.Instance.Round : 1;
            float roundSpeedMult = 1f + (round - 1) * 0.06f;

            for (int lane = 0; lane < LaneCount; lane++)
            {
                _laneNextZ[lane] = startZ + firstSpawnOffset + Random.Range(0f, 12f);
                int guard = 0;
                while (_laneNextZ[lane] < startZ + spawnDistance && guard < 8)
                {
                    guard++;
                    Vehicle vehicle = GetFreeVehicle();
                    if (vehicle == null) break;
                    float speed = Random.Range(LaneMinSpeed[lane], LaneMaxSpeed[lane]) * roundSpeedMult;
                    vehicle.Spawn(LaneDirection[lane], speed, LaneX[lane], _laneNextZ[lane]);
                    _laneNextZ[lane] += Random.Range(minGap, maxGap);
                }
            }
        }

        public void ClearTraffic()
        {
            for (int i = 0; i < _pool.Count; i++) _pool[i].Despawn();
            for (int lane = 0; lane < LaneCount; lane++) _laneNextZ[lane] = float.PositiveInfinity;
        }
    }
}
