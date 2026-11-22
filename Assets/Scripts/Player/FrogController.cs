using FrogRunner.Core;
using FrogRunner.Input;
using FrogRunner.RiverGlider;
using FrogRunner.Traffic;
using FrogRunner.WorldGen;
using UnityEngine;

namespace FrogRunner.Player
{
    /// <summary>
    /// Manual 4-way frog controller (CharacterController-based).
    /// Controls: WASD/Arrows = move, Space = jump/glide, Shift = dash, Ctrl/C = crouch.
    /// TUNING: all speeds, jump/glide physics, lane clamp and camera shake are exposed below.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public class FrogController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 12.5f; // fast from Round 1
        [SerializeField] private float backwardSpeedMultiplier = 0.8f;
        [SerializeField] private float acceleration = 70f;
        [SerializeField] private float playableHalfWidth = 11.5f;
        [SerializeField] private float minZ = -30f;

        [Header("Jump / Glide")]
        [SerializeField] private float jumpSpeed = 12.5f;
        [SerializeField] private float gravity = -32f;
        [SerializeField] private float glideGravity = -1.3f;
        [SerializeField] private float glideMaxFall = -1.2f;
        [SerializeField] private float glideActivateDelay = 0.1f;

        [Header("Dash / Crouch")]
        [SerializeField] private float dashSpeed = 18f;
        [SerializeField] private float dashDuration = 0.28f;
        [SerializeField] private float dashCooldown = 1.05f;
        [SerializeField] private float crouchSpeedMultiplier = 0.55f;

        [Header("Hit / Respawn")]
        [SerializeField] private float hitKnockback = 7.5f;
        [SerializeField] private float hitLift = 5.5f;
        [SerializeField] private float tumbleDuration = 1.15f;
        [SerializeField] private float invulnerabilityTime = 2f;

        public FrogAnimator animator;
        public int PlayerIndex { get; set; }
        public IPlayerInput InputSource { get; set; }
        public bool IsGrounded { get; private set; }
        public bool IsGliding { get; private set; }
        public bool IsDashing => _dashTimer > 0f;
        public bool IsTumbling => _tumbleTimer > 0f;
        public bool IsCrouching { get; private set; }
        public bool IsOnLog { get; private set; }
        public bool IsInvulnerable => _invulnerableTimer > 0f;
        public bool IsIntroTuck => _introTuckTimer > 0f;
        public bool InWater { get; private set; }
        public bool IsEliminatedThisRound { get; private set; }
        public string EliminationReason { get; private set; } = "";
        public float Speed => _speed;
        public float RunTime => _runTime;
        public float LateralInput => _lateralInput;
        public float ForwardInput { get; private set; }

