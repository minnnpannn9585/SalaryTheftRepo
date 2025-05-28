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
        [Tooltip("VR Camera Controller for camera behavior")]
        [SerializeField]
        private VRCameraController vrCameraController;
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

            isStrafing = alwaysStrafe;

            SwitchState(AnimationState.Locomotion);
        }

        #endregion

        #region VR Input

        /// <summary>
        /// Updates VR input states from XR controllers.
        /// </summary>
        private void UpdateVRInput()
        {
            // Get left controller input
            InputDevice leftDevice = InputDevices.GetDeviceAtXRNode(leftHandNode);
            if (leftDevice.isValid)
            {
                leftDevice.TryGetFeatureValue(primaryAxis, out leftThumbstick);
                leftDevice.TryGetFeatureValue(jumpButton, out jumpButtonPressed);
                leftDevice.TryGetFeatureValue(crouchButton, out crouchButtonPressed);
                leftDevice.TryGetFeatureValue(sprintGrip, out leftGripValue);
            }

            // Get right controller input
            InputDevice rightDevice = InputDevices.GetDeviceAtXRNode(rightHandNode);
            if (rightDevice.isValid)
            {
                rightDevice.TryGetFeatureValue(primaryAxis, out rightThumbstick);
                rightDevice.TryGetFeatureValue(sprintGrip, out rightGripValue);
            }

            // Process jump input
            if (jumpButtonPressed)
            {
                OnJumpPressed();
            }

            // Process crouch input
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

            // Process sprint input (based on grip)
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
        /// Handles jump input from VR controller.
        /// </summary>
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

        /// <summary>
        /// Gets the primary movement input from VR controllers.
        /// </summary>
        /// <returns>Movement input vector</returns>
        public Vector2 GetMovementInput()
        {
            // Use left thumbstick as primary movement input
            Vector2 input = leftThumbstick;
            
            // Apply deadzone
            if (input.magnitude < movementDeadzone)
            {
                input = Vector2.zero;
            }

            return input;
        }

        /// <summary>
        /// Checks if movement input is currently detected.
        /// </summary>
        /// <returns>True if movement input detected</returns>
        public bool IsMovementInputDetected()
        {
            return GetMovementInput().magnitude > 0f;
        }

        #endregion

        #region Aim and Lock-on

        /// <summary>
        /// Activates the aim action of the player.
        /// </summary>
        private void ActivateAim()
        {
            isAiming = true;
            isStrafing = !isSprinting;
        }

        /// <summary>
        /// Deactivates the aim action of the player.
        /// </summary>
        private void DeactivateAim()
        {
            isAiming = false;
            isStrafing = !isSprinting && (alwaysStrafe || isLockedOn);
        }

        /// <summary>
        /// Adds an object to the list of target candidates.
        /// </summary>
        /// <param name="newTarget">The object to add.</param>
        public void AddTargetCandidate(GameObject newTarget)
        {
            if (newTarget != null)
            {
                currentTargetCandidates.Add(newTarget);
            }
        }

        /// <summary>
        /// Removes an object to the list of target candidates if present.
        /// </summary>
        /// <param name="targetToRemove">The object to remove if present.</param>
        public void RemoveTarget(GameObject targetToRemove)
        {
            if (currentTargetCandidates.Contains(targetToRemove))
            {
                currentTargetCandidates.Remove(targetToRemove);
            }
        }

        /// <summary>
        /// Toggle the lock-on state.
        /// </summary>
        public void ToggleLockOn()
        {
            EnableLockOn(!isLockedOn);
        }

        /// <summary>
        /// Sets the lock-on state to the given state.
        /// </summary>
        /// <param name="enable">The state to set lock-on to.</param>
        private void EnableLockOn(bool enable)
        {
            isLockedOn = enable;
            isStrafing = false;

            isStrafing = enable ? !isSprinting : alwaysStrafe || isAiming;

            if (vrCameraController != null)
            {
                vrCameraController.LockOn(enable, targetLockOnPos);
            }

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

        /// <summary>
        /// Toggle the walking state.
        /// </summary>
        public void ToggleWalk()
        {
            EnableWalk(!isWalking);
        }

        /// <summary>
        /// Sets the walking state to that of the passed in state.
        /// </summary>
        /// <param name="enable">The state to set.</param>
        private void EnableWalk(bool enable)
        {
            isWalking = enable && isGrounded && !isSprinting;
        }

        #endregion

        #region Sprinting State

        /// <summary>
        /// Activates sprinting behaviour.
        /// </summary>
        private void ActivateSprint()
        {
            if (!isCrouching)
            {
                EnableWalk(false);
                isSprinting = true;
                isStrafing = false;
            }
        }

        /// <summary>
        /// Deactivates sprinting behaviour.
        /// </summary>
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

        /// <summary>
        /// Activates crouching behaviour
        /// </summary>
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

        /// <summary>
        /// Deactivates crouching behaviour.
        /// </summary>
        private void DeactivateCrouch()
        {
            crouchKeyPressed = false;

            if (!cannotStandUp && !isSliding)
            {
                CapsuleCrouchingSize(false);
                isCrouching = false;
            }
        }

        /// <summary>
        /// Activates sliding behaviour.
        /// </summary>
        public void ActivateSliding()
        {
            isSliding = true;
        }

        /// <summary>
        /// Deactivates sliding behaviour
        /// </summary>
        public void DeactivateSliding()
        {
            isSliding = false;
        }

        /// <summary>
        /// Adjusts the capsule size for the player, depending on the passed in boolean value.
        /// </summary>
        /// <param name="crouching">Whether the player is crouching or not.</param>
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

        /// <summary>
        /// Switch the current state to the passed in state.
        /// </summary>
        /// <param name="newState">The state to switch to.</param>
        private void SwitchState(AnimationState newState)
        {
            ExitCurrentState();
            EnterState(newState);
        }

        /// <summary>
        /// Enter the given state.
        /// </summary>
        /// <param name="stateToEnter">The state to enter.</param>
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

        /// <summary>
        /// Exit the current state.
        /// </summary>
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

        /// <summary>
        /// Updates the animator to have the latest values.
        /// </summary>
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

        /// <summary>
        /// Performs the actions required when entering the base state.
        /// </summary>
        private void EnterBaseState()
        {
            previousRotation = transform.forward;
        }

        /// <summary>
        /// Calculates the input type and sets the required internal states.
        /// </summary>
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
            
            if (vrCameraController != null)
            {
                moveDirection = (vrCameraController.GetCameraForwardZeroedYNormalised() * moveInput.y)
                    + (vrCameraController.GetCameraRightZeroedYNormalised() * moveInput.x);
            }
            else
            {
                moveDirection = new Vector3(moveInput.x, 0, moveInput.y);
            }
        }

        #endregion

        #region Movement

        /// <summary>
        /// Performs the movement of the player
        /// </summary>
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

        /// <summary>
        /// Applies gravity to the player.
        /// </summary>
        private void ApplyGravity()
        {
            if (velocity.y > Physics.gravity.y)
            {
                velocity.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
            }
        }

        /// <summary>
        /// Calculates the movement direction of the player, and sets the relevant flags.
        /// </summary>
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

        /// <summary>
        /// Calculates the character gait.
        /// Calculate what the current locomotion gait is (Walk, Run, Sprint)
        /// (for use in jumps, landings etc when deciding which animation to use)
        /// Gait values will be:
        /// Idle = 0, Walk = 1, Run = 2, Sprint = 3
        /// </summary>
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

        /// <summary>
        /// Calculates the face move direction based on the locomotion of the character.
        /// </summary>
        private void FaceMoveDirection()
        {
            Vector3 characterForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
            Vector3 characterRight = new Vector3(transform.right.x, 0f, transform.right.z).normalized;
            Vector3 directionForward = new Vector3(moveDirection.x, 0f, moveDirection.z).normalized;

            if (vrCameraController != null)
            {
                cameraForward = vrCameraController.GetCameraForwardZeroedYNormalised();
            }
            else
            {
                cameraForward = Vector3.forward;
            }
            
            Quaternion strafingTargetRotation = Quaternion.LookRotation(cameraForward);

            strafeAngle = characterForward != directionForward ? Vector3.SignedAngle(characterForward, directionForward, Vector3.up) : 0f;

            isTurningInPlace = false;

            if (isStrafing)
            {
                if (moveDirection.magnitude > 0.01)
                {
                    if (cameraForward != Vector3.zero)
                    {
                        // Shuffle direction values - these are separate from the strafe values as we don't want to lerp
                        shuffleDirectionZ = Vector3.Dot(characterForward, directionForward);
                        shuffleDirectionX = Vector3.Dot(characterRight, directionForward);

                        UpdateStrafeDirection(
                            Vector3.Dot(characterForward, directionForward),
                            Vector3.Dot(characterRight, directionForward)
                        );
                        cameraRotationOffset = Mathf.Lerp(cameraRotationOffset, 0f, rotationSmoothing * Time.deltaTime);

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

                    transform.rotation = Quaternion.Slerp(transform.rotation, strafingTargetRotation, rotationSmoothing * Time.deltaTime);
                }
                else
                {
                    UpdateStrafeDirection(1f, 0f);

                    float t = 20 * Time.deltaTime;
                    float newOffset = 0f;

                    if (characterForward != cameraForward)
                    {
                        newOffset = Vector3.SignedAngle(characterForward, cameraForward, Vector3.up);
                    }

                    cameraRotationOffset = Mathf.Lerp(cameraRotationOffset, newOffset, t);

                    if (Mathf.Abs(cameraRotationOffset) > 10)
                    {
                        isTurningInPlace = true;
                    }
                }
            }
            else
            {
                UpdateStrafeDirection(1f, 0f);
                cameraRotationOffset = Mathf.Lerp(cameraRotationOffset, 0f, rotationSmoothing * Time.deltaTime);

                shuffleDirectionZ = 1;
                shuffleDirectionX = 0;

                Vector3 faceDirection = new Vector3(velocity.x, 0f, velocity.z);

                if (faceDirection == Vector3.zero)
                {
                    return;
                }

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(faceDirection),
                    rotationSmoothing * Time.deltaTime
                );
            }
        }

        /// <summary>
        /// Checks if the player has stopped moving.
        /// </summary>
        private void CheckIfStopped()
        {
            isStopped = moveDirection.magnitude == 0 && speed2D < .5;
        }

        /// <summary>
        /// Checks if the player has started moving.
        /// </summary>
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

        /// <summary>
        /// Updates the strafe direction variables to those provided.
        /// </summary>
        /// <param name="TargetZ">The value to set for Z axis.</param>
        /// <param name="TargetX">The value to set for X axis.</param>
        private void UpdateStrafeDirection(float TargetZ, float TargetX)
        {
            strafeDirectionZ = Mathf.Lerp(strafeDirectionZ, TargetZ, ANIMATION_DAMP_TIME * Time.deltaTime);
            strafeDirectionX = Mathf.Lerp(strafeDirectionX, TargetX, ANIMATION_DAMP_TIME * Time.deltaTime);
            strafeDirectionZ = Mathf.Round(strafeDirectionZ * 1000f) / 1000f;
            strafeDirectionX = Mathf.Round(strafeDirectionX * 1000f) / 1000f;
        }

        #endregion

        #region Ground Checks

        /// <summary>
        /// Checks if the character is grounded.
        /// </summary>
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

        /// <summary>
        /// Checks for ground incline and sets the required variables.
        /// </summary>
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

        /// <summary>
        /// Checks the height of the ceiling above the player to make sure there is room to stand up if crouching.
        /// </summary>
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

        /// <summary>
        /// Resets the falling duration variables.
        /// </summary>
        private void ResetFallingDuration()
        {
            fallStartTime = Time.time;
            fallingDuration = 0f;
        }

        /// <summary>
        /// Updates the falling duration variables.
        /// </summary>
        private void UpdateFallingDuration()
        {
            fallingDuration = Time.time - fallStartTime;
        }

        #endregion

        #region Checks

        /// <summary>
        /// Checks if body turns can be enabled, and enabled or disables as required.
        /// </summary>
        private void CheckEnableTurns()
        {
            headLookDelay = VariableOverrideDelayTimer(headLookDelay);
            enableHeadTurn = headLookDelay == 0.0f && !isStarting;
            bodyLookDelay = VariableOverrideDelayTimer(bodyLookDelay);
            enableBodyTurn = bodyLookDelay == 0.0f && !(isStarting || isTurningInPlace);
        }

        /// <summary>
        /// Checks if lean can be enabled, then enabled or disables as required.
        /// </summary>
        private void CheckEnableLean()
        {
            leanDelay = VariableOverrideDelayTimer(leanDelay);
            enableLean = leanDelay == 0.0f && !(isStarting || isTurningInPlace);
        }

        #endregion

        #region Lean and Offsets

        /// <summary>
        /// Calculates the required rotational additives based on the passed in parameters.
        /// </summary>
        /// <param name="leansActivated">If leans are activated or not.</param>
        /// <param name="headLookActivated">If head look is activated or not.</param>
        /// <param name="bodyLookActivated">If body look is activated or not.</param>
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

            float cameraTilt = 0f;
            if (vrCameraController != null)
            {
                cameraTilt = vrCameraController.GetCameraTiltX();
            }
            
            cameraTilt = (cameraTilt > 180f ? cameraTilt - 360f : cameraTilt) / -180;
            cameraTilt = Mathf.Clamp(cameraTilt, -0.1f, 1.0f);
            headLookY = cameraTilt;
            bodyLookY = cameraTilt;

            previousRotation = currentRotation;
        }

        /// <summary>
        /// Calculates a smoothed value between the given variable and target variable, from the given parameters.
        /// </summary>
        /// <param name="mainVariable">The variable to smooth.</param>
        /// <param name="newValue">The target new value.</param>
        /// <param name="maxRateChange">The max rate of change.</param>
        /// <param name="smoothness">The smoothness amount.</param>
        /// <param name="referenceCurve">The reference curve.</param>
        /// <param name="referenceValue">The reference value.</param>
        /// <param name="isMultiplier">If the value is a multiplier or not.</param>
        /// <returns>The smoothed value.</returns>
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

        /// <summary>
        /// Provides a clamped override delay to avoid animation transition issues.
        /// </summary>
        /// <param name="timeVariable">The time variable to use.</param>
        /// <returns>A clamped override delay.</returns>
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

        /// <summary>
        /// Updates and sets the best target for lock on from the list of available targets.
        /// </summary>
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
                    float distanceScore = 1 / distance * 100;

                    Vector3 cameraPos = vrCameraController != null ? vrCameraController.GetCameraPosition() : transform.position;
                    Vector3 cameraForwardDir = vrCameraController != null ? vrCameraController.GetCameraForward() : transform.forward;
                    
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

        /// <summary>
        /// Sets up the locomotion state upon entry.
        /// </summary>
        private void EnterLocomotionState()
        {
            // VR version doesn't use events for jump, handled in UpdateVRInput
        }

        /// <summary>
        /// Updates the locomotion state.
        /// </summary>
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

        /// <summary>
        /// Performs the required actions when exiting the locomotion state.
        /// </summary>
        private void ExitLocomotionState()
        {
            // VR version cleanup if needed
        }

        /// <summary>
        /// Moves from the locomotion to the jump state.
        /// </summary>
        private void LocomotionToJumpState()
        {
            SwitchState(AnimationState.Jump);
        }

        #endregion

        #region Jump State

        /// <summary>
        /// Sets up the Jump state upon entry.
        /// </summary>
        private void EnterJumpState()
        {
            playerAnimator.SetBool(isJumpingAnimHash, true);

            isSliding = false;

            velocity = new Vector3(velocity.x, jumpForce, velocity.z);
        }

        /// <summary>
        /// updates the jump state.
        /// </summary>
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

        /// <summary>
        /// Performs the required actions upon exiting the jump state.
        /// </summary>
        private void ExitJumpState()
        {
            playerAnimator.SetBool(isJumpingAnimHash, false);
        }

        #endregion

        #region Fall State

        /// <summary>
        /// Sets up the fall state upon entry.
        /// </summary>
        private void EnterFallState()
        {
            ResetFallingDuration();
            velocity.y = 0f;

            DeactivateCrouch();
            isSliding = false;
        }

        /// <summary>
        /// Updates the fall state.
        /// </summary>
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

        /// <summary>
        /// Sets up the crouch state upon entry.
        /// </summary>
        private void EnterCrouchState()
        {
            // VR version doesn't use events for jump, handled in UpdateVRInput
        }

        /// <summary>
        /// Updates the crouch state.
        /// </summary>
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

        /// <summary>
        /// Performs the required actions upon exiting the crouch state.
        /// </summary>
        private void ExitCrouchState()
        {
            // VR version cleanup if needed
        }

        /// <summary>
        /// Moves from the crouch state to the jump state.
        /// </summary>
        private void CrouchToJumpState()
        {
            if (!cannotStandUp)
            {
                DeactivateCrouch();
                SwitchState(AnimationState.Jump);
            }
        }

        /// <summary>
        /// Moves from the crouch state to the locomotion state.
        /// </summary>
        private void SwitchToLocomotionState()
        {
            DeactivateCrouch();
            SwitchState(AnimationState.Locomotion);
        }

        #endregion

        #region Public Methods for External Access

        /// <summary>
        /// Gets the current movement speed of the player.
        /// </summary>
        /// <returns>Current movement speed</returns>
        public float GetCurrentSpeed()
        {
            return speed2D;
        }

        /// <summary>
        /// Gets the current gait state of the player.
        /// </summary>
        /// <returns>Current gait state</returns>
        public GaitState GetCurrentGait()
        {
            return currentGait;
        }

        /// <summary>
        /// Gets whether the player is currently grounded.
        /// </summary>
        /// <returns>True if grounded</returns>
        public bool IsGrounded()
        {
            return isGrounded;
        }

        /// <summary>
        /// Gets whether the player is currently crouching.
        /// </summary>
        /// <returns>True if crouching</returns>
        public bool IsCrouching()
        {
            return isCrouching;
        }

        /// <summary>
        /// Gets whether the player is currently sprinting.
        /// </summary>
        /// <returns>True if sprinting</returns>
        public bool IsSprinting()
        {
            return isSprinting;
        }

        /// <summary>
        /// Gets the current animation state.
        /// </summary>
        /// <returns>Current animation state</returns>
        public AnimationState GetCurrentAnimationState()
        {
            return currentState;
        }

        /// <summary>
        /// Sets the VR camera controller reference.
        /// </summary>
        /// <param name="controller">VR camera controller</param>
        public void SetVRCameraController(VRCameraController controller)
        {
            vrCameraController = controller;
        }

        #endregion
    }
}