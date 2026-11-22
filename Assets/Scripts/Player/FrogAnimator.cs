using FrogRunner.RiverGlider;
using UnityEngine;

namespace FrogRunner.Player
{
    /// <summary>
    /// Code-driven frog pose machine (no external animation clips required).
    /// States: intro tuck-hop, run/hop cycle, airborne, gliding, crouch, dash, tumble,
    /// log balancing and landing squash. Swap in a custom FBX later by replacing FrogFactory visuals
    /// and pointing these transform fields at the new rig.
    /// TUNING: phase speed and pose angles are all inside LateUpdate below.
    /// </summary>
    [DisallowMultipleComponent]
    public class FrogAnimator : MonoBehaviour
    {
        public FrogController controller;
        public Transform visualRoot;
        public Transform bodyRoot;
        public Transform bodyMesh;
        public Transform headPivot;
        public Transform armL;
        public Transform armR;
        public Transform forearmL;
        public Transform forearmR;
        public Transform legL;
        public Transform legR;
        public Transform legLowerL;
        public Transform legLowerR;
        public Transform footL;
        public Transform footR;
        public HangGlider glider;

        class Part
        {
            public Transform t;
            public Vector3 position;
            public Quaternion rotation = Quaternion.identity;
            public Vector3 scale = Vector3.one;

            public void Cache()
            {
                if (t == null) return;
                position = t.localPosition;
                rotation = t.localRotation;
                scale = t.localScale;
            }

            public void Reset()
            {
                if (t == null) return;
                t.localPosition = position;
                t.localRotation = rotation;
                t.localScale = scale;
            }
        }

        Part _body = new Part();
        Part _head = new Part();
        Part _armL = new Part();
        Part _armR = new Part();
        Part _forearmL = new Part();
        Part _forearmR = new Part();
        Part _legL = new Part();
        Part _legR = new Part();
        Part _legLowerL = new Part();
        Part _legLowerR = new Part();
        Part _footL = new Part();
        Part _footR = new Part();
        Part _bodyMesh = new Part();

        bool _cached;
        float _phase;
        float _landTimer;
        float _tumbleSpin;
        bool _wasGrounded = true;

        public void CaptureBasePose()
        {
            if (visualRoot == null) visualRoot = transform.Find("Visual");
            _body.t = bodyRoot;
            _head.t = headPivot;
            _armL.t = armL;
            _armR.t = armR;
            _forearmL.t = forearmL;
            _forearmR.t = forearmR;
            _legL.t = legL;
            _legR.t = legR;
            _legLowerL.t = legLowerL;
            _legLowerR.t = legLowerR;
            _footL.t = footL;
            _footR.t = footR;
            _bodyMesh.t = bodyMesh;

            _body.Cache();
            _head.Cache();
            _armL.Cache();
            _armR.Cache();
            _forearmL.Cache();
            _forearmR.Cache();
            _legL.Cache();
            _legR.Cache();
            _legLowerL.Cache();
            _legLowerR.Cache();
            _footL.Cache();
            _footR.Cache();
            _bodyMesh.Cache();
            _cached = true;
        }

        public void ResetPose()
        {
            if (!_cached) CaptureBasePose();
            _body.Reset();
            _head.Reset();
            _armL.Reset();
            _armR.Reset();
            _forearmL.Reset();
            _forearmR.Reset();
            _legL.Reset();
            _legR.Reset();
            _legLowerL.Reset();
            _legLowerR.Reset();
            _footL.Reset();
            _footR.Reset();
            _bodyMesh.Reset();
            _phase = 0f;
            _landTimer = 0f;
            _tumbleSpin = 0f;
            _wasGrounded = true;
            if (glider != null) glider.SetDeployed(false);
        }

