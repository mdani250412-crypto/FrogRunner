using FrogRunner.Core;
using UnityEngine;

namespace FrogRunner.Player
{
    /// <summary>
    /// Smooth third-person chase camera with lateral look-ahead, speed FOV and impact shake.
    /// TUNING: offset/lookAhead/smoothTime control the feel; Shake() calls come from jumps and hits.
    /// </summary>
    [DisallowMultipleComponent]
    public class ChaseCamera : MonoBehaviour
    {
        public static ChaseCamera Instance { get; private set; }

        public Transform target;
        public FrogController controller;
        public Vector3 offset = new Vector3(0f, 3.15f, -7.2f);
        public float lookHeight = 1.0f;
        public float lookAhead = 3.6f;
        public float lateralFollow = 0.75f;
        public float smoothTime = 0.16f;
        public float rotationSharpness = 9f;
        public float baseFov = 60f;

        Vector3 smoothedPosition;
        Vector3 followVelocity;
        Quaternion smoothedRotation = Quaternion.identity;
        bool initialized;
        float shakeTime;
        float shakeDuration;
        float shakeAmount;
        Camera _camera;

        void Awake()
        {
            Instance = this;
            _camera = GetComponent<Camera>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Shake(float amount, float duration)
        {
            shakeAmount = Mathf.Max(shakeAmount, amount);
            shakeDuration = Mathf.Max(shakeDuration, duration);
            shakeTime = Mathf.Max(shakeTime, duration);
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            Vector3 look = new Vector3(target.position.x, target.position.y + lookHeight, target.position.z + lookAhead);
            smoothedPosition = new Vector3(target.position.x * lateralFollow, target.position.y + offset.y, target.position.z + offset.z);
            smoothedRotation = Quaternion.LookRotation(look - smoothedPosition, Vector3.up);
            transform.SetPositionAndRotation(smoothedPosition, smoothedRotation);
            followVelocity = Vector3.zero;
            initialized = true;
        }

        void LateUpdate()
        {
            if (target == null && GameManager.Instance != null && GameManager.Instance.Player != null)
                target = GameManager.Instance.Player.transform;
            if (controller == null && target != null)
                controller = target.GetComponent<FrogController>();
            if (target == null) return;
            if (!initialized) { SnapToTarget(); return; }

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            float speed01 = controller != null ? Mathf.Clamp01(controller.Speed / 16f) : 0f;
            float glide01 = controller != null && controller.IsGliding ? 1f : 0f;

            Vector3 desiredPosition = new Vector3(
                target.position.x * lateralFollow,
                target.position.y + offset.y + glide01 * 0.45f,
                target.position.z + offset.z - speed01 * 0.55f);

            smoothedPosition = Vector3.SmoothDamp(smoothedPosition, desiredPosition, ref followVelocity, Mathf.Max(0.02f, smoothTime), Mathf.Infinity, dt);

            Vector3 lookAt = new Vector3(
                target.position.x,
                target.position.y + lookHeight,
                target.position.z + lookAhead + speed01 * 1.8f);
            Quaternion desiredRotation = Quaternion.LookRotation(lookAt - smoothedPosition, Vector3.up);
            smoothedRotation = Quaternion.Slerp(smoothedRotation, desiredRotation, 1f - Mathf.Exp(-rotationSharpness * dt));

            Vector3 shakeOffset = Vector3.zero;
            float roll = 0f;
            if (shakeTime > 0f)
            {
                shakeTime = Mathf.Max(0f, shakeTime - dt);
                float strength = shakeDuration > 0.001f ? shakeTime / shakeDuration : 0f;
                Vector3 random = Random.insideUnitSphere * (shakeAmount * strength);
                shakeOffset = new Vector3(random.x, random.y, 0f);
                roll = random.x * 4f;
                if (shakeTime <= 0f)
                {
                    shakeAmount = 0f;
                    shakeDuration = 0f;
                }
            }

            transform.position = smoothedPosition + shakeOffset;
            transform.rotation = smoothedRotation * Quaternion.Euler(0f, 0f, roll);

            if (_camera != null)
            {
                float targetFov = baseFov + speed01 * 5.5f + glide01 * 3.5f;
                _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFov, 1f - Mathf.Exp(-4f * dt));
            }
        }
    }
}
