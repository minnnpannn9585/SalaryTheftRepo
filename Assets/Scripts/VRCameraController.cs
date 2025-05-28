// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// Sample scripts are included only as examples and are not intended as production-ready.

using UnityEngine;

namespace Synty.AnimationBaseLocomotion.Samples
{
    public class VRCameraController : MonoBehaviour
    {
        private const int LAG_DELTA_TIME_ADJUSTMENT = 20;

        [Header("XR Rig Setup")]
        [Tooltip("The main VR camera (will be found automatically if not assigned)")]
        [SerializeField]
        private Camera vrCamera;
        
        [Tooltip("The character game object")]
        [SerializeField]
        private GameObject syntyCharacter;

        [Header("Follow Targets")]
        [SerializeField]
        private Transform playerTarget;
        [SerializeField]
        private Transform lockOnTarget;

        [Header("Camera Settings")]
        [SerializeField]
        private bool isLockedOn;
        [SerializeField]
        private float cameraDistance = 5f;
        [SerializeField]
        private float cameraHeightOffset = 1.6f; // VR typical eye height
        [SerializeField]
        private float cameraHorizontalOffset;
        [SerializeField]
        private float cameraTiltOffset;
        [SerializeField]
        private Vector2 cameraTiltBounds = new Vector2(-10f, 45f);
        [SerializeField]
        private float positionalCameraLag = 1f;
        [SerializeField]
        private float rotationalCameraLag = 1f;

        [Header("VR Specific")]
        [SerializeField]
        private bool followPlayerRotation = true;
        [SerializeField]
        private bool smoothFollowMovement = true;

        private float lastAngleX;
        private float lastAngleY;
        private Vector3 lastPosition;
        private float newAngleX;
        private float newAngleY;
        private Vector3 newPosition;

        private Transform xrRig;

        private void Start()
        {
            // This script should be attached to the XR Rig
            xrRig = transform;

            // Find the VR camera automatically if not assigned
            if (vrCamera == null)
            {
                // Look for camera in children (typical XR Rig structure)
                vrCamera = GetComponentInChildren<Camera>();
                
                if (vrCamera == null)
                {
                    // Fallback to main camera
                    vrCamera = Camera.main;
                }
                
                if (vrCamera == null)
                {
                    Debug.LogWarning("VRCameraController: No VR camera found. Please assign manually.");
                }
            }

            // Setup player target if not assigned
            if (playerTarget == null && syntyCharacter != null)
            {
                playerTarget = syntyCharacter.transform;
            }

            // Setup lock-on target
            if (syntyCharacter != null)
            {
                Transform lockOnTransform = syntyCharacter.transform.Find("TargetLockOnPos");
                if (lockOnTransform != null)
                {
                    lockOnTarget = lockOnTransform;
                }
            }

            // Initialize XR Rig position and rotation
            if (playerTarget != null)
            {
                xrRig.position = playerTarget.position + Vector3.up * cameraHeightOffset;
                if (followPlayerRotation)
                {
                    xrRig.rotation = playerTarget.rotation;
                }
                
                lastPosition = xrRig.position;
                lastAngleX = xrRig.eulerAngles.x;
                lastAngleY = xrRig.eulerAngles.y;
            }
        }

        private void Update()
        {
            if (playerTarget == null) return;

            float positionalFollowSpeed = 1 / (positionalCameraLag / LAG_DELTA_TIME_ADJUSTMENT);
            float rotationalFollowSpeed = 1 / (rotationalCameraLag / LAG_DELTA_TIME_ADJUSTMENT);

            // Handle lock-on functionality
            if (isLockedOn && lockOnTarget != null)
            {
                Vector3 aimVector = lockOnTarget.position - playerTarget.position;
                Quaternion targetRotation = Quaternion.LookRotation(aimVector);
                
                if (smoothFollowMovement)
                {
                    targetRotation = Quaternion.Lerp(xrRig.rotation, targetRotation, rotationalFollowSpeed * Time.deltaTime);
                }
                
                newAngleY = targetRotation.eulerAngles.y;
                newAngleX = targetRotation.eulerAngles.x;
            }
            else if (followPlayerRotation)
            {
                // Follow player rotation smoothly
                newAngleY = playerTarget.eulerAngles.y;
                newAngleX = Mathf.Clamp(playerTarget.eulerAngles.x, cameraTiltBounds.x, cameraTiltBounds.y);
                
                if (smoothFollowMovement)
                {
                    newAngleX = Mathf.Lerp(lastAngleX, newAngleX, rotationalFollowSpeed * Time.deltaTime);
                    newAngleY = Mathf.Lerp(lastAngleY, newAngleY, rotationalFollowSpeed * Time.deltaTime);
                }
            }

            // Update position
            Vector3 targetPosition = playerTarget.position + Vector3.up * cameraHeightOffset;
            targetPosition += playerTarget.right * cameraHorizontalOffset;
            
            if (smoothFollowMovement)
            {
                newPosition = Vector3.Lerp(lastPosition, targetPosition, positionalFollowSpeed * Time.deltaTime);
            }
            else
            {
                newPosition = targetPosition;
            }

            // Apply transformations to XR Rig
            xrRig.position = newPosition;
            
            if (followPlayerRotation || isLockedOn)
            {
                xrRig.eulerAngles = new Vector3(newAngleX + cameraTiltOffset, newAngleY, 0);
            }

            // Store last values for smooth interpolation
            lastPosition = newPosition;
            lastAngleX = newAngleX;
            lastAngleY = newAngleY;
        }