        CharacterController _cc;
        Vector3 _planarVelocity;
        float _lateralInput;
        float _forwardInput;
        float _verticalVelocity = -2f;
        float _speed;
        float _airTime;
        float _runTime;
        float _dashTimer;
        float _dashCooldownTimer;
        float _tumbleTimer;
        float _invulnerableTimer;
        float _introTuckTimer;
        float _slideTimer;
        bool _controlEnabled;
        bool _wasGrounded = true;
        Vector3 _hitVelocity;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (animator == null) animator = GetComponent<FrogAnimator>();
        }

        public void SetControlEnabled(bool enabled)
        {
            _controlEnabled = enabled;
            if (!enabled)
            {
                _lateralInput = 0f;
                _forwardInput = 0f;
                ForwardInput = 0f;
                IsGliding = false;
                IsCrouching = false;
            }
        }

        public void BeginRun()
        {
            _runTime = 0f;
            _introTuckTimer = 0.15f;
            _planarVelocity = Vector3.zero;
            _verticalVelocity = 4.8f;
            IsGrounded = false;
            _wasGrounded = false;
            _airTime = 0f;
            InWater = false;
            ClearRoundElimination();
        }

        public void RespawnAt(Vector3 position)
        {
            _cc.enabled = false;
            transform.position = position;
            transform.rotation = Quaternion.identity;
            _cc.enabled = true;
            _planarVelocity = Vector3.zero;
            _lateralInput = 0f;
            _forwardInput = 0f;
            ForwardInput = 0f;
            _verticalVelocity = -2f;
            _dashTimer = 0f;
            _tumbleTimer = 0f;
            _hitVelocity = Vector3.zero;
            _invulnerableTimer = 1.4f;
            _introTuckTimer = 0f;
            _airTime = 0f;
            _runTime = 0f;
            IsOnLog = false;
            IsGliding = false;
            IsCrouching = false;
            InWater = false;
            IsGrounded = false;
            _wasGrounded = true;
            _speed = 0f;
            if (animator != null) animator.ResetPose();
            if (ChaseCamera.Instance != null) ChaseCamera.Instance.SnapToTarget();
        }

        public void SetInvulnerable(float seconds)
        {
            _invulnerableTimer = Mathf.Max(_invulnerableTimer, seconds);
        }

        public void ApplyHit(Vector3 sourcePosition)
        {
            Vector3 away = transform.position - sourcePosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = Vector3.back;
            _hitVelocity = away.normalized * hitKnockback + Vector3.right * Random.Range(-1.8f, 1.8f);
            _verticalVelocity = hitLift;
            _tumbleTimer = tumbleDuration;
            _invulnerableTimer = invulnerabilityTime;
            _planarVelocity *= 0.25f;
            IsGliding = false;
            IsGrounded = false;
            if (ChaseCamera.Instance != null) ChaseCamera.Instance.Shake(0.42f, 0.45f);
            if (FrogRunner.Audio.AudioManager.Instance != null) FrogRunner.Audio.AudioManager.Instance.PlayCrash();
        }

        /// <summary>Safe bounce used by force-field: no life loss, no tumble.</summary>
        public void ApplySafeBounce(Vector3 velocity)
        {
            _hitVelocity = new Vector3(velocity.x, 0f, velocity.z);
            _verticalVelocity = Mathf.Max(velocity.y, 4f);
            IsGliding = false;
            IsGrounded = false;
            _airTime = 0f;
        }

        /// <summary>Oil slick: temporary lateral slide, no damage.</summary>
        public void ApplySlide(float duration)
        {
            _slideTimer = Mathf.Max(_slideTimer, duration);
            if (_planarVelocity.sqrMagnitude < 2.25f)
                _planarVelocity += new Vector3(Random.value < 0.5f ? -5f : 5f, 0f, 0f);
        }

        public void EnterWaterDeath()
        {
            InWater = true;
            _controlEnabled = false;
            _planarVelocity = Vector3.zero;
            _hitVelocity = Vector3.zero;
            _verticalVelocity = 0f;
            IsGliding = false;
            if (ChaseCamera.Instance != null) ChaseCamera.Instance.Shake(0.25f, 0.5f);
        }

        public void MarkEliminatedThisRound(string reason)
        {
            IsEliminatedThisRound = true;
            EliminationReason = reason ?? "Eliminated";
            SetControlEnabled(false);
        }

        public void ClearRoundElimination()
        {
            IsEliminatedThisRound = false;
            EliminationReason = "";
            InWater = false;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (_invulnerableTimer > 0f) _invulnerableTimer = Mathf.Max(0f, _invulnerableTimer - dt);
            if (_introTuckTimer > 0f) _introTuckTimer = Mathf.Max(0f, _introTuckTimer - dt);
            if (_dashCooldownTimer > 0f) _dashCooldownTimer = Mathf.Max(0f, _dashCooldownTimer - dt);

            bool playing = GameManager.Instance != null && GameManager.Instance.State == GameState.Playing;
            bool active = playing && _controlEnabled && !InWater;
            if (!active)
            {
                _lateralInput = 0f;
                IsGliding = false;
                IsCrouching = false;
            }

            if (InWater)
            {
                // Sink slightly for the splash read, then the GameManager respawns us.
                transform.position += Vector3.down * (0.6f * dt);
                return;
            }

            if (_tumbleTimer > 0f)
            {
                UpdateTumble(dt);
                return;
            }

            if (_introTuckTimer > 0f) _runTime += dt;
            else if (active) _runTime += dt;

            ReadInput(active);
            UpdateDashing(active);
            UpdateCrouch(active);
            ApplyGliderAndGravity(active, dt);
            MoveFrog(dt, active);
            HandleWaterFallback();
        }

        void ReadInput(bool active)
        {
            _lateralInput = 0f;
            _forwardInput = 0f;
            _crouchHeld = false;
            if (!active) { ForwardInput = 0f; return; }

            if (InputSource == null)
            {
                // Default P1: WASD/arrows + mobile fallback.
                InputSource = new KeyboardPlayerInput { useMobileFallback = (PlayerIndex == 0) };
            }
            InputSource.Tick(true);

            _lateralInput = Mathf.Clamp(InputSource.Horizontal, -1f, 1f);
            _forwardInput = Mathf.Clamp(InputSource.Vertical, -1f, 1f);
            ForwardInput = _forwardInput;
            bool jumpHeld = InputSource.JumpHeld;
            bool jumpPressed = InputSource.JumpPressed;
            bool dashPressed = InputSource.DashPressed;
            bool crouchHeld = InputSource.CrouchHeld;

            if (jumpPressed && IsGrounded)
            {
                float jumpMult = 1f;
                var inv = GetComponent<FrogRunner.PowerUps.PowerUpInventory>();
                if (inv != null) jumpMult = inv.CurrentJumpMultiplier;
                _verticalVelocity = jumpSpeed * jumpMult;
                IsGrounded = false;
                _wasGrounded = false;
                _airTime = 0f;
                if (FrogRunner.Audio.AudioManager.Instance != null) FrogRunner.Audio.AudioManager.Instance.PlayHop();
            }

            if (jumpHeld && !IsGrounded && _airTime > glideActivateDelay && _verticalVelocity < 1f)
                IsGliding = true;
            else if (!jumpHeld)
                IsGliding = false;

            if (dashPressed && _dashCooldownTimer <= 0f)
            {
                _dashTimer = dashDuration;
                _dashCooldownTimer = dashCooldown;
                Vector3 dashDir = new Vector3(_lateralInput, 0f, _forwardInput);
                if (dashDir.sqrMagnitude < 0.01f) dashDir = Vector3.forward;
                _planarVelocity = dashDir.normalized * dashSpeed;
                if (IsGrounded && ChaseCamera.Instance != null) ChaseCamera.Instance.Shake(0.08f, 0.15f);
                if (FrogRunner.Audio.AudioManager.Instance != null) FrogRunner.Audio.AudioManager.Instance.PlayDash();
            }

            _crouchHeld = crouchHeld;
        }

        bool _crouchHeld;

        void UpdateDashing(bool active)
        {
            if (_dashTimer > 0f) _dashTimer = Mathf.Max(0f, _dashTimer - Time.deltaTime);
            if (!active) _dashTimer = 0f;
        }

        void UpdateCrouch(bool active)
        {
            IsCrouching = active && IsGrounded && _crouchHeld && _dashTimer <= 0f && _tumbleTimer <= 0f;
        }

        void ApplyGliderAndGravity(bool active, float dt)
        {
            if (IsGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            if (!IsGrounded) _airTime += dt;

            if (IsGliding && !IsGrounded)
            {
                _verticalVelocity += glideGravity * dt;
                if (_verticalVelocity < glideMaxFall) _verticalVelocity = glideMaxFall;
            }
            else
            {
                _verticalVelocity += gravity * dt;
                if (_verticalVelocity < -40f) _verticalVelocity = -40f;
            }

            if (!active) IsGliding = false;
        }

        void MoveFrog(float dt, bool active)
        {
            FloatingLog log = DetectLog();
            IsOnLog = log != null && (IsGrounded || transform.position.y <= log.TopY + 0.45f);
            if (IsOnLog && log != null)
            {
                _cc.Move(log.LastDelta);
                // Forgiving landing snap: keep frog on log surface.
                Vector3 lp = transform.position;
                if (lp.y < log.TopY) { lp.y = log.TopY; transform.position = lp; _verticalVelocity = Mathf.Max(_verticalVelocity, -1f); IsGrounded = true; }
            }

            float roundScale = 1f;
            var rm = FrogRunner.Rounds.RoundManager.Instance;
            if (rm != null) roundScale = 1f + (rm.Round - 1) * 0.045f; // R1->R10 escalation

            float speed = moveSpeed * roundScale;
            var inv = GetComponent<FrogRunner.PowerUps.PowerUpInventory>();
            if (inv != null) speed *= inv.CurrentSpeedMultiplier;
            if (IsCrouching) speed *= crouchSpeedMultiplier;
            if (IsGliding) speed += 1.8f;

            Vector3 inputDir = new Vector3(_lateralInput, 0f, _forwardInput);
            if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();
            float forwardMult = _forwardInput < 0f ? backwardSpeedMultiplier : 1f;
            Vector3 targetPlanar = active ? new Vector3(inputDir.x * speed, 0f, inputDir.z * speed * forwardMult) : Vector3.zero;

            if (_slideTimer > 0f)
            {
                _slideTimer = Mathf.Max(0f, _slideTimer - dt);
                _planarVelocity = Vector3.MoveTowards(_planarVelocity, targetPlanar, 4f * dt);
            }
            else
            {
                float accel = active ? acceleration : 28f;
                _planarVelocity = Vector3.MoveTowards(_planarVelocity, targetPlanar, accel * dt);
            }

            Vector3 motion = new Vector3(_planarVelocity.x, _verticalVelocity, _planarVelocity.z) * dt;
            _cc.Move(motion);

            Vector3 p = transform.position;
            float limit = Mathf.Min(playableHalfWidth, CourseBuilder.PlayableHalfWidth);
            if (Mathf.Abs(p.x) > limit) { p.x = Mathf.Clamp(p.x, -limit, limit); transform.position = p; }
            if (p.z < minZ) { p.z = minZ; transform.position = p; }

            _speed = new Vector3(_planarVelocity.x, 0f, _planarVelocity.z).magnitude;
            bool groundedNow = _cc.isGrounded || IsOnLog;
            if (groundedNow && _verticalVelocity < 0f) _verticalVelocity = -2f;
            IsGrounded = groundedNow;
            if (!groundedNow && _wasGrounded) _airTime = 0f;
            if (groundedNow && !_wasGrounded)
            {
                float impact = Mathf.Clamp01(Mathf.Abs(_verticalVelocity) / 18f);
                IsGliding = false;
                if (ChaseCamera.Instance != null) ChaseCamera.Instance.Shake(0.04f + impact * 0.12f, 0.16f);
            }
            _wasGrounded = groundedNow;
        }

        void UpdateTumble(float dt)
        {
            _tumbleTimer = Mathf.Max(0f, _tumbleTimer - dt);
            _hitVelocity = Vector3.MoveTowards(_hitVelocity, Vector3.zero, 5.5f * dt);
            _verticalVelocity += gravity * dt;
            Vector3 motion = new Vector3(_hitVelocity.x, _verticalVelocity, _hitVelocity.z) * dt;
            _cc.Move(motion);
            _planarVelocity = Vector3.MoveTowards(_planarVelocity, Vector3.zero, 8f * dt);
            _speed = new Vector3(_hitVelocity.x, 0f, _hitVelocity.z).magnitude;
            IsGrounded = _cc.isGrounded;
            if (IsGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            _wasGrounded = IsGrounded;
            HandleWaterFallback();
        }

        FloatingLog DetectLog()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            // Forgiving detection radius for wider logs.
            if (Physics.SphereCast(origin, 0.45f, Vector3.down, out RaycastHit hit, 1.2f, ~0, QueryTriggerInteraction.Ignore))
                return hit.collider.GetComponentInParent<FloatingLog>();
            return null;
        }

        void HandleWaterFallback()
        {
            if (InWater || CourseBuilder.Instance == null) return;
            if (!CourseBuilder.Instance.IsOverRiver(transform.position)) return;
            FloatingLog log = DetectLog();
            if (log != null && transform.position.y >= log.TopY - 0.25f) return;
            // More forgiving: only die when clearly below water.
            if (transform.position.y <= CourseBuilder.Instance.WaterLevel - 0.35f)
            {
                GameManager.Instance?.PlayerFellInWater(transform.position);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (InWater) return;

            var vehicle = other.GetComponent<Vehicle>();
            if (vehicle != null)
            {
                GameManager.Instance?.OnTrafficHit(vehicle.transform.position, this);
                return;
            }

            var water = other.GetComponent<WaterVolume>();
            if (water != null && transform.position.y <= water.WaterLevel + 0.35f)
            {
                GameManager.Instance?.PlayerFellInWater(transform.position, this);
            }
        }
    }
}
