using DG.Tweening;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;
        public float GlideMovementSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;

        [Range(0, 1)]
        public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;
        public float JumpPadHeight = 3.0f;

        public float WindBoostHeight = 0.5f;
        public float WindBoostSpeed = 5.0f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip(
            "Time required to pass before being able to jump again. Set to 0f to instantly jump again"
        )]
        public float JumpTimeout = 0.50f;

        [Tooltip(
            "Time required to pass before entering the fall state. Useful for walking down stairs"
        )]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip(
            "If the character is grounded or not. Not part of the CharacterController built in grounded check"
        )]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip(
            "The radius of the grounded check. Should match the radius of the CharacterController"
        )]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip(
            "The follow target set in the Cinemachine Virtual Camera that the camera will follow"
        )]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip(
            "Additional degress to override the camera. Useful for fine tuning camera position when locked"
        )]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        private bool isClimbing;
        private Vector3 lastGrabWallDirection;
        private Vector3 _wallNormal;

        public float GlidingGravity = -5.0f;
        private bool _isGliding = false;
        private bool _hasJumped = false;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDClimb;
        private int _animIDDirection;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        [SerializeField]
        private float glideCheck = 1.5f;

        [SerializeField]
        private bool _isGlidable = true;

        private bool _startWindEffect = false;

        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError(
                "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it"
            );
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            JumpAndGravity();
            GroundedCheck();
            Move();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDClimb = Animator.StringToHash("Climb");
            _animIDDirection = Animator.StringToHash("ClimbDirection");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(
                transform.position.x,
                transform.position.y - GroundedOffset,
                transform.position.z
            );
            Grounded = Physics.CheckSphere(
                spherePosition,
                GroundedRadius,
                GroundLayers,
                QueryTriggerInteraction.Ignore
            );

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(
                _cinemachineTargetYaw,
                float.MinValue,
                float.MaxValue
            );
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
                _cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw,
                0.0f
            );
        }

        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero)
                targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(
                _controller.velocity.x,
                0.0f,
                _controller.velocity.z
            ).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (
                currentHorizontalSpeed < targetSpeed - speedOffset
                || currentHorizontalSpeed > targetSpeed + speedOffset
            )
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(
                    currentHorizontalSpeed,
                    targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate
                );

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(
                _animationBlend,
                targetSpeed,
                Time.deltaTime * SpeedChangeRate
            );
            if (_animationBlend < 0.01f)
                _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation =
                    Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg
                    + _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y,
                    _targetRotation,
                    ref _rotationVelocity,
                    RotationSmoothTime
                );

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            Vector3 targetDirection =
                Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // Calculate normalized input direction based on camera orientation to ensure movement is relative to camera view
            Vector3 inputDirectionWorld =
                _mainCamera.transform.forward * inputDirection.z
                + _mainCamera.transform.right * inputDirection.x;
            inputDirectionWorld.y = 0; // Ensure we're only moving horizontally

            if (_isGliding)
            {
                // For gliding, we modify the target direction to include lateral movement based on input
                // This uses a simplified control, perhaps less speed than normal walking to simulate air resistance
                targetSpeed *= GlideMovementSpeed; // Assume GlideMovementSpeed is a fraction < 1 for slower movement
                targetDirection = inputDirectionWorld * targetSpeed;
            }
            else if (!isClimbing)
            {
                // Normal movement logic when not climbing or gliding
                if (_input.move != Vector2.zero)
                {
                    _targetRotation =
                        Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg
                        + _mainCamera.transform.eulerAngles.y;
                    float rotation = Mathf.SmoothDampAngle(
                        transform.eulerAngles.y,
                        _targetRotation,
                        ref _rotationVelocity,
                        RotationSmoothTime
                    );
                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }
                targetDirection =
                    Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward * targetSpeed;
            }

            //climbing function


            if (!isClimbing)
            {
                //not climbing any wall
                float avoidFloorDistance = 0.1f;
                float ladderGrabDistance = 0.4f;
                if (
                    Physics.Raycast(
                        transform.position + Vector3.up * avoidFloorDistance,
                        targetDirection,
                        out RaycastHit raycastHit,
                        ladderGrabDistance
                    )
                )
                {
                    if (raycastHit.transform.TryGetComponent(out Climbable climbable))
                    {
                        // Pass the hit normal to GrabWall
                        GrabWall(targetDirection, raycastHit.normal);
                    }
                }
            }
            else
            {
                //climbing the wall
                float avoidFloorDistance = 0.1f;
                float ladderGrabDistance = 0.4f;
                if (
                    Physics.Raycast(
                        transform.position + Vector3.up * avoidFloorDistance,
                        lastGrabWallDirection,
                        out RaycastHit raycastHit,
                        ladderGrabDistance
                    )
                )
                {
                    if (!raycastHit.transform.TryGetComponent(out Climbable climbable))
                    {
                        DropWall();
                        _verticalVelocity = 4f;
                        if (_hasAnimator)
                        {
                            _animator.SetBool(_animIDClimb, false);
                        }
                    }
                }
                else
                {
                    DropWall();
                    _verticalVelocity = 4f;
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDClimb, false);
                    }
                }

                if (Vector3.Dot(targetDirection, lastGrabWallDirection) < 0)
                {
                    //climbing down the wall
                    float wallDropDistance = 0.1f;
                    if (
                        Physics.Raycast(
                            transform.position,
                            Vector3.down,
                            out RaycastHit floorRaycastHit,
                            wallDropDistance
                        )
                    )
                    {
                        DropWall();
                        if (_hasAnimator)
                        {
                            _animator.SetBool(_animIDClimb, false);
                        }
                    }
                }
            }

            if (isClimbing)
            {
                // Adjust character rotation to face the wall
                Quaternion wallRotation = Quaternion.LookRotation(-_wallNormal);
                transform.rotation = Quaternion.Euler(
                    transform.rotation.eulerAngles.x,
                    wallRotation.eulerAngles.y,
                    transform.rotation.eulerAngles.z
                );

                targetDirection.y = targetDirection.z;
                targetDirection.z = 0f;
                targetDirection.x = 0f;
                _verticalVelocity = 0f;
                Grounded = true;
                _speed = targetSpeed;

                // //don't rotate when climbing
                // transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDClimb, true);
                    _animator.SetFloat(_animIDDirection, targetDirection.y);
                }
            }

            // move the player
            // _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
            //                  new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // Gliding and normal movement use the same method to move but with different target directions
            // When gliding, the vertical velocity is modified elsewhere, but we still apply the calculated targetDirection here
            _controller.Move(
                targetDirection.normalized * (_speed * Time.deltaTime)
                    + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime
            );

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void GrabWall(Vector3 directionToWall, Vector3 wallNormal)
        {
            isClimbing = true;
            this.lastGrabWallDirection = directionToWall;
            this._wallNormal = wallNormal; // Store the wall normal
        }

        private void DropWall()
        {
            isClimbing = false;
        }

        // private void JumpAndGravity()
        // {
        //     if (Grounded)
        //     {
        //         //rest gliding state when grounded
        //         _isGliding = false;

        //         // reset the fall timeout timer
        //         _fallTimeoutDelta = FallTimeout;

        //         // update animator if using character
        //         if (_hasAnimator)
        //         {
        //             _animator.SetBool(_animIDJump, false);
        //             _animator.SetBool(_animIDFreeFall, false);
        //             _animator.SetBool("isGliding", false);
        //         }

        //         // stop our velocity dropping infinitely when grounded
        //         if (_verticalVelocity < 0.0f)
        //         {
        //             _verticalVelocity = -2f;
        //         }

        //         // Jump
        //         if (_input.jump && _jumpTimeoutDelta <= 0.0f)
        //         {
        //             // the square root of H * -2 * G = how much velocity needed to reach desired height
        //             _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

        //             // update animator if using character
        //             if (_hasAnimator)
        //             {
        //                 _animator.SetBool(_animIDJump, true);
        //             }
        //         }

        //         // jump timeout
        //         if (_jumpTimeoutDelta >= 0.0f)
        //         {
        //             _jumpTimeoutDelta -= Time.deltaTime;
        //         }
        //     }
        //     else
        //     {
        //         if(!_isGliding && _input.jump && _jumpTimeoutDelta <= 0.0f)
        //         {
        //             _isGliding = true;
        //             _input.jump = false; // Prevent further jump checks while in the gliding state

        //             if (_hasAnimator)
        //             {
        //                 _animator.SetBool("isGliding", true); // Assume you have a gliding animation
        //             }
        //         }

        //         // Apply gliding gravity if gliding, else apply normal falling gravity
        //         float appliedGravity = _isGliding ? GlidingGravity : Gravity;
        //         _verticalVelocity += appliedGravity * Time.deltaTime;


        //         // // reset the jump timeout timer
        //         // _jumpTimeoutDelta = JumpTimeout;

        //         // // fall timeout
        //         // if (_fallTimeoutDelta >= 0.0f)
        //         // {
        //         //     _fallTimeoutDelta -= Time.deltaTime;
        //         // }
        //         // else
        //         // {
        //         //     // update animator if using character
        //         //     if (_hasAnimator)
        //         //     {
        //         //         _animator.SetBool(_animIDFreeFall, true);
        //         //     }
        //         // }

        //         // // if we are not grounded, do not jump
        //         // _input.jump = false;
        //     }

        //     // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        //     if (_verticalVelocity < _terminalVelocity)
        //     {
        //         // _verticalVelocity += Gravity * Time.deltaTime;
        //         // _verticalVelocity = Mathf.Max(_verticalVelocity, _terminalVelocity);
        //         _verticalVelocity += Gravity * Time.deltaTime;
        //         _verticalVelocity = Mathf.Max(_verticalVelocity, -_terminalVelocity); // Assuming down is negative
        //     }
        // }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _isGliding = false;
                _hasJumped = false; // Reset when grounded

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                    _animator.SetBool("isGliding", false);
                }

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                if (_input.jump && !_hasJumped)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    _hasJumped = true; // Player has initiated a jump

                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                    Debug.Log("Jump");
                    _input.jump = false; // Prevent further jump checks while in the gliding state
                }
            }
            else
            {
                if (_input.jump && !_isGliding && CheckIsGlidable() && _isGlidable)
                {
                    _isGliding = true;
                    _verticalVelocity = 0f; // Reset vertical velocity when gliding
                    if (_hasAnimator)
                    {
                        _animator.SetBool("isGliding", true);
                    }
                    _input.jump = false; // Prevent further jump checks while in the gliding state
                }
                else if (_input.jump && _isGliding)
                {
                    _isGliding = false;
                    Debug.Log("Stop gliding");

                    if (_hasAnimator)
                    {
                        _animator.SetBool("isGliding", false);
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                    _input.jump = false; // Prevent further jump checks while in the gliding state
                }
                else if (_input.jump)
                {
                    _input.jump = false;
                }
                _animator.SetBool(_animIDFreeFall, true);

                float appliedGravity = _isGliding ? GlidingGravity : Gravity;
                _verticalVelocity += appliedGravity * Time.deltaTime;

                if (_verticalVelocity < -_terminalVelocity) // Ensuring _verticalVelocity does not exceed terminal velocity
                {
                    _verticalVelocity = -_terminalVelocity;
                }
            }
        }

        public void JumpPadJump()
        {
            Grounded = true;
            _verticalVelocity = Mathf.Sqrt(JumpPadHeight * -2f * Gravity);
            _hasJumped = true; // Player has initiated a jump
        }

        public void WindEffect()
        {
            _hasJumped = true;
            //Move forward
            this.transform.DOMove(
                    this.transform.position
                        + new Vector3(0, WindBoostHeight, 0)
                        + this.transform.forward * WindBoostSpeed,
                    1f
                )
                .OnComplete(() =>
                {
                    _verticalVelocity = 0f;
                });
        }

        public void SetGlidable(bool boolean)
        {
            _isGlidable = boolean;
        }

        private bool CheckIsGlidable()
        {
            Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, glideCheck);
            // Debug.Log(hit.collider);
            return hit.collider == null;
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f)
                lfAngle += 360f;
            if (lfAngle > 360f)
                lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded)
                Gizmos.color = transparentGreen;
            else
                Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(
                    transform.position.x,
                    transform.position.y - GroundedOffset,
                    transform.position.z
                ),
                GroundedRadius
            );
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(
                        FootstepAudioClips[index],
                        transform.TransformPoint(_controller.center),
                        FootstepAudioVolume
                    );
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(
                    LandingAudioClip,
                    transform.TransformPoint(_controller.center),
                    FootstepAudioVolume
                );
            }
        }

        //Draw gizmos for glidable check
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, Vector3.down * glideCheck);
        }
    }
}
