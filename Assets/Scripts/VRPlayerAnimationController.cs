// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// Sample scripts are included only as examples and are not intended as production-ready.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Synty.AnimationBaseLocomotion.Samples
{
    public class VRPlayerAnimationController : MonoBehaviour
    {
        #region Enum

        public enum AnimationState
        {
            Base,
            Locomotion,
            Jump,
            Fall,
            Crouch
        }

        public enum GaitState
        {
            Idle,
            Walk,
            Run,
            Sprint
        }

        #endregion

        #region Animation Variable Hashes

        private readonly int movementInputTappedHash = Animator.StringToHash("MovementInputTapped");
        private readonly int movementInputPressedHash = Animator.StringToHash("MovementInputPressed");
        private readonly int movementInputHeldHash = Animator.StringToHash("MovementInputHeld");
        private readonly int shuffleDirectionXHash = Animator.StringToHash("ShuffleDirectionX");
        private readonly int shuffleDirectionZHash = Animator.StringToHash("ShuffleDirectionZ");

        private readonly int moveSpeedHash = Animator.StringToHash("MoveSpeed");
        private readonly int currentGaitHash = Animator.StringToHash("CurrentGait");

        private readonly int isJumpingAnimHash = Animator.StringToHash("IsJumping");
        private readonly int fallingDurationHash = Animator.StringToHash("FallingDuration");

        private readonly int inclineAngleHash = Animator.StringToHash("InclineAngle");

        private readonly int strafeDirectionXHash = Animator.StringToHash("StrafeDirectionX");
        private readonly int strafeDirectionZHash = Animator.StringToHash("StrafeDirectionZ");

        private readonly int forwardStrafeHash = Animator.StringToHash("ForwardStrafe");
        private readonly int cameraRotationOffsetHash = Animator.StringToHash("CameraRotationOffset");
        private readonly int isStrafingHash = Animator.StringToHash("IsStrafing");
        private readonly int isTurningInPlaceHash = Animator.StringToHash("IsTurningInPlace");

        private readonly int isCrouchingHash = Animator.StringToHash("IsCrouching");

        private readonly int isWalkingHash = Animator.StringToHash("IsWalking");
        private readonly int isStoppedHash = Animator.StringToHash("IsStopped");
        private readonly int isStartingHash = Animator.StringToHash("IsStarting");

        private readonly int isGroundedHash = Animator.StringToHash("IsGrounded");

        private readonly int leanValueHash = Animator.StringToHash("LeanValue");
        private readonly int headLookXHash = Animator.StringToHash("HeadLookX");
        private readonly int headLookYHash = Animator.StringToHash("HeadLookY");

        private readonly int bodyLookXHash = Animator.StringToHash("BodyLookX");
        private readonly int bodyLookYHash = Animator.StringToHash("BodyLookY");

        private readonly int locomotionStartDirectionHash = Animator.StringToHash("LocomotionStartDirection");

        #endregion

        #region Player Settings Variables

        #region Scripts/Objects

        [Header("External Components")]
        [Tooltip("Camera Offset transform (from XR Origin)")]
        [SerializeField]
        private Transform cameraOffset;
        [Tooltip("Main Camera transform for getting camera direction")]
        [SerializeField]
        private Camera mainCamera;
        [Tooltip("Animator component for controlling player animations")]
        [SerializeField]
        private Animator playerAnimator;
        [Tooltip("Character Controller component for controlling player movement")]
        [SerializeField]
        private CharacterController characterController;

        [Header("VR Input")]
        [Tooltip("Left hand XR controller")]
        [SerializeField]
        private XRNode leftHandNode = XRNode.LeftHand;
        [Tooltip("Right hand XR controller")]
        [SerializeField]
        private XRNode rightHandNode = XRNode.RightHand;
        [Tooltip("Primary input axis for movement (usually left thumbstick)")]
        [SerializeField]
        private InputFeatureUsage<Vector2> primaryAxis = CommonUsages.primary2DAxis;
        [Tooltip("Jump button input")]
        [SerializeField]
        private InputFeatureUsage<bool> jumpButton = CommonUsages.primaryButton;
        [Tooltip("Crouch button input")]
        [SerializeField]
        private InputFeatureUsage<bool> crouchButton = CommonUsages.secondaryButton;
        [Tooltip("Sprint grip input")]
        [SerializeField]
        private InputFeatureUsage<float> sprintGrip = CommonUsages.grip;

        #endregion

        #region Locomotion Settings

        [Header("Player Locomotion")]
        [Header("Main Settings")]
        [Tooltip("Whether the character always faces the camera facing direction")]
        [SerializeField]
        private bool alwaysStrafe = true;
        [Tooltip("Slowest movement speed of the player when set to a walk state")]
        [SerializeField]
        private float walkSpeed = 1.4f;
        [Tooltip("Default movement speed of the player")]
        [SerializeField]
        private float runSpeed = 2.5f;
        [Tooltip("Top movement speed of the player")]
        [SerializeField]
        private float sprintSpeed = 7f;
        [Tooltip("Damping factor for changing speed")]
        [SerializeField]
        private float speedChangeDamping = 10f;
        [Tooltip("Rotation smoothing factor")]
        [SerializeField]
        private float rotationSmoothing = 10f;
        [Tooltip("Offset for camera rotation")]
        [SerializeField]
        private float cameraRotationOffset;

        [Header("VR Specific Settings")]
        [Tooltip("Grip threshold for sprinting")]
        [SerializeField]
        private float sprintGripThreshold = 0.7f;
        [Tooltip("Movement deadzone for thumbstick")]
        [SerializeField]
        private float movementDeadzone = 0.1f;
        [Tooltip("Body rotation speed when using right thumbstick")]
        [SerializeField]
        private float bodyRotationSpeed = 90f;
        [Tooltip("Snap turn angle (0 = smooth turn)")]
        [SerializeField]
        private float snapTurnAngle = 30f;
        [Tooltip("Use snap turning instead of smooth turning")]
        [SerializeField]
        private bool useSnapTurn = false;

        // 新增：相机旋转控制
        [Header("Camera Rotation Settings")]
        [Tooltip("是否同时旋转相机视角")]
        [SerializeField]
        private bool rotateCameraWithBody = true;

        [Tooltip("XR Origin transform（如果使用XR系统）")]
        [SerializeField]
        private Transform xrOrigin;

        #endregion

        #region Shuffle Settings

        [Header("Shuffles")]
        [Tooltip("Threshold for button hold duration")]
        [SerializeField]
        private float buttonHoldThreshold = 0.15f;
        [Tooltip("Direction of shuffling on the X-axis")]
        [SerializeField]
        private float shuffleDirectionX;
        [Tooltip("Direction of shuffling on the Z-axis")]
        [SerializeField]
        private float shuffleDirectionZ;

        #endregion

        #region Capsule Settings

        [Header("Capsule Values")]
        [Tooltip("Standing height of the player capsule")]
        [SerializeField]
        private float capsuleStandingHeight = 1.8f;
        [Tooltip("Standing center of the player capsule")]
        [SerializeField]
        private float capsuleStandingCentre = 0.93f;
        [Tooltip("Crouching height of the player capsule")]
        [SerializeField]
        private float capsuleCrouchingHeight = 1.2f;
        [Tooltip("Crouching center of the player capsule")]
        [SerializeField]
        private float capsuleCrouchingCentre = 0.6f;

        #endregion

        #region Strafing

        [Header("Player Strafing")]
        [Tooltip("Minimum threshold for forward strafing angle")]
        [SerializeField]
        private float forwardStrafeMinThreshold = -55.0f;
        [Tooltip("Maximum threshold for forward strafing angle")]
        [SerializeField]
        private float forwardStrafeMaxThreshold = 125.0f;
        [Tooltip("Current forward strafing value")]
        [SerializeField]
        private float forwardStrafe = 1f;

        #endregion

        #region Grounded Settings

        [Header("Grounded Angle")]
        [Tooltip("Position of the rear ray for grounded angle check")]
        [SerializeField]
        private Transform rearRayPos;
        [Tooltip("Position of the front ray for grounded angle check")]
        [SerializeField]
        private Transform frontRayPos;
        [Tooltip("Layer mask for checking ground")]
        [SerializeField]
        private LayerMask groundLayerMask;
        [Tooltip("Current incline angle")]
        [SerializeField]
        private float inclineAngle;
        [Tooltip("Useful for rough ground")]
        [SerializeField]
        private float groundedOffset = -0.14f;

        #endregion

        #region In-Air Settings

        [Header("Player In-Air")]
        [Tooltip("Force applied when the player jumps")]
        [SerializeField]
        private float jumpForce = 10f;
        [Tooltip("Multiplier for gravity when in the air")]
        [SerializeField]
        private float gravityMultiplier = 2f;
        [Tooltip("Duration of falling")]
        [SerializeField]
        private float fallingDuration;

        #endregion

        #region Head Look Settings

        [Header("Player Head Look")]
        [Tooltip("Flag indicating if head turning is enabled")]
        [SerializeField]
        private bool enableHeadTurn = true;
        [Tooltip("Delay for head turning")]
        [SerializeField]
        private float headLookDelay;
        [Tooltip("X-axis value for head turning")]
        [SerializeField]
        private float headLookX;
        [Tooltip("Y-axis value for head turning")]
        [SerializeField]
        private float headLookY;
        [Tooltip("Curve for X-axis head turning")]
        [SerializeField]
        private AnimationCurve headLookXCurve;

        #endregion

        #region Body Look Settings

        [Header("Player Body Look")]
        [Tooltip("Flag indicating if body turning is enabled")]
        [SerializeField]
        private bool enableBodyTurn = true;
        [Tooltip("Delay for body turning")]
        [SerializeField]
        private float bodyLookDelay;
        [Tooltip("X-axis value for body turning")]
        [SerializeField]
        private float bodyLookX;
        [Tooltip("Y-axis value for body turning")]
        [SerializeField]
        private float bodyLookY;
        [Tooltip("Curve for X-axis body turning")]
        [SerializeField]
        private AnimationCurve bodyLookXCurve;

        #endregion

        #region Lean Settings

        [Header("Player Lean")]
        [Tooltip("Flag indicating if leaning is enabled")]
        [SerializeField]
        private bool enableLean = true;
        [Tooltip("Delay for leaning")]
        [SerializeField]
        private float leanDelay;
        [Tooltip("Current value for leaning")]
        [SerializeField]
        private float leanValue;
        [Tooltip("Curve for leaning")]
        [SerializeField]
        private AnimationCurve leanCurve;
        [Tooltip("Delay for head leaning looks")]
        [SerializeField]
        private float leansHeadLooksDelay;
        [Tooltip("Flag indicating if an animation clip has ended")]
        [SerializeField]
        private bool animationClipEnd;

        #endregion

        #endregion

        #region Runtime Properties

        private readonly List<GameObject> currentTargetCandidates = new List<GameObject>();
        private AnimationState currentState = AnimationState.Base;
        private bool cannotStandUp;
        private bool crouchKeyPressed;
        private bool isAiming;
        private bool isCrouching;
        private bool isGrounded = true;
        private bool isLockedOn;
        private bool isSliding;
        private bool isSprinting;
        private bool isStarting;
        private bool isStopped = true;
        private bool isStrafing;
        private bool isTurningInPlace;
        private bool isWalking;
        private bool movementInputHeld;
        private bool movementInputPressed;
        private bool movementInputTapped;
        private float currentMaxSpeed;
        private float locomotionStartDirection;
        private float locomotionStartTimer;
        private float lookingAngle;
        private float newDirectionDifferenceAngle;
        private float speed2D;
        private float strafeAngle;
        private float strafeDirectionX;
        private float strafeDirectionZ;
        private GameObject currentLockOnTarget;
        private GaitState currentGait;
        private Transform targetLockOnPos;
        private Vector3 currentRotation = new Vector3(0f, 0f, 0f);
        private Vector3 moveDirection;
        private Vector3 previousRotation;
        private Vector3 velocity;

        // VR Input state
        private Vector2 leftThumbstick;
        private Vector2 rightThumbstick;
        private bool jumpButtonPressed;
        private bool crouchButtonPressed;
        private float leftGripValue;
        private float rightGripValue;
        private float movementInputDuration;

        // VR body rotation
        private bool snapTurnTriggered;
        private float lastSnapTurnTime;

        // 新增：右摇杆旋转优先级控制
        private float lastThumbstickRotationTime = 0f;
        private bool isUsingThumbstickRotation = false;

        #endregion

        #region Base State Variables

        private const float ANIMATION_DAMP_TIME = 5f;
        private const float STRAFE_DIRECTION_DAMP_TIME = 20f;
        private float targetMaxSpeed;
        private float fallStartTime;
        private float rotationRate;
        private float initialLeanValue;
        private float initialTurnValue;
        private Vector3 cameraForward;
        private Vector3 targetVelocity;

        #endregion

        #region Animation Controller

        #region Start

        private void Start()
        {
            targetLockOnPos = transform.Find("TargetLockOnPos");

            if (targetLockOnPos == null)
            {
                GameObject lockOnTarget = new GameObject("TargetLockOnPos");
                lockOnTarget.transform.SetParent(transform);
                lockOnTarget.transform.localPosition = Vector3.zero;
                targetLockOnPos = lockOnTarget.transform;
            }

            isStrafing = alwaysStrafe;

            // 新增：自动查找XR Origin
            if (xrOrigin == null)
            {
                xrOrigin = FindXROrigin();
            }

            SwitchState(AnimationState.Locomotion);
        }

        /// <summary>
        /// 自动查找XR Origin
        /// </summary>
        private Transform FindXROrigin()
        {
            GameObject xrOriginObj = GameObject.Find("XR Origin");
            if (xrOriginObj == null)
            {
                xrOriginObj = GameObject.Find("XR Rig");
            }

            if (xrOriginObj != null)
            {
                return xrOriginObj.transform;
            }

            if (cameraOffset != null)
            {
                Transform parent = cameraOffset.parent;
                while (parent != null)
                {
                    if (parent.name.Contains("XR Origin") || parent.name.Contains("XR Rig"))
                    {
                        return parent;
                    }
                    parent = parent.parent;
                }
            }

            return null;
        }

        #endregion

        #region VR Input

        private void UpdateVRInput()
        {
            InputDevice leftDevice = InputDevices.GetDeviceAtXRNode(leftHandNode);
            if (leftDevice.isValid)
            {
                leftDevice.TryGetFeatureValue(primaryAxis, out leftThumbstick);
                leftDevice.TryGetFeatureValue(jumpButton, out jumpButtonPressed);
                leftDevice.TryGetFeatureValue(crouchButton, out crouchButtonPressed);
                leftDevice.TryGetFeatureValue(sprintGrip, out leftGripValue);
            }

            InputDevice rightDevice = InputDevices.GetDeviceAtXRNode(rightHandNode);
            if (rightDevice.isValid)
            {
                rightDevice.TryGetFeatureValue(primaryAxis, out rightThumbstick);
                rightDevice.TryGetFeatureValue(sprintGrip, out rightGripValue);
            }

            ProcessBodyRotation();

            if (jumpButtonPressed)
            {
                OnJumpPressed();
            }

            if (crouchButtonPressed != crouchKeyPressed)
            {
                if (crouchButtonPressed)
                {
                    ActivateCrouch();
                }
                else
                {
                    DeactivateCrouch();
                }
            }

            bool shouldSprint = (leftGripValue > sprintGripThreshold) || (rightGripValue > sprintGripThreshold);
            if (shouldSprint && !isSprinting)
            {
                ActivateSprint();
            }
            else if (!shouldSprint && isSprinting)
            {
                DeactivateSprint();
            }
        }

        /// <summary>
        /// 修正版本：旋转整个角色和相机视角，支持优先级控制
        /// </summary>
        private void ProcessBodyRotation()
        {
            if (rightThumbstick.magnitude < movementDeadzone)
            {
                snapTurnTriggered = false;
                isUsingThumbstickRotation = false;
                return;
            }

            isUsingThumbstickRotation = true;
            lastThumbstickRotationTime = Time.time;

            if (useSnapTurn)
            {
                if (!snapTurnTriggered && Time.time - lastSnapTurnTime > 0.3f)
                {
                    if (rightThumbstick.x > 0.7f)
                    {
                        RotatePlayerAndCamera(snapTurnAngle);
                        snapTurnTriggered = true;
                        lastSnapTurnTime = Time.time;
                    }
                    else if (rightThumbstick.x < -0.7f)
                    {
                        RotatePlayerAndCamera(-snapTurnAngle);
                        snapTurnTriggered = true;
                        lastSnapTurnTime = Time.time;
                    }
                }
            }
            else
            {
                if (bodyRotationSpeed > 0)
                {
                    float rotationInput = rightThumbstick.x;
                    float rotationAmount = rotationInput * bodyRotationSpeed * Time.deltaTime;
                    RotatePlayerAndCamera(rotationAmount);
                }
            }
        }

        private void RotatePlayerAndCamera(float angle)
        {
            if (!rotateCameraWithBody)
            {
                transform.Rotate(0, angle, 0);
                return;
            }

            if (xrOrigin != null)
            {
                xrOrigin.Rotate(0, angle, 0);
                transform.Rotate(0, angle, 0);
            }
            else if (cameraOffset != null)
            {
                transform.Rotate(0, angle, 0);
                cameraOffset.Rotate(0, angle, 0);
            }
            else if (mainCamera != null)
            {
                transform.Rotate(0, angle, 0);
                mainCamera.transform.Rotate(0, angle, 0);
            }
            else
            {
                transform.Rotate(0, angle, 0);
            }
        }

        private void RotateBody(float angle)
        {
            RotatePlayerAndCamera(angle);
        }

        private void OnJumpPressed()
        {
            switch (currentState)
            {
                case AnimationState.Locomotion:
                    LocomotionToJumpState();
                    break;
                case AnimationState.Crouch:
                    CrouchToJumpState();
                    break;
            }
        }

        public Vector2 GetMovementInput()
        {
            Vector2 input = leftThumbstick;
            if (input.magnitude < movementDeadzone)
            {
                input = Vector2.zero;
            }
            return input;
        }

        public bool IsMovementInputDetected()
        {
            return GetMovementInput().magnitude > 0f;
        }

        #region VR Camera Helper Methods

        private Vector3 GetCameraForwardZeroedYNormalised()
        {
            if (mainCamera != null)
            {
                Vector3 forward = mainCamera.transform.forward;
                forward.y = 0;
                return forward.normalized;
            }
            return Vector3.forward;
        }

        private Vector3 GetCameraRightZeroedYNormalised()
        {
            if (mainCamera != null)
            {
                Vector3 right = mainCamera.transform.right;
                right.y = 0;
                return right.normalized;
            }
            return Vector3.right;
        }

        private Vector3 GetCameraPosition()
        {
            if (mainCamera != null)
            {
                return mainCamera.transform.position;
            }
            else if (cameraOffset != null)
            {
                return cameraOffset.position;
            }
            return transform.position;
        }

        private Vector3 GetCameraForward()
        {
            if (mainCamera != null)
            {
                return mainCamera.transform.forward;
            }
            return Vector3.forward;
        }

        private float GetCameraTiltX()
        {
            if (mainCamera != null)
            {
                return mainCamera.transform.eulerAngles.x;
            }
            return 0f;
        }

        #endregion

        private void ActivateAim()
        {
            isAiming = true;
            isStrafing = !isSprinting;
        }

        private void DeactivateAim()
        {
            isAiming = false;
            isStrafing = !isSprinting && (alwaysStrafe || isLockedOn);
        }

        public void AddTargetCandidate(GameObject newTarget)
        {
            if (newTarget != null)
            {
                currentTargetCandidates.Add(newTarget);
            }
        }

        public void RemoveTarget(GameObject targetToRemove)
        {
            if (currentTargetCandidates.Contains(targetToRemove))
            {
                currentTargetCandidates.Remove(targetToRemove);
            }
        }

        public void ToggleLockOn()
        {
            EnableLockOn(!isLockedOn);
        }

        private void EnableLockOn(bool enable)
        {
            isLockedOn = enable;
            isStrafing = false;
            isStrafing = enable ? !isSprinting : alwaysStrafe || isAiming;

            if (enable && currentLockOnTarget != null)
            {
                var lockOnComponent = currentLockOnTarget.GetComponent<SampleObjectLockOn>();
                if (lockOnComponent != null)
                {
                    lockOnComponent.Highlight(true, true);
                }
            }
        }

        #endregion

        #region Walking State

        public void ToggleWalk()
        {
            EnableWalk(!isWalking);
        }

        private void EnableWalk(bool enable)
        {
            isWalking = enable && isGrounded && !isSprinting;
        }

        #endregion

        #region Sprinting State

        private void ActivateSprint()
        {
            if (!isCrouching)
            {
                EnableWalk(false);
                isSprinting = true;
                isStrafing = false;
            }
        }

        private void DeactivateSprint()
        {
            isSprinting = false;
            if (alwaysStrafe || isAiming || isLockedOn)
            {
                isStrafing = true;
            }
        }

        #endregion

        #region Crouching State

        private void ActivateCrouch()
        {
            crouchKeyPressed = true;
            if (isGrounded)
            {
                CapsuleCrouchingSize(true);
                DeactivateSprint();
                isCrouching = true;
            }
        }

        private void DeactivateCrouch()
        {
            crouchKeyPressed = false;
            if (!cannotStandUp && !isSliding)
            {
                CapsuleCrouchingSize(false);
                isCrouching = false;
            }
        }

        public void ActivateSliding()
        {
            isSliding = true;
        }

        public void DeactivateSliding()
        {
            isSliding = false;
        }

        private void CapsuleCrouchingSize(bool crouching)
        {
            if (crouching)
            {
                characterController.center = new Vector3(0f, capsuleCrouchingCentre, 0f);
                characterController.height = capsuleCrouchingHeight;
            }
            else
            {
                characterController.center = new Vector3(0f, capsuleStandingCentre, 0f);
                characterController.height = capsuleStandingHeight;
            }
        }

        #endregion

        #endregion

        #region Shared State

        #region State Change

        private void SwitchState(AnimationState newState)
        {
            ExitCurrentState();
            EnterState(newState);
        }

        private void EnterState(AnimationState stateToEnter)
        {
            currentState = stateToEnter;
            switch (currentState)
            {
                case AnimationState.Base:
                    EnterBaseState();
                    break;
                case AnimationState.Locomotion:
                    EnterLocomotionState();
                    break;
                case AnimationState.Jump:
                    EnterJumpState();
                    break;
                case AnimationState.Fall:
                    EnterFallState();
                    break;
                case AnimationState.Crouch:
                    EnterCrouchState();
                    break;
            }
        }

        private void ExitCurrentState()
        {
            switch (currentState)
            {
                case AnimationState.Locomotion:
                    ExitLocomotionState();
                    break;
                case AnimationState.Jump:
                    ExitJumpState();
                    break;
                case AnimationState.Crouch:
                    ExitCrouchState();
                    break;
            }
        }

        #endregion

        #region Updates

        private void Update()
        {
            UpdateVRInput();

            switch (currentState)
            {
                case AnimationState.Locomotion:
                    UpdateLocomotionState();
                    break;
                case AnimationState.Jump:
                    UpdateJumpState();
                    break;
                case AnimationState.Fall:
                    UpdateFallState();
                    break;
                case AnimationState.Crouch:
                    UpdateCrouchState();
                    break;
            }
        }

        private void UpdateAnimatorController()
        {
            playerAnimator.SetFloat(leanValueHash, leanValue);
            playerAnimator.SetFloat(headLookXHash, headLookX);
            playerAnimator.SetFloat(headLookYHash, headLookY);
            playerAnimator.SetFloat(bodyLookXHash, bodyLookX);
            playerAnimator.SetFloat(bodyLookYHash, bodyLookY);

            playerAnimator.SetFloat(isStrafingHash, isStrafing ? 1.0f : 0.0f);
            playerAnimator.SetFloat(inclineAngleHash, inclineAngle);
            playerAnimator.SetFloat(moveSpeedHash, speed2D);
            playerAnimator.SetInteger(currentGaitHash, (int)currentGait);

            playerAnimator.SetFloat(strafeDirectionXHash, strafeDirectionX);
            playerAnimator.SetFloat(strafeDirectionZHash, strafeDirectionZ);
            playerAnimator.SetFloat(forwardStrafeHash, forwardStrafe);
            playerAnimator.SetFloat(cameraRotationOffsetHash, cameraRotationOffset);

            playerAnimator.SetBool(movementInputHeldHash, movementInputHeld);
            playerAnimator.SetBool(movementInputPressedHash, movementInputPressed);
            playerAnimator.SetBool(movementInputTappedHash, movementInputTapped);
            playerAnimator.SetFloat(shuffleDirectionXHash, shuffleDirectionX);
            playerAnimator.SetFloat(shuffleDirectionZHash, shuffleDirectionZ);

            playerAnimator.SetBool(isTurningInPlaceHash, isTurningInPlace);
            playerAnimator.SetBool(isCrouchingHash, isCrouching);

            playerAnimator.SetFloat(fallingDurationHash, fallingDuration);
            playerAnimator.SetBool(isGroundedHash, isGrounded);

            playerAnimator.SetBool(isWalkingHash, isWalking);
            playerAnimator.SetBool(isStoppedHash, isStopped);

            playerAnimator.SetFloat(locomotionStartDirectionHash, locomotionStartDirection);
        }

        #endregion

        #endregion

        #region Base State

        #region Setup

        private void EnterBaseState()
        {
            previousRotation = transform.forward;
        }

        private void CalculateInput()
        {
            bool movementDetected = IsMovementInputDetected();

            if (movementDetected)
            {
                if (movementInputDuration == 0)
                {
                    movementInputTapped = true;
                }
                else if (movementInputDuration > 0 && movementInputDuration < buttonHoldThreshold)
                {
                    movementInputTapped = false;
                    movementInputPressed = true;
                    movementInputHeld = false;
                }
                else
                {
                    movementInputTapped = false;
                    movementInputPressed = false;
                    movementInputHeld = true;
                }

                movementInputDuration += Time.deltaTime;
            }
            else
            {
                movementInputDuration = 0;
                movementInputTapped = false;
                movementInputPressed = false;
                movementInputHeld = false;
            }

            Vector2 moveInput = GetMovementInput();
            Vector3 bodyForward = transform.forward;
            Vector3 bodyRight = transform.right;

            bodyForward.y = 0;
            bodyRight.y = 0;
            bodyForward.Normalize();
            bodyRight.Normalize();

            moveDirection = (bodyForward * moveInput.y) + (bodyRight * moveInput.x);
        }

        #endregion

        #region Movement

        private void Move()
        {
            characterController.Move(velocity * Time.deltaTime);

            if (isLockedOn)
            {
                if (currentLockOnTarget != null)
                {
                    targetLockOnPos.position = currentLockOnTarget.transform.position;
                }
            }
        }

        private void ApplyGravity()
        {
            if (velocity.y > Physics.gravity.y)
            {
                velocity.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
            }
        }

        private void CalculateMoveDirection()
        {
            CalculateInput();

            if (!isGrounded)
            {
                targetMaxSpeed = currentMaxSpeed;
            }
            else if (isCrouching)
            {
                targetMaxSpeed = walkSpeed;
            }
            else if (isSprinting)
            {
                targetMaxSpeed = sprintSpeed;
            }
            else if (isWalking)
            {
                targetMaxSpeed = walkSpeed;
            }
            else
            {
                targetMaxSpeed = runSpeed;
            }

            currentMaxSpeed = Mathf.Lerp(currentMaxSpeed, targetMaxSpeed, ANIMATION_DAMP_TIME * Time.deltaTime);

            targetVelocity.x = moveDirection.x * currentMaxSpeed;
            targetVelocity.z = moveDirection.z * currentMaxSpeed;

            velocity.z = Mathf.Lerp(velocity.z, targetVelocity.z, speedChangeDamping * Time.deltaTime);
            velocity.x = Mathf.Lerp(velocity.x, targetVelocity.x, speedChangeDamping * Time.deltaTime);

            speed2D = new Vector3(velocity.x, 0f, velocity.z).magnitude;
            speed2D = Mathf.Round(speed2D * 1000f) / 1000f;

            Vector3 playerForwardVector = transform.forward;

            newDirectionDifferenceAngle = playerForwardVector != moveDirection
                ? Vector3.SignedAngle(playerForwardVector, moveDirection, Vector3.up)
                : 0f;

            CalculateGait();
        }

        private void CalculateGait()
        {
            float runThreshold = (walkSpeed + runSpeed) / 2;
            float sprintThreshold = (runSpeed + sprintSpeed) / 2;

            if (speed2D < 0.01)
            {
                currentGait = GaitState.Idle;
            }
            else if (speed2D < runThreshold)
            {
                currentGait = GaitState.Walk;
            }
            else if (speed2D < sprintThreshold)
            {
                currentGait = GaitState.Run;
            }
            else
            {
                currentGait = GaitState.Sprint;
            }
        }

        private void FaceMoveDirection()
        {
            Vector3 characterForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
            Vector3 characterRight = new Vector3(transform.right.x, 0f, transform.right.z).normalized;
            Vector3 directionForward = new Vector3(moveDirection.x, 0f, moveDirection.z).normalized;

            strafeAngle = characterForward != directionForward ? Vector3.SignedAngle(characterForward, directionForward, Vector3.up) : 0f;

            isTurningInPlace = false;

            if (isStrafing)
            {
                if (moveDirection.magnitude > 0.01)
                {
                    shuffleDirectionZ = Vector3.Dot(characterForward, directionForward);
                    shuffleDirectionX = Vector3.Dot(characterRight, directionForward);

                    UpdateStrafeDirection(
                        Vector3.Dot(characterForward, directionForward),
                        Vector3.Dot(characterRight, directionForward)
                    );

                    float targetValue = strafeAngle > forwardStrafeMinThreshold && strafeAngle < forwardStrafeMaxThreshold ? 1f : 0f;

                    if (Mathf.Abs(forwardStrafe - targetValue) <= 0.001f)
                    {
                        forwardStrafe = targetValue;
                    }
                    else
                    {
                        float t = Mathf.Clamp01(STRAFE_DIRECTION_DAMP_TIME * Time.deltaTime);
                        forwardStrafe = Mathf.SmoothStep(forwardStrafe, targetValue, t);
                    }
                }
                else
                {
                    UpdateStrafeDirection(1f, 0f);
                }
            }
            else
            {
                UpdateStrafeDirection(1f, 0f);

                shuffleDirectionZ = 1;
                shuffleDirectionX = 0;

                Vector3 faceDirection = new Vector3(velocity.x, 0f, velocity.z);

                if (faceDirection.magnitude > 0.1f)
                {
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(faceDirection),
                        rotationSmoothing * Time.deltaTime
                    );
                }
            }
        }

        private void CheckIfStopped()
        {
            isStopped = moveDirection.magnitude == 0 && speed2D < .5;
        }

        private void CheckIfStarting()
        {
            locomotionStartTimer = VariableOverrideDelayTimer(locomotionStartTimer);

            bool isStartingCheck = false;

            if (locomotionStartTimer <= 0.0f)
            {
                if (moveDirection.magnitude > 0.01 && speed2D < 1 && !isStrafing)
                {
                    isStartingCheck = true;
                }

                if (isStartingCheck)
                {
                    if (!isStarting)
                    {
                        locomotionStartDirection = newDirectionDifferenceAngle;
                        playerAnimator.SetFloat(locomotionStartDirectionHash, locomotionStartDirection);
                    }

                    float delayTime = 0.2f;
                    leanDelay = delayTime;
                    headLookDelay = delayTime;
                    bodyLookDelay = delayTime;

                    locomotionStartTimer = delayTime;
                }
            }
            else
            {
                isStartingCheck = true;
            }

            isStarting = isStartingCheck;
            playerAnimator.SetBool(isStartingHash, isStarting);
        }

        private void UpdateStrafeDirection(float TargetZ, float TargetX)
        {
            strafeDirectionZ = Mathf.Lerp(strafeDirectionZ, TargetZ, ANIMATION_DAMP_TIME * Time.deltaTime);
            strafeDirectionX = Mathf.Lerp(strafeDirectionX, TargetX, ANIMATION_DAMP_TIME * Time.deltaTime);
            strafeDirectionZ = Mathf.Round(strafeDirectionZ * 1000f) / 1000f;
            strafeDirectionX = Mathf.Round(strafeDirectionX * 1000f) / 1000f;
        }

        #endregion

        #region Ground Checks

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(
                characterController.transform.position.x,
                characterController.transform.position.y - groundedOffset,
                characterController.transform.position.z
            );
            isGrounded = Physics.CheckSphere(spherePosition, characterController.radius, groundLayerMask, QueryTriggerInteraction.Ignore);

            if (isGrounded)
            {
                GroundInclineCheck();
            }
        }

        private void GroundInclineCheck()
        {
            float rayDistance = Mathf.Infinity;
            rearRayPos.rotation = Quaternion.Euler(transform.rotation.x, 0, 0);
            frontRayPos.rotation = Quaternion.Euler(transform.rotation.x, 0, 0);

            Physics.Raycast(rearRayPos.position, rearRayPos.TransformDirection(-Vector3.up), out RaycastHit rearHit, rayDistance, groundLayerMask);
            Physics.Raycast(
                frontRayPos.position,
                frontRayPos.TransformDirection(-Vector3.up),
                out RaycastHit frontHit,
                rayDistance,
                groundLayerMask
            );

            Vector3 hitDifference = frontHit.point - rearHit.point;
            float xPlaneLength = new Vector2(hitDifference.x, hitDifference.z).magnitude;

            inclineAngle = Mathf.Lerp(inclineAngle, Mathf.Atan2(hitDifference.y, xPlaneLength) * Mathf.Rad2Deg, 20f * Time.deltaTime);
        }

        private void CeilingHeightCheck()
        {
            float rayDistance = Mathf.Infinity;
            float minimumStandingHeight = capsuleStandingHeight - frontRayPos.localPosition.y;

            Vector3 midpoint = new Vector3(transform.position.x, transform.position.y + frontRayPos.localPosition.y, transform.position.z);
            if (Physics.Raycast(midpoint, transform.TransformDirection(Vector3.up), out RaycastHit ceilingHit, rayDistance, groundLayerMask))
            {
                cannotStandUp = ceilingHit.distance < minimumStandingHeight;
            }
            else
            {
                cannotStandUp = false;
            }
        }

        #endregion

        #region Falling

        private void ResetFallingDuration()
        {
            fallStartTime = Time.time;
            fallingDuration = 0f;
        }

        private void UpdateFallingDuration()
        {
            fallingDuration = Time.time - fallStartTime;
        }

        #endregion

        #region Checks

        private void CheckEnableTurns()
        {
            headLookDelay = VariableOverrideDelayTimer(headLookDelay);
            enableHeadTurn = headLookDelay == 0.0f && !isStarting;
            bodyLookDelay = VariableOverrideDelayTimer(bodyLookDelay);
            enableBodyTurn = bodyLookDelay == 0.0f && !(isStarting || isTurningInPlace);
        }

        private void CheckEnableLean()
        {
            leanDelay = VariableOverrideDelayTimer(leanDelay);
            enableLean = leanDelay == 0.0f && !(isStarting || isTurningInPlace);
        }

        #endregion

        #region Lean and Offsets

        private void CalculateRotationalAdditives(bool leansActivated, bool headLookActivated, bool bodyLookActivated)
        {
            if (headLookActivated || leansActivated || bodyLookActivated)
            {
                currentRotation = transform.forward;

                rotationRate = currentRotation != previousRotation
                    ? Vector3.SignedAngle(currentRotation, previousRotation, Vector3.up) / Time.deltaTime * -1f
                    : 0f;
            }

            initialLeanValue = leansActivated ? rotationRate : 0f;

            float leanSmoothness = 5;
            float maxLeanRotationRate = 275.0f;

            float referenceValue = speed2D / sprintSpeed;
            leanValue = CalculateSmoothedValue(
                leanValue,
                initialLeanValue,
                maxLeanRotationRate,
                leanSmoothness,
                leanCurve,
                referenceValue,
                true
            );

            float headTurnSmoothness = 5f;

            if (headLookActivated && isTurningInPlace)
            {
                initialTurnValue = cameraRotationOffset;
                headLookX = Mathf.Lerp(headLookX, initialTurnValue / 200, 5f * Time.deltaTime);
            }
            else
            {
                initialTurnValue = headLookActivated ? rotationRate : 0f;
                headLookX = CalculateSmoothedValue(
                    headLookX,
                    initialTurnValue,
                    maxLeanRotationRate,
                    headTurnSmoothness,
                    headLookXCurve,
                    headLookX,
                    false
                );
            }

            float bodyTurnSmoothness = 5f;

            initialTurnValue = bodyLookActivated ? rotationRate : 0f;

            bodyLookX = CalculateSmoothedValue(
                bodyLookX,
                initialTurnValue,
                maxLeanRotationRate,
                bodyTurnSmoothness,
                bodyLookXCurve,
                bodyLookX,
                false
            );

            float cameraTilt = GetCameraTiltX();
            cameraTilt = (cameraTilt > 180f ? cameraTilt - 360f : cameraTilt) / -180;
            cameraTilt = Mathf.Clamp(cameraTilt, -0.1f, 1.0f);
            headLookY = cameraTilt;
            bodyLookY = cameraTilt;

            previousRotation = currentRotation;
        }

        private float CalculateSmoothedValue(
            float mainVariable,
            float newValue,
            float maxRateChange,
            float smoothness,
            AnimationCurve referenceCurve,
            float referenceValue,
            bool isMultiplier
        )
        {
            float changeVariable = newValue / maxRateChange;

            changeVariable = Mathf.Clamp(changeVariable, -1.0f, 1.0f);

            if (isMultiplier)
            {
                float multiplier = referenceCurve.Evaluate(referenceValue);
                changeVariable *= multiplier;
            }
            else
            {
                changeVariable = referenceCurve.Evaluate(changeVariable);
            }

            if (!changeVariable.Equals(mainVariable))
            {
                changeVariable = Mathf.Lerp(mainVariable, changeVariable, smoothness * Time.deltaTime);
            }

            return changeVariable;
        }

        private float VariableOverrideDelayTimer(float timeVariable)
        {
            if (timeVariable > 0.0f)
            {
                timeVariable -= Time.deltaTime;
                timeVariable = Mathf.Clamp(timeVariable, 0.0f, 1.0f);
            }
            else
            {
                timeVariable = 0.0f;
            }

            return timeVariable;
        }

        #endregion

        #region Lock-on System

        private void UpdateBestTarget()
        {
            GameObject newBestTarget;

            if (currentTargetCandidates.Count == 0)
            {
                newBestTarget = null;
            }
            else if (currentTargetCandidates.Count == 1)
            {
                newBestTarget = currentTargetCandidates[0];
            }
            else
            {
                newBestTarget = null;
                float bestTargetScore = 0f;

                foreach (GameObject target in currentTargetCandidates)
                {
                    var lockOnComponent = target.GetComponent<SampleObjectLockOn>();
                    if (lockOnComponent != null)
                    {
                        lockOnComponent.Highlight(false, false);
                    }

                    float distance = Vector3.Distance(transform.position, target.transform.position);
                    float distanceScore = distance > 0.01f ? (1 / distance * 100) : 100f;

                    Vector3 cameraPos = GetCameraPosition();
                    Vector3 cameraForwardDir = GetCameraForward();

                    Vector3 targetDirection = target.transform.position - cameraPos;
                    float angleInView = Vector3.Dot(targetDirection.normalized, cameraForwardDir);
                    float angleScore = angleInView * 40;

                    float totalScore = distanceScore + angleScore;

                    if (totalScore > bestTargetScore)
                    {
                        bestTargetScore = totalScore;
                        newBestTarget = target;
                    }
                }
            }

            if (!isLockedOn)
            {
                currentLockOnTarget = newBestTarget;

                if (currentLockOnTarget != null)
                {
                    var lockOnComponent = currentLockOnTarget.GetComponent<SampleObjectLockOn>();
                    if (lockOnComponent != null)
                    {
                        lockOnComponent.Highlight(true, false);
                    }
                }
            }
            else
            {
                if (currentTargetCandidates.Contains(currentLockOnTarget))
                {
                    var lockOnComponent = currentLockOnTarget.GetComponent<SampleObjectLockOn>();
                    if (lockOnComponent != null)
                    {
                        lockOnComponent.Highlight(true, true);
                    }
                }
                else
                {
                    currentLockOnTarget = newBestTarget;
                    EnableLockOn(false);
                }
            }
        }

        #endregion

        #endregion

        #region Locomotion State

        private void EnterLocomotionState()
        {
        }

        private void UpdateLocomotionState()
        {
            UpdateBestTarget();
            GroundedCheck();

            if (!isGrounded)
            {
                SwitchState(AnimationState.Fall);
            }

            if (isCrouching)
            {
                SwitchState(AnimationState.Crouch);
            }

            CheckEnableTurns();
            CheckEnableLean();
            CalculateRotationalAdditives(enableLean, enableHeadTurn, enableBodyTurn);

            CalculateMoveDirection();
            CheckIfStarting();
            CheckIfStopped();
            FaceMoveDirection();
            Move();
            UpdateAnimatorController();
        }

        private void ExitLocomotionState()
        {
        }

        private void LocomotionToJumpState()
        {
            SwitchState(AnimationState.Jump);
        }

        #endregion

        #region Jump State

        private void EnterJumpState()
        {
            playerAnimator.SetBool(isJumpingAnimHash, true);
            isSliding = false;
            velocity = new Vector3(velocity.x, jumpForce, velocity.z);
        }

        private void UpdateJumpState()
        {
            UpdateBestTarget();
            ApplyGravity();

            if (velocity.y <= 0f)
            {
                playerAnimator.SetBool(isJumpingAnimHash, false);
                SwitchState(AnimationState.Fall);
            }

            GroundedCheck();

            CalculateRotationalAdditives(false, enableHeadTurn, enableBodyTurn);
            CalculateMoveDirection();
            FaceMoveDirection();
            Move();
            UpdateAnimatorController();
        }

        private void ExitJumpState()
        {
            playerAnimator.SetBool(isJumpingAnimHash, false);
        }

        #endregion

        #region Fall State

        private void EnterFallState()
        {
            ResetFallingDuration();
            velocity.y = 0f;
            DeactivateCrouch();
            isSliding = false;
        }

        private void UpdateFallState()
        {
            UpdateBestTarget();
            GroundedCheck();

            CalculateRotationalAdditives(false, enableHeadTurn, enableBodyTurn);

            CalculateMoveDirection();
            FaceMoveDirection();

            ApplyGravity();
            Move();
            UpdateAnimatorController();

            if (characterController.isGrounded)
            {
                SwitchState(AnimationState.Locomotion);
            }

            UpdateFallingDuration();
        }

        #endregion

        #region Crouch State

        private void EnterCrouchState()
        {
        }

        private void UpdateCrouchState()
        {
            UpdateBestTarget();

            GroundedCheck();
            if (!isGrounded)
            {
                DeactivateCrouch();
                CapsuleCrouchingSize(false);
                SwitchState(AnimationState.Fall);
            }

            CeilingHeightCheck();

            if (!crouchKeyPressed && !cannotStandUp)
            {
                DeactivateCrouch();
                SwitchToLocomotionState();
            }

            if (!isCrouching)
            {
                CapsuleCrouchingSize(false);
                SwitchToLocomotionState();
            }

            CheckEnableTurns();
            CheckEnableLean();

            CalculateRotationalAdditives(false, enableHeadTurn, false);

            CalculateMoveDirection();
            CheckIfStarting();
            CheckIfStopped();

            FaceMoveDirection();
            Move();
            UpdateAnimatorController();
        }

        private void ExitCrouchState()
        {
        }

        private void CrouchToJumpState()
        {
            if (!cannotStandUp)
            {
                DeactivateCrouch();
                SwitchState(AnimationState.Jump);
            }
        }

        private void SwitchToLocomotionState()
        {
            DeactivateCrouch();
            SwitchState(AnimationState.Locomotion);
        }

        #endregion

        #region Public Methods for External Access

        /// <summary>
        /// 检查右摇杆是否正在控制旋转
        /// </summary>
        /// <returns>是否正在使用右摇杆旋转</returns>
        public bool IsThumbstickControllingRotation()
        {
            return isUsingThumbstickRotation || (Time.time - lastThumbstickRotationTime < 0.5f);
        }

        /// <summary>
        /// 手动旋转玩家和相机（供外部脚本调用）
        /// </summary>
        /// <param name="angle">旋转角度</param>
        public void ManualRotatePlayerAndCamera(float angle)
        {
            RotatePlayerAndCamera(angle);
        }

        /// <summary>
        /// 设置是否同时旋转相机
        /// </summary>
        /// <param name="rotateCamera">是否旋转相机</param>
        public void SetRotateCameraWithBody(bool rotateCamera)
        {
            rotateCameraWithBody = rotateCamera;
        }

        /// <summary>
        /// 设置XR Origin引用
        /// </summary>
        /// <param name="origin">XR Origin transform</param>
        public void SetXROrigin(Transform origin)
        {
            xrOrigin = origin;
        }

        public float GetCurrentSpeed()
        {
            return speed2D;
        }

        public GaitState GetCurrentGait()
        {
            return currentGait;
        }

        public bool IsGrounded()
        {
            return isGrounded;
        }

        public bool IsCrouching()
        {
            return isCrouching;
        }

        public bool IsSprinting()
        {
            return isSprinting;
        }

        public AnimationState GetCurrentAnimationState()
        {
            return currentState;
        }

        public void SetCameraOffset(Transform offset)
        {
            cameraOffset = offset;
        }

        public void SetMainCamera(Camera camera)
        {
            mainCamera = camera;
        }

        public void ManualRotateBody(float angle)
        {
            RotateBody(angle);
        }

        public float GetBodyRotationY()
        {
            return transform.eulerAngles.y;
        }

        public void SetSnapTurnMode(bool useSnap)
        {
            useSnapTurn = useSnap;
        }

        #endregion
    }
}