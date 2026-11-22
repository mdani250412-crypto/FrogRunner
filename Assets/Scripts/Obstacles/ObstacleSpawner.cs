using System.Collections.Generic;
using FrogRunner.Core;
using FrogRunner.Rounds;
using FrogRunner.WorldGen;
using UnityEngine;

namespace FrogRunner.Obstacles
{
    /// <summary>
    /// Spawns round-scaled obstacles ahead of players. Difficulty comes from variety/density, not speed.
    /// </summary>
    [DisallowMultipleComponent]
    public class ObstacleSpawner : MonoBehaviour
    {
        [SerializeField] float spawnAhead = 65f;
        [SerializeField] float recycleBehind = 25f;

        readonly List<GameObject> _active = new List<GameObject>();
        float _nextCrocZ, _nextHazardZ, _nextBoulderZ, _nextTrapZ;
        bool _init;
        Material _croMat, _oilMat, _coneMat, _boulderMat, _trapMat;

        void EnsureMaterials()
        {
            if (_init) return; _init = true;
            _croMat = RetroAssets.Solid("MAT_Croc", new Color(0.22f, 0.45f, 0.18f), 0.04f);
            if (_croMat.HasProperty("_MainTex")) { _croMat.mainTexture = RetroAssets.WhiteTexture; _croMat.mainTextureScale = Vector2.one; }
            _oilMat = RetroAssets.Solid("MAT_Oil", new Color(0.08f, 0.08f, 0.1f), 0.7f);
            _coneMat = RetroAssets.Solid("MAT_Cone", new Color(1f, 0.45f, 0.1f), 0.15f);
            _boulderMat = RetroAssets.Solid("MAT_Boulder", new Color(0.45f, 0.42f, 0.4f), 0.04f);
            _trapMat = RetroAssets.Solid("MAT_Trap", new Color(0.55f, 0.15f, 0.15f), 0.2f);
        }

        public void ResetSpawns(float playerZ)
        {
            EnsureMaterials();
            DespawnAll();
            _nextCrocZ = playerZ + 40f;
            _nextHazardZ = playerZ + 30f;
            _nextBoulderZ = playerZ + 45f;
            _nextTrapZ = playerZ + 35f;
        }

        public void DespawnAll()
        {
            for (int i = 0; i < _active.Count; i++) if (_active[i] != null) Destroy(_active[i]);
            _active.Clear();
        }

        void Update()
        {
            var gm = GameManager.Instance;
            var rounds = RoundManager.Instance;
            if (gm == null || rounds == null || gm.State != GameState.Playing) return;
            EnsureMaterials();
            float pz = gm.MaxPlayerZ();
            float density = rounds.ObstacleDensityMultiplier;

            if (rounds.HasCrocodiles)
                while (_nextCrocZ < pz + spawnAhead) { SpawnCroc(_nextCrocZ); _nextCrocZ += Random.Range(18f, 30f) / Mathf.Max(0.8f, density); }
            if (rounds.HasRoadHazards)
                while (_nextHazardZ < pz + spawnAhead) { SpawnHazard(_nextHazardZ); _nextHazardZ += Random.Range(20f, 34f) / Mathf.Max(0.8f, density); }
            if (rounds.HasRollingBoulders)
                while (_nextBoulderZ < pz + spawnAhead) { SpawnBoulder(_nextBoulderZ); _nextBoulderZ += Random.Range(28f, 45f) / Mathf.Max(0.8f, density); }
            if (rounds.HasTraps)
                while (_nextTrapZ < pz + spawnAhead) { SpawnTrap(_nextTrapZ); _nextTrapZ += Random.Range(24f, 40f) / Mathf.Max(0.8f, density); }

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var go = _active[i];
                if (go == null) { _active.RemoveAt(i); continue; }
                if (go.transform.position.z < pz - recycleBehind) { Destroy(go); _active.RemoveAt(i); }
            }
        }

        void SpawnCroc(float z)
        {
            if (z > CourseBuilder.FinishZ - 15f) return;
            float riverZ = Mathf.Clamp(z, CourseBuilder.RiverStartZ + 4f, CourseBuilder.RiverEndZ - 4f);
            if (z < CourseBuilder.RiverStartZ || z > CourseBuilder.RiverEndZ) riverZ = Random.Range(CourseBuilder.RiverStartZ + 5f, CourseBuilder.RiverEndZ - 5f);
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Crocodile";
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(Random.Range(-6f, 6f), -0.45f, riverZ);
            go.transform.localScale = new Vector3(1.1f, 0.45f, 2.2f);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
            go.GetComponent<MeshRenderer>().sharedMaterial = _croMat;
            var c = go.AddComponent<Crocodile>();
            c.direction = Random.value < 0.5f ? -1 : 1;
            c.speed = Random.Range(1.8f, 2.8f); // active hazard, still dodgeable
            c.OnSpawned();
            _active.Add(go);
        }

        void SpawnHazard(float z)
        {
            if (z > CourseBuilder.FinishZ - 15f) return;
            if (z > CourseBuilder.RiverStartZ - 2f && z < CourseBuilder.RiverEndZ + 2f) return;
            bool oil = Random.value < 0.55f;
            var go = GameObject.CreatePrimitive(oil ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            go.name = oil ? "OilSlick" : "RoadBarrier";
            go.transform.SetParent(transform, false);
            float x = Random.Range(-7f, 7f);
            if (oil)
            {
                go.transform.position = new Vector3(x, 0.03f, z);
                go.transform.localScale = new Vector3(2.4f, 0.02f, 2.4f);
                var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
                go.GetComponent<MeshRenderer>().sharedMaterial = _oilMat;
                var s = go.AddComponent<OilSlick>();
                s.radiusX = 1.2f; s.radiusZ = 1.2f;
            }
            else
            {
                go.transform.position = new Vector3(x, 0.55f, z);
                go.transform.localScale = new Vector3(1.2f, 1.1f, 1.2f);
                var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
                go.GetComponent<MeshRenderer>().sharedMaterial = _coneMat;
                go.AddComponent<ConstructionBarrier>();
            }
            _active.Add(go);
        }

        void SpawnBoulder(float z)
        {
            if (z > CourseBuilder.FinishZ - 15f) return;
            if (z > CourseBuilder.RiverStartZ - 2f && z < CourseBuilder.RiverEndZ + 2f) z = CourseBuilder.GrassEndZ - 6f;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "RollingBoulder";
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(Random.Range(-8f, 8f), 0.75f, z);
            go.transform.localScale = Vector3.one * 1.5f;
            var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
            go.GetComponent<MeshRenderer>().sharedMaterial = _boulderMat;
            var b = go.AddComponent<RollingBoulder>();
            b.direction = Random.value < 0.5f ? -1 : 1;
            b.speed = Random.Range(4.5f, 6.5f); // fair
            _active.Add(go);
        }

        void SpawnTrap(float z)
        {
            if (z > CourseBuilder.FinishZ - 15f) return;
            if (z > CourseBuilder.RiverStartZ - 2f && z < CourseBuilder.RiverEndZ + 2f) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "PopUpTrap";
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(Random.Range(-6.5f, 6.5f), 0.0f, z);
            go.transform.localScale = new Vector3(0.8f, 0.45f, 0.8f);
            var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
            go.GetComponent<MeshRenderer>().sharedMaterial = _trapMat;
            var t = go.AddComponent<PopUpTrap>();
            t.OnSpawned();
            _active.Add(go);
        }
    }
}
