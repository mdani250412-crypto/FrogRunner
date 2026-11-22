using System.Collections.Generic;
using FrogRunner.Core;
using FrogRunner.WorldGen;
using UnityEngine;

namespace FrogRunner.PowerUps
{
    /// <summary>
    /// Spawns power-ups and bonus coins ahead of players. Uses simple pooling.
    /// TUNING: spawn intervals/distances control frequency.
    /// </summary>
    [DisallowMultipleComponent]
    public class PowerUpSpawner : MonoBehaviour
    {
        [SerializeField] private int poolPerType = 4;
        [SerializeField] private int coinPoolSize = 24;
        [SerializeField] private float spawnAheadMax = 65f;
        [SerializeField] private float laneXRange = 7f;

        readonly List<PowerUp> _powerUps = new List<PowerUp>();
        readonly List<BonusCoin> _coins = new List<BonusCoin>();
        float _nextPowerUpZ;
        float _nextCoinZ;
        bool _initialized;

        Material _shieldMat, _speedMat, _magnetMat, _coinMat;

        void EnsurePool()
        {
            if (_initialized) return;
            _initialized = true;
            _shieldMat = RetroAssets.Solid("MAT_PU_Shield", new Color(0.2f, 0.85f, 1f), 0.4f, true);
            _speedMat = RetroAssets.Solid("MAT_PU_Speed", new Color(1f, 0.75f, 0.15f), 0.3f, true);
            _magnetMat = RetroAssets.Solid("MAT_PU_Magnet", new Color(0.9f, 0.3f, 1f), 0.3f, true);
            _coinMat = RetroAssets.Solid("MAT_PU_Coin", new Color(1f, 0.88f, 0.25f), 0.5f, true);

            CreatePool(PowerUpType.ForceField, _shieldMat);
            CreatePool(PowerUpType.SpeedBoost, _speedMat);
            CreatePool(PowerUpType.ScoreMagnet, _magnetMat);

            for (int i = 0; i < coinPoolSize; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "BonusCoin";
                go.transform.SetParent(transform, false);
                go.transform.localScale = new Vector3(0.55f, 0.05f, 0.55f);
                go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
                go.GetComponent<MeshRenderer>().sharedMaterial = _coinMat;
                var coin = go.AddComponent<BonusCoin>();
                go.SetActive(false);
                _coins.Add(coin);
            }
        }

        void CreatePool(PowerUpType type, Material mat)
        {
            for (int i = 0; i < poolPerType; i++)
            {
                PrimitiveType prim = type == PowerUpType.ForceField ? PrimitiveType.Sphere : PrimitiveType.Capsule;
                var go = GameObject.CreatePrimitive(prim);
                go.name = "PowerUp_" + type;
                go.transform.SetParent(transform, false);
                go.transform.localScale = type == PowerUpType.ForceField ? Vector3.one * 0.8f : new Vector3(0.55f, 0.45f, 0.55f);
                var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
                go.GetComponent<MeshRenderer>().sharedMaterial = mat;
                var pu = go.AddComponent<PowerUp>();
                pu.type = type;
                go.SetActive(false);
                _powerUps.Add(pu);
            }
        }

        public void ResetSpawns(float playerZ)
        {
            EnsurePool();
            DespawnAll();
            _nextPowerUpZ = playerZ + 35f;
            _nextCoinZ = playerZ + 18f;
        }

        public void DespawnAll()
        {
            EnsurePool();
            for (int i = 0; i < _powerUps.Count; i++) _powerUps[i].gameObject.SetActive(false);
            for (int i = 0; i < _coins.Count; i++) _coins[i].gameObject.SetActive(false);
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;
            EnsurePool();
            float playerZ = gm.MaxPlayerZ();

            while (_nextPowerUpZ < playerZ + spawnAheadMax)
            {
                SpawnPowerUp(_nextPowerUpZ);
                _nextPowerUpZ += Random.Range(38f, 62f);
            }
            while (_nextCoinZ < playerZ + spawnAheadMax)
            {
                SpawnCoin(_nextCoinZ);
                _nextCoinZ += Random.Range(10f, 20f);
            }

            RecycleBehind(playerZ - 20f);
        }

        void SpawnPowerUp(float z)
        {
            if (z > CourseBuilder.FinishZ - 10f) return;
            if (z > CourseBuilder.RiverStartZ - 8f && z < CourseBuilder.RiverEndZ + 8f) z = CourseBuilder.RiverEndZ + 10f;
            PowerUpType type = (PowerUpType)Random.Range(0, 3);
            for (int i = 0; i < _powerUps.Count; i++)
            {
                var p = _powerUps[i];
                if (p.gameObject.activeSelf || p.type != type) continue;
                float x = Random.Range(-laneXRange, laneXRange);
                p.Initialize(type, new Vector3(x, 1.0f, z));
                return;
            }
        }

        void SpawnCoin(float z)
        {
            if (z > CourseBuilder.FinishZ - 10f) return;
            if (z > CourseBuilder.RiverStartZ - 5f && z < CourseBuilder.RiverEndZ + 5f) return;
            for (int i = 0; i < _coins.Count; i++)
            {
                var c = _coins[i];
                if (c.gameObject.activeSelf) continue;
                float x = Random.Range(-laneXRange, laneXRange);
                c.Initialize(new Vector3(x, 0.9f, z));
                return;
            }
        }

        void RecycleBehind(float z)
        {
            for (int i = 0; i < _powerUps.Count; i++) if (_powerUps[i].transform.position.z < z) _powerUps[i].gameObject.SetActive(false);
            for (int i = 0; i < _coins.Count; i++) if (_coins[i].transform.position.z < z) _coins[i].gameObject.SetActive(false);
        }
    }
}
