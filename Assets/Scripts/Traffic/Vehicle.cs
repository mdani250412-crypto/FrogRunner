using FrogRunner.Core;
using FrogRunner.Player;
using FrogRunner.Vehicles;
using UnityEngine;

namespace FrogRunner.Traffic
{
    /// <summary>
    /// One pooled highway vehicle. Drives itself along Z, checks hits/near-misses against the frog,
    /// and is recycled by TrafficSpawner. Vehicles have no physics collider: collision checks are
    /// mathematical, which keeps a 30-car pool cheap and lets the runner jump clean over roofs.
    /// TUNING: hit box padding and the near-miss band are inside CheckPlayer().
    /// </summary>
    public class Vehicle : MonoBehaviour
    {
        public float Speed { get; private set; } = 6f;
        public float Length { get; private set; } = 4.2f;
        public float Width { get; private set; } = 1.9f;
        public float TopHeight { get; private set; } = 1.45f;
        public int Direction { get; private set; } = 1;

        static Material _glass, _tire, _rim, _light, _tail, _bumper, _cargo;

        Transform _visualRoot;
        readonly System.Collections.Generic.List<WheelRotator> _wheels = new System.Collections.Generic.List<WheelRotator>();
        float _wheelSpin;
        bool _nearMissed;

        public static Vehicle Create(Transform parent, Material paint, bool truck, int seed)
        {
            var go = new GameObject(truck ? "Truck" : "Car");
            go.transform.SetParent(parent, false);
            var vehicle = go.AddComponent<Vehicle>();
            vehicle.BuildVisual(paint, truck, seed);
            go.SetActive(false);
            return vehicle;
        }

        void BuildVisual(Material paint, bool truck, int seed)
        {
            EnsureSharedMaterials();
            _visualRoot = new GameObject("Visual").transform;
            _visualRoot.SetParent(transform, false);

            if (truck)
            {
                Length = 5.6f;
                Width = 2.05f;
                TopHeight = 3.05f;

                RetroBuilder.Box("Chassis", _visualRoot, new Vector3(0f, 0.78f, 0f),
                    new Vector3(2.02f, 0.58f, 5.5f), paint);
                RetroBuilder.Box("Cab", _visualRoot, new Vector3(0f, 1.62f, 1.55f),
                    new Vector3(1.96f, 1.35f, 1.65f), paint);
                RetroBuilder.Box("Windshield", _visualRoot, new Vector3(0f, 1.77f, 2.36f),
                    new Vector3(1.62f, 0.72f, 0.06f), _glass);
                RetroBuilder.Box("CargoBox", _visualRoot, new Vector3(0f, 1.95f, -0.75f),
                    new Vector3(2.15f, 2.05f, 3.2f), _cargo);
                RetroBuilder.Box("CargoStripe", _visualRoot, new Vector3(0f, 1.35f, -0.75f),
                    new Vector3(2.18f, 0.22f, 3.23f), paint);
                RetroBuilder.Box("FrontBumper", _visualRoot, new Vector3(0f, 0.55f, 2.84f),
                    new Vector3(2.12f, 0.28f, 0.24f), _bumper);
                RetroBuilder.Box("RearBumper", _visualRoot, new Vector3(0f, 0.55f, -2.86f),
                    new Vector3(2.12f, 0.28f, 0.24f), _bumper);
                AddLights(1.0f, 2.82f, 0.62f, 2.86f, 1.82f);
                AddWheels(0.62f, 1.55f, 2.02f, 0.72f);
            }
            else
            {
                Length = 4.3f;
                Width = 1.9f;
                TopHeight = 1.48f;

                RetroBuilder.Box("Chassis", _visualRoot, new Vector3(0f, 0.58f, 0f),
                    new Vector3(1.84f, 0.56f, 4.22f), paint);
                RetroBuilder.Box("Cabin", _visualRoot, new Vector3(0f, 1.06f, -0.12f),
                    new Vector3(1.58f, 0.58f, 1.95f), paint);
                RetroBuilder.Box("Windshield", _visualRoot, new Vector3(0f, 1.16f, 0.88f),
                    new Vector3(1.42f, 0.42f, 0.06f), _glass);
                RetroBuilder.Box("RearGlass", _visualRoot, new Vector3(0f, 1.17f, -1.10f),
                    new Vector3(1.36f, 0.40f, 0.06f), _glass);
                RetroBuilder.Box("SideGlassL", _visualRoot, new Vector3(-0.80f, 1.15f, -0.1f),
                    new Vector3(0.05f, 0.34f, 1.5f), _glass);
                RetroBuilder.Box("SideGlassR", _visualRoot, new Vector3(0.80f, 1.15f, -0.1f),
                    new Vector3(0.05f, 0.34f, 1.5f), _glass);
                RetroBuilder.Box("FrontBumper", _visualRoot, new Vector3(0f, 0.42f, 2.16f),
                    new Vector3(1.98f, 0.22f, 0.20f), _bumper);
                RetroBuilder.Box("RearBumper", _visualRoot, new Vector3(0f, 0.42f, -2.16f),
                    new Vector3(1.98f, 0.22f, 0.20f), _bumper);
                AddLights(0.84f, 2.17f, 0.56f, 2.19f, 1.16f);
                AddWheels(0.55f, 1.16f, 1.9f, 0.62f);
            }

            // A tiny bob/tilt makes traffic read as alive without animation assets.
            _visualRoot.localRotation = Quaternion.Euler(0f, 0f, ((seed % 5) - 2) * 0.3f);
        }