        /// <summary>
        /// Locks the camera to aim at a specified target.
        /// </summary>
        /// <param name="enable">Whether lock on is enabled or not.</param>
        /// <param name="newLockOnTarget">The target to lock on to.</param>
        public void LockOn(bool enable, Transform newLockOnTarget = null)
        {
            isLockedOn = enable;

            if (newLockOnTarget != null)
            {
                lockOnTarget = newLockOnTarget;
            }
        }

        /// <summary>
        /// Gets the position of the VR camera (actual camera, not XR Rig).
        /// </summary>
        /// <returns>The position of the VR camera.</returns>
        public Vector3 GetCameraPosition()
        {
            return vrCamera != null ? vrCamera.transform.position : xrRig.position;
        }

        /// <summary>
        /// Gets the position of the XR Rig.
        /// </summary>
        /// <returns>The position of the XR Rig.</returns>
        public Vector3 GetRigPosition()
        {
            return xrRig.position;
        }

        /// <summary>
        /// Gets the forward vector of the VR camera (actual camera, not XR Rig).
        /// </summary>
        /// <returns>The forward vector of the VR camera.</returns>
        public Vector3 GetCameraForward()
        {
            return vrCamera != null ? vrCamera.transform.forward : xrRig.forward;
        }

        /// <summary>
        /// Gets the forward vector of the XR Rig.
        /// </summary>
        /// <returns>The forward vector of the XR Rig.</returns>
        public Vector3 GetRigForward()
        {
            return xrRig.forward;
        }

        /// <summary>
        /// Gets the forward vector of the VR camera with the Y value zeroed.
        /// </summary>
        /// <returns>The forward vector of the VR camera with the Y value zeroed.</returns>
        public Vector3 GetCameraForwardZeroedY()
        {
            Vector3 forward = GetCameraForward();
            return new Vector3(forward.x, 0, forward.z);
        }

        /// <summary>
        /// Gets the normalised forward vector of the VR camera with the Y value zeroed.
        /// </summary>
        /// <returns>The normalised forward vector of the VR camera with the Y value zeroed.</returns>
        public Vector3 GetCameraForwardZeroedYNormalised()
        {
            return GetCameraForwardZeroedY().normalized;
        }

        /// <summary>
        /// Gets the right vector of the VR camera with the Y value zeroed.
        /// </summary>
        /// <returns>The right vector of the VR camera with the Y value zeroed.</returns>
        public Vector3 GetCameraRightZeroedY()
        {
            Vector3 right = vrCamera != null ? vrCamera.transform.right : xrRig.right;
            return new Vector3(right.x, 0, right.z);
        }

        /// <summary>
        /// Gets the right vector of the XR Rig with the Y value zeroed.
        /// </summary>
        /// <returns>The right vector of the XR Rig with the Y value zeroed.</returns>
        public Vector3 GetRigRightZeroedY()
        {
            Vector3 right = xrRig.right;
            return new Vector3(right.x, 0, right.z);
        }

        /// <summary>
        /// Gets the normalised right vector of the VR camera with the Y value zeroed.
        /// </summary>
        /// <returns>The normalised right vector of the VR camera with the Y value zeroed.</returns>
        public Vector3 GetCameraRightZeroedYNormalised()
        {
            return GetCameraRightZeroedY().normalized;
        }

        /// <summary>
        /// Gets the normalised right vector of the XR Rig with the Y value zeroed.
        /// </summary>
        /// <returns>The normalised right vector of the XR Rig with the Y value zeroed.</returns>
        public Vector3 GetRigRightZeroedYNormalised()
        {
            return GetRigRightZeroedY().normalized;
        }

        /// <summary>
        /// Gets the X value of the VR camera tilt.
        /// </summary>
        /// <returns>The X value of the VR camera tilt.</returns>
        public float GetCameraTiltX()
        {
            return vrCamera != null ? vrCamera.transform.eulerAngles.x : xrRig.eulerAngles.x;
        }

        /// <summary>
        /// Gets the X value of the XR Rig tilt.
        /// </summary>
        /// <returns>The X value of the XR Rig tilt.</returns>
        public float GetRigTiltX()
        {
            return xrRig.eulerAngles.x;
        }

        /// <summary>
        /// Gets the VR camera component.
        /// </summary>
        /// <returns>The VR camera component.</returns>
        public Camera GetVRCamera()
        {
            return vrCamera;
        }

        /// <summary>
        /// Sets whether the camera should follow player rotation.
        /// </summary>
        /// <param name="follow">Whether to follow player rotation.</param>
        public void SetFollowPlayerRotation(bool follow)
        {
            followPlayerRotation = follow;
        }

        /// <summary>
        /// Sets the camera distance from the player.
        /// </summary>
        /// <param name="distance">The distance from the player.</param>
        public void SetCameraDistance(float distance)
        {
            cameraDistance = distance;
        }

        /// <summary>
        /// Sets the camera height offset.
        /// </summary>
        /// <param name="height">The height offset.</param>
        public void SetCameraHeightOffset(float height)
        {
            cameraHeightOffset = height;
        }

        /// <summary>
        /// Gets whether the camera is currently locked on to a target.
        /// </summary>
        /// <returns>True if locked on, false otherwise.</returns>
        public bool IsLockedOn()
        {
            return isLockedOn;
        }

        /// <summary>
        /// Gets the current lock-on target.
        /// </summary>
        /// <returns>The current lock-on target transform.</returns>
        public Transform GetLockOnTarget()
        {
            return lockOnTarget;
        }
    }
}