        void LateUpdate()
        {
            if (controller == null) controller = GetComponent<FrogController>();
            if (!_cached) CaptureBasePose();
            if (controller == null || _body.t == null) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            bool grounded = controller.IsGrounded;
            bool gliding = controller.IsGliding;
            bool tumbling = controller.IsTumbling;
            bool dashing = controller.IsDashing;
            bool crouching = controller.IsCrouching;
            bool onLog = controller.IsOnLog;
            bool intro = controller.IsIntroTuck;
            float speed01 = Mathf.Clamp01(controller.Speed / 16f);

            if (!_wasGrounded && grounded) _landTimer = 0.26f;
            _wasGrounded = grounded;
            if (_landTimer > 0f) _landTimer = Mathf.Max(0f, _landTimer - dt);
            if (tumbling) _tumbleSpin += dt * 720f;
            else _tumbleSpin = Mathf.MoveTowards(_tumbleSpin, 0f, 900f * dt);

            _phase += dt * (3f + speed01 * 13f);
            float k = 1f - Mathf.Exp(-16f * dt);
            float softK = 1f - Mathf.Exp(-10f * dt);

            Vector3 bodyPos = _body.position;
            Quaternion bodyRot = _body.rotation;
            Vector3 bodyScale = _bodyMesh.scale;
            Quaternion headRot = _head.rotation;
            Vector3 headPos = _head.position;

            float armLX = 0f, armRX = 0f, armLZ = 12f, armRZ = -12f;
            float foreLX = 18f, foreRX = 18f;
            float legLX = 0f, legRX = 0f, legLZ = 5f, legRZ = -5f;
            float lowerLX = 12f, lowerRX = 12f;
            float footLX = -8f, footRX = -8f;

            if (tumbling)
            {
                bodyRot *= Quaternion.Euler(0f, 0f, _tumbleSpin);
                bodyPos += Vector3.up * 0.08f;
                bodyScale = Scale(bodyScale, new Vector3(1.05f, 0.82f, 1.12f));
                float flail = Mathf.Sin(_tumbleSpin * Mathf.Deg2Rad) * 65f;
                armLX = flail; armRX = -flail; armLZ = 55f; armRZ = -55f;
                legLX = -flail * 0.7f; legRX = flail * 0.7f;
            }
            else if (gliding)
            {
                bodyRot *= Quaternion.Euler(-14f, 0f, 0f);
                bodyPos += Vector3.up * 0.03f;
                armLX = -158f; armRX = -158f; armLZ = 24f; armRZ = -24f;
                foreLX = 6f; foreRX = 6f;
                legLX = -22f; legRX = -22f; lowerLX = 38f; lowerRX = 38f;
                footLX = -18f; footRX = -18f;
            }
            else if (crouching)
            {
                bodyPos += Vector3.down * 0.22f;
                bodyScale = Scale(bodyScale, new Vector3(1.08f, 0.76f, 1.06f));
                armLX = 18f; armRX = 18f;
                legLX = 38f; legRX = 38f; lowerLX = -48f; lowerRX = -48f;
            }
            else if (!grounded)
            {
                bodyRot *= Quaternion.Euler(-8f, 0f, 0f);
                armLX = -72f; armRX = -72f; armLZ = 38f; armRZ = -38f;
                legLX = -45f; legRX = -45f; lowerLX = 55f; lowerRX = 55f;
            }
            else if (intro)
            {
                bodyPos += Vector3.down * 0.10f;
                bodyRot *= Quaternion.Euler(26f, 0f, 0f);
                bodyScale = Scale(bodyScale, new Vector3(1.02f, 0.86f, 1.06f));
                armLX = 55f; armRX = 55f;
                legLX = -62f; legRX = -62f; lowerLX = 72f; lowerRX = 72f;
            }
            else if (dashing)
            {
                bodyRot *= Quaternion.Euler(48f, 0f, 0f);
                bodyPos += Vector3.down * 0.06f;
                bodyScale = Scale(bodyScale, new Vector3(1.10f, 0.84f, 1.14f));
                armLX = 82f; armRX = 82f; armLZ = 24f; armRZ = -24f;
                legLX = -70f; legRX = -70f; lowerLX = 80f; lowerRX = 80f;
            }
            else if (onLog)
            {
                float sway = Mathf.Sin(Time.time * 3.2f) * 6f;
                bodyRot *= Quaternion.Euler(0f, sway * 0.35f, sway);
                bodyPos += Vector3.up * (Mathf.Sin(Time.time * 4f) * 0.015f);
                armLX = 5f; armRX = 5f; armLZ = 68f; armRZ = -68f;
                legLX = 18f; legRX = 18f; lowerLX = -24f; lowerRX = -24f;
            }
            else if (speed01 > 0.04f)
            {
                float hop = Mathf.Abs(Mathf.Sin(_phase));
                bodyPos += Vector3.up * (hop * (0.06f + speed01 * 0.10f));
                bodyRot *= Quaternion.Euler(Mathf.Sin(_phase * 2f) * 4f, 0f, Mathf.Sin(_phase) * 3f);
                bodyScale = Scale(bodyScale, new Vector3(1f + hop * 0.03f, 1f - hop * 0.04f, 1f + hop * 0.03f));

                float swing = Mathf.Sin(_phase) * (22f + speed01 * 22f);
                legLX = swing; legRX = -swing;
                lowerLX = 20f + Mathf.Max(0f, -swing) * 0.7f;
                lowerRX = 20f + Mathf.Max(0f, swing) * 0.7f;
                footLX = -12f - Mathf.Max(0f, swing) * 0.3f;
                footRX = -12f - Mathf.Max(0f, -swing) * 0.3f;
                armLX = -swing * 0.65f; armRX = swing * 0.65f;
                armLZ = 16f; armRZ = -16f;
            }
            else
            {
                bodyPos += Vector3.up * (Mathf.Sin(Time.time * 1.8f) * 0.015f);
                headRot *= Quaternion.Euler(Mathf.Sin(Time.time * 1.1f) * 3f, Mathf.Sin(Time.time * 0.7f) * 4f, 0f);
                armLX = 4f; armRX = 4f;
                legLX = 2f; legRX = 2f;
            }

            if (_landTimer > 0f && !tumbling && !gliding)
            {
                float l = _landTimer / 0.26f;
                bodyScale = Scale(bodyScale, new Vector3(1f + l * 0.14f, 1f - l * 0.18f, 1f + l * 0.14f));
                bodyPos += Vector3.down * (l * 0.04f);
                armLZ += l * 30f; armRZ -= l * 30f;
            }

            if (crouching) headPos += Vector3.down * 0.14f;
            if (gliding) headPos += Vector3.up * 0.04f;

            Apply(_body, bodyPos, bodyRot, _body.scale, k);
            Apply(_bodyMesh, _bodyMesh.position, _bodyMesh.rotation, bodyScale, k);
            Apply(_head, headPos, headRot, _head.scale, k);
            ApplyArm(_armL, _armL.rotation, armLX, armLZ, k);
            ApplyArm(_armR, _armR.rotation, armRX, armRZ, k);
            ApplyArm(_forearmL, _forearmL.rotation, foreLX, 0f, k);
            ApplyArm(_forearmR, _forearmR.rotation, foreRX, 0f, k);
            ApplyArm(_legL, _legL.rotation, legLX, legLZ, k);
            ApplyArm(_legR, _legR.rotation, legRX, legRZ, k);
            ApplyArm(_legLowerL, _legLowerL.rotation, lowerLX, 0f, k);
            ApplyArm(_legLowerR, _legLowerR.rotation, lowerRX, 0f, k);
            ApplyArm(_footL, _footL.rotation, footLX, 0f, softK);
            ApplyArm(_footR, _footR.rotation, footRX, 0f, softK);

            if (glider != null) glider.SetDeployed(gliding);
        }

        static void Apply(Part part, Vector3 position, Quaternion rotation, Vector3 scale, float k)
        {
            if (part.t == null) return;
            part.t.localPosition = Vector3.Lerp(part.t.localPosition, position, k);
            part.t.localRotation = Quaternion.Slerp(part.t.localRotation, rotation, k);
            part.t.localScale = Vector3.Lerp(part.t.localScale, scale, k);
        }

        static void ApplyArm(Part part, Quaternion baseRotation, float x, float z, float k)
        {
            if (part.t == null) return;
            part.t.localRotation = Quaternion.Slerp(part.t.localRotation, baseRotation * Quaternion.Euler(x, 0f, z), k);
        }

        static Vector3 Scale(Vector3 a, Vector3 b) => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
    }
}