        void AddLights(float lightX, float lightZ, float bumperY, float tailZ, float tailY)
        {
            RetroBuilder.Box("HeadlightL", _visualRoot, new Vector3(-lightX, bumperY, lightZ),
                new Vector3(0.36f, 0.20f, 0.08f), _light);
            RetroBuilder.Box("HeadlightR", _visualRoot, new Vector3(lightX, bumperY, lightZ),
                new Vector3(0.36f, 0.20f, 0.08f), _light);
            RetroBuilder.Box("TaillightL", _visualRoot, new Vector3(-lightX, tailY, -tailZ),
                new Vector3(0.30f, 0.18f, 0.08f), _tail);
            RetroBuilder.Box("TaillightR", _visualRoot, new Vector3(lightX, tailY, -tailZ),
                new Vector3(0.30f, 0.18f, 0.08f), _tail);
        }

        void AddWheels(float y, float z, float halfWidth, float radius)
        {
            // Cylinder mesh: Y is height (axle). Rotate 90 on Z so flat faces point outward (left/right).
            float tireWidth = Mathf.Max(0.18f, radius * 0.45f);
            var wheelScale = new Vector3(radius * 2f, tireWidth, radius * 2f);
            Quaternion wheelRotation = Quaternion.Euler(0f, 0f, 90f);
            float outward = halfWidth - tireWidth * 0.5f;
            CreateWheel("WheelFL", new Vector3(-outward, y, z), wheelScale, wheelRotation, radius, true);
            CreateWheel("WheelFR", new Vector3(outward, y, z), wheelScale, wheelRotation, radius, false);
            CreateWheel("WheelRL", new Vector3(-outward, y, -z), wheelScale, wheelRotation, radius, true);
            CreateWheel("WheelRR", new Vector3(outward, y, -z), wheelScale, wheelRotation, radius, false);
        }

