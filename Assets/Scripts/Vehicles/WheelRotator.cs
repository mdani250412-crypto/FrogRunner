using UnityEngine;

namespace FrogRunner.Vehicles
{
    /// <summary>
    /// Spins wheel meshes around their local axle axis proportional to vehicle speed.
    /// Add to each wheel transform; Vehicle sets Speed/Direction each frame.
    /// TUNING: radius controls visual rotation rate - match it to the wheel mesh radius.
    /// </summary>
    public class WheelRotator : MonoBehaviour
    {
        [Tooltip("Wheel radius in world units used to convert linear speed to angular spin.")]
        public float radius = 0.32f;

        [Tooltip("Local axis the wheel rotates around. For cylinders rotated 90 on Z, local Y is the axle.")]
        public Vector3 localAxis = Vector3.up;

        float _spinAngle;

        public void Spin(float linearSpeed, int direction, float dt)
        {
            if (radius <= 0.001f || dt <= 0f) return;
            float circumference = 2f * Mathf.PI * radius;
            float degrees = (linearSpeed / circumference) * 360f * direction * dt;
            _spinAngle = Mathf.Repeat(_spinAngle + degrees, 360f);
            transform.localRotation = Quaternion.AngleAxis(_spinAngle, localAxis) * _baseRotation;
        }

        Quaternion _baseRotation;
        bool _cached;

        void Awake()
        {
            CacheBase();
        }

        public void CacheBase()
        {
            if (_cached) return;
            _baseRotation = transform.localRotation;
            _cached = true;
        }

        public void ResetSpin()
        {
            _spinAngle = 0f;
            if (_cached) transform.localRotation = _baseRotation;
        }
    }
}
