using FrogRunner.Core;
using UnityEngine;

namespace FrogRunner.RiverGlider
{
    /// <summary>
    /// A drifting, bobbing river log the frog can land on and ride. The character controller applies
    /// LastDelta each frame so standing on a moving log feels stable. Logs are wide for reliable landings.
    /// TUNING: drift amplitude/speed per log are passed by CourseBuilder.
    /// </summary>
    [DisallowMultipleComponent]
    public class FloatingLog : MonoBehaviour
    {
        public Vector3 LastDelta { get; private set; }
        public float TopY => transform.position.y + 0.62f;

        Vector3 _basePosition;
        float _phase;
        float _driftAmplitude;
        float _driftSpeed;

        public static FloatingLog Create(Transform parent, Vector3 position, float driftAmplitude,
            float driftSpeed, float phase)
        {
            var go = new GameObject("FloatingLog");
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var log = go.AddComponent<FloatingLog>();

            // Extra wide/long logs for forgiving river crossing.
            RetroBuilder.Cylinder("LogBody", go.transform, new Vector3(0f, 0.16f, 0f),
                new Vector3(2.1f, 12.5f, 2.1f), RetroAssets.Wood, false, true, true,
                Quaternion.Euler(0f, 0f, 90f));
            RetroBuilder.Cylinder("EndRingL", go.transform, new Vector3(-6.15f, 0.16f, 0f),
                new Vector3(2.2f, 0.18f, 2.2f), RetroAssets.Bark, false, true, true,
                Quaternion.Euler(0f, 0f, 90f));
            RetroBuilder.Cylinder("EndRingR", go.transform, new Vector3(6.15f, 0.16f, 0f),
                new Vector3(2.2f, 0.18f, 2.2f), RetroAssets.Bark, false, true, true,
                Quaternion.Euler(0f, 0f, 90f));
            RetroBuilder.Box("MossPatchA", go.transform, new Vector3(-1.6f, 1.05f, 0.18f),
                new Vector3(2.8f, 0.06f, 0.95f), RetroAssets.Leaf1, false, false, true);
            RetroBuilder.Box("MossPatchB", go.transform, new Vector3(2.0f, 1.04f, -0.12f),
                new Vector3(2.0f, 0.05f, 0.84f), RetroAssets.Leaf2, false, false, true);

            var box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.45f, 0f);
            box.size = new Vector3(13.2f, 1.35f, 2.6f); // generous landing footprint

            log.Configure(driftAmplitude, driftSpeed, phase);
            return log;
        }

        float _driftZ;
        float _driftZSpeed;

        public void Configure(float driftAmplitude, float driftSpeed, float phase)
        {
            _basePosition = transform.position;
            _driftAmplitude = driftAmplitude;
            _driftSpeed = driftSpeed;
            _phase = phase;
            _driftZ = 0f;
            _driftZSpeed = 0.4f + Mathf.Abs(phase % 1f) * 0.35f;
            LastDelta = Vector3.zero;
        }

        void Update()
        {
            float t = Time.time;
            float dt = Time.deltaTime;
            // Visible lane drift along river + lateral bob; wrap within river bounds.
            _driftZ += _driftZSpeed * dt;
            float riverLen = FrogRunner.WorldGen.CourseBuilder.RiverEndZ - FrogRunner.WorldGen.CourseBuilder.RiverStartZ - 8f;
            if (riverLen > 1f && _driftZ > riverLen) _driftZ -= riverLen;
            Vector3 next = new Vector3(
                _basePosition.x + Mathf.Sin(t * _driftSpeed + _phase) * _driftAmplitude,
                _basePosition.y + Mathf.Sin(t * 1.9f + _phase * 1.7f) * 0.055f,
                _basePosition.z + _driftZ);
            float minZ = FrogRunner.WorldGen.CourseBuilder.RiverStartZ + 4f;
            float maxZ = FrogRunner.WorldGen.CourseBuilder.RiverEndZ - 4f;
            if (next.z > maxZ) { next.z = minZ + (next.z - maxZ); _basePosition.z = minZ - _driftZ; }
            LastDelta = next - transform.position;
            transform.position = next;
            transform.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 0.37f + _phase) * 3.5f, 0f);
        }

        void OnDisable()
        {
            LastDelta = Vector3.zero;
        }
    }
}