        void CreateWheel(string name, Vector3 localPos, Vector3 scale, Quaternion rotation, float radius, bool leftSide)
        {
            GameObject wheel = RetroBuilder.Cylinder(name, _visualRoot, localPos, scale, _tire, false, true, false, rotation);
            // Rim/hubcap: slightly smaller diameter, sits on outer face for visible detail.
            float rimOut = (scale.y * 0.5f) + 0.01f;
            Vector3 rimLocal = leftSide ? new Vector3(0f, -rimOut, 0f) : new Vector3(0f, rimOut, 0f);
            var rimScale = new Vector3(radius * 1.15f, 0.03f, radius * 1.15f);
            RetroBuilder.Cylinder(name + "_Rim", wheel.transform, rimLocal, rimScale, _rim, false, true, false);

            var rotator = wheel.AddComponent<WheelRotator>();
            rotator.radius = Mathf.Max(0.1f, radius);
            rotator.localAxis = Vector3.up; // axle along cylinder height/local Y
            rotator.CacheBase();
            _wheels.Add(rotator);
        }

        static void EnsureSharedMaterials()
        {
            if (_glass != null) return;
            _glass = RetroAssets.Solid("MAT_CarGlass", new Color(0.10f, 0.17f, 0.22f), 0.4f);
            _tire = RetroAssets.Solid("MAT_Tire", new Color(0.07f, 0.07f, 0.08f), 0.04f);
            _rim = RetroAssets.Solid("MAT_Rim", new Color(0.78f, 0.80f, 0.84f), 0.35f);
            _light = RetroAssets.Solid("MAT_Headlight", new Color(1f, 0.92f, 0.62f), 0.3f, true);
            _tail = RetroAssets.Solid("MAT_Taillight", new Color(0.75f, 0.08f, 0.05f), 0.2f, true);
            _bumper = RetroAssets.Solid("MAT_Bumper", new Color(0.42f, 0.43f, 0.45f), 0.15f);
            _cargo = RetroAssets.Solid("MAT_Cargo", new Color(0.80f, 0.80f, 0.76f), 0.06f);
        }

        public void Spawn(int direction, float speed, float laneX, float z)
        {
            Direction = direction >= 0 ? 1 : -1;
            Speed = speed;
            transform.position = new Vector3(laneX, 0.04f, z);
            transform.rotation = Direction > 0 ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
            _nearMissed = false;
            gameObject.SetActive(true);
        }

        public void Despawn()
        {
            _nearMissed = false;
            gameObject.SetActive(false);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            transform.position += Vector3.forward * (Direction * Speed * dt);

            if (_visualRoot != null)
            {
                _wheelSpin += Direction * Speed * dt * 3.2f;
                _visualRoot.localPosition = new Vector3(0f, Mathf.Sin(_wheelSpin * 0.12f) * 0.008f, 0f);
            }
            for (int i = 0; i < _wheels.Count; i++)
                _wheels[i].Spin(Speed, Direction, dt);

            CheckPlayer();
        }

        void CheckPlayer()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;
            var players = gm.Players;
            if (players == null || players.Count == 0) return;

            float hitHalfWidth = Width * 0.5f + 0.28f;
            float hitHalfLength = Length * 0.5f + 0.22f;
            float nearOuter = hitHalfWidth + 1.35f;

            for (int i = 0; i < players.Count; i++)
            {
                FrogController player = players[i];
                if (player == null || player.InWater) continue;
                Vector3 p = player.transform.position;
                float dx = Mathf.Abs(p.x - transform.position.x);
                float dz = Mathf.Abs(p.z - transform.position.z);

                if (dx <= hitHalfWidth && dz <= hitHalfLength && p.y <= TopHeight + 0.10f)
                {
                    _nearMissed = true;
                    gm.OnTrafficHit(transform.position, player);
                    // Shield can bounce player; let other players still be checked.
                    continue;
                }

                if (!_nearMissed && dz < 1.8f && dx > hitHalfWidth && dx < nearOuter)
                {
                    _nearMissed = true;
                    gm.OnNearMiss(transform.position, player);
                }
                else if (dz > 5.5f && dx > nearOuter + 1.5f)
                {
                    _nearMissed = false;
                }
            }
        }
    }
}
