// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Modified for NPC behavior with behavior tree system and advanced vision

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Synty.AnimationBaseLocomotion.NPC
{
    public class NPCAnimationController : MonoBehaviour
    {
        #region Enums

        private enum AnimationState
        {
            Base,
            Locomotion,
            Jump,
            Fall,
            Crouch
        }

        private enum GaitState
        {
            Idle,
            Walk,
            Run,
            Sprint
        }

        public enum NPCGaitState
        {
            Idle,
            Walk,
            Run,
            Sprint
        }

        #endregion

        #region Animation Variable Hashes

        private readonly int _movementInputTappedHash = Animator.StringToHash("MovementInputTapped");
        private readonly int _movementInputPressedHash = Animator.StringToHash("MovementInputPressed");
        private readonly int _movementInputHeldHash = Animator.StringToHash("MovementInputHeld");
        private readonly int _shuffleDirectionXHash = Animator.StringToHash("ShuffleDirectionX");
        private readonly int _shuffleDirectionZHash = Animator.StringToHash("ShuffleDirectionZ");
        private readonly int _moveSpeedHash = Animator.StringToHash("MoveSpeed");
        private readonly int _currentGaitHash = Animator.StringToHash("CurrentGait");
        private readonly int _isJumpingAnimHash = Animator.StringToHash("IsJumping");
        private readonly int _fallingDurationHash = Animator.StringToHash("FallingDuration");
        private readonly int _inclineAngleHash = Animator.StringToHash("InclineAngle");
        private readonly int _strafeDirectionXHash = Animator.StringToHash("StrafeDirectionX");
        private readonly int _strafeDirectionZHash = Animator.StringToHash("StrafeDirectionZ");
        private readonly int _forwardStrafeHash = Animator.StringToHash("ForwardStrafe");
        private readonly int _cameraRotationOffsetHash = Animator.StringToHash("CameraRotationOffset");
        private readonly int _isStrafingHash = Animator.StringToHash("IsStrafing");
        private readonly int _isTurningInPlaceHash = Animator.StringToHash("IsTurningInPlace");
        private readonly int _isCrouchingHash = Animator.StringToHash("IsCrouching");
        private readonly int _isWalkingHash = Animator.StringToHash("IsWalking");
        private readonly int _isStoppedHash = Animator.StringToHash("IsStopped");
        private readonly int _isStartingHash = Animator.StringToHash("IsStarting");
        private readonly int _isGroundedHash = Animator.StringToHash("IsGrounded");
        private readonly int _leanValueHash = Animator.StringToHash("LeanValue");
        private readonly int _headLookXHash = Animator.StringToHash("HeadLookX");
        private readonly int _headLookYHash = Animator.StringToHash("HeadLookY");
        private readonly int _bodyLookXHash = Animator.StringToHash("BodyLookX");
        private readonly int _bodyLookYHash = Animator.StringToHash("BodyLookY");
        private readonly int _locomotionStartDirectionHash = Animator.StringToHash("LocomotionStartDirection");

        #endregion

        #region Serialized Fields

        [Header("Core Components")]
        [SerializeField] private Animator _animator;
        [SerializeField] private CharacterController _controller;

        [Header("Movement Settings")]
        [SerializeField] private bool _alwaysStrafe = false;
        [SerializeField] private float _walkSpeed = 1.4f;
        [SerializeField] private float _runSpeed = 2.5f;
        [SerializeField] private float _sprintSpeed = 7f;
        [SerializeField] private float _speedChangeDamping = 10f;
        [SerializeField] private float _rotationSmoothing = 10f;

        [Header("Path Following")]
        [SerializeField] private bool _enablePathFollowing = true;
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private bool _loopWaypoints = true;
        [SerializeField] private float _waypointReachDistance = 1f;
        [SerializeField] private float _waypointWaitTime = 2f;

        [Header("Detection Settings")]
        [SerializeField] private LayerMask _obstacleLayerMask = -1;
        [SerializeField] private float _obstacleDetectionDistance = 1.0f;
        [SerializeField] private float _obstacleDetectionAngle = 20f;
        [SerializeField] private int _obstacleRayCount = 2;
        [SerializeField] private float _playerDetectionAngle = 60f;
        [SerializeField] private float _playerDetectionDistance = 10f;
        [SerializeField] private string _playerTag = "Player";
        [SerializeField] private LayerMask _blockingLayerMask = -1; // 新增：定义哪些图层会阻挡视线

        [Header("Vision System")]
        [SerializeField] private bool _enableHeadTurn = true;
        [SerializeField] private float _maxHeadRotationAngle = 60f;
        [SerializeField] private float _headRotationSpeed = 45f;
        [SerializeField] private float _scanningSpeed = 30f;
        [SerializeField] private float _scanPauseTime = 1f;
        [SerializeField] private float _walkScanRange = 25f;
        [SerializeField] private float _walkScanSpeed = 20f;
        [SerializeField] private float _playerLockBreakAngle = 80f;
        [SerializeField] private float _stationaryScanInterval = 8f;
        [SerializeField] private float _stationaryScanDuration = 4f;

        [Header("Physics Settings")]
        [SerializeField] private Transform _rearRayPos;
        [SerializeField] private Transform _frontRayPos;
        [SerializeField] private LayerMask _groundLayerMask;
        [SerializeField] private float _groundedOffset = -0.14f;
        [SerializeField] private float _jumpForce = 10f;
        [SerializeField] private float _gravityMultiplier = 2f;

        #endregion

        #region Private Variables

        // Animation state
        private AnimationState _currentState = AnimationState.Base;
        private GaitState _currentGait;
        private bool _isGrounded = true;
        private bool _isWalking;
        private bool _isSprinting;
        private bool _isCrouching;
        private bool _isStarting;
        private bool _isStopped = true;
        private bool _isStrafing;
        private bool _isTurningInPlace;

        // Movement
        private Vector3 _moveDirection;
        private Vector3 _velocity;
        private Vector3 _targetVelocity;
        private float _speed2D;
        private float _currentMaxSpeed;
        private float _targetMaxSpeed;

        // Animation values
        private float _headLookX;
        private float _headLookY;
        private float _bodyLookX;
        private float _bodyLookY;
        private float _leanValue;
        private float _inclineAngle;
        private float _locomotionStartDirection;
        private float _locomotionStartTimer;
        private float _fallingDuration;
        private float _fallStartTime;

        // Input simulation
        private bool _movementInputHeld;
        private bool _movementInputPressed;
        private bool _movementInputTapped;

        // Vision system
        private float _currentHeadAngle = 0f;
        private float _targetHeadAngle = 0f;
        private bool _isPlayerLocked = false;
        private bool _isScanning = false;
        private bool _isScanningStationary = false;
        private float _scanTimer = 0f;
        private float _scanDirection = 1f;
        private float _lastStationaryScanTime = 0f;
        private float _stationaryScanTimer = 0f;
        private float _walkScanTimer = 0f;

        // Detection
        [Header("NPC Status (Debug)")]
        [Tooltip("Is player currently detected?")]
        [SerializeField] private bool _hasPlayerInSight = false;
        [Tooltip("Is obstacle detected ahead?")]
        [SerializeField] private bool _hasObstacleAhead = false;
        [Tooltip("Currently detected player transform")]
        [SerializeField] private Transform _detectedPlayer;
        [Tooltip("Is player blocked by obstacles?")]
        [SerializeField] private bool _isPlayerBlocked = false; // 新增：玩家是否被遮挡

        // Path following
        private int _currentWaypointIndex = 0;
        private float _waypointWaitTimer = 0f;
        private bool _isWaitingAtWaypoint = false;
        private bool _isFollowingPath = false;

        // Animation helpers
        private float _headLookDelay;
        private float _bodyLookDelay;
        private float _leanDelay;
        private Vector3 _currentRotation;
        private Vector3 _previousRotation;
        private float _rotationRate;
        private float _newDirectionDifferenceAngle;
        private float _strafeDirectionX;
        private float _strafeDirectionZ;
        private float _forwardStrafe = 1f;

        // Behavior tree
        private NPCBehaviorTree _behaviorTree;

        // Constants
        private const float _ANIMATION_DAMP_TIME = 5f;

        #endregion

        #region Public Properties

        public bool HasPlayerInSight => _hasPlayerInSight;
        public bool HasObstacleAhead => _hasObstacleAhead;
        public Transform DetectedPlayer => _detectedPlayer;
        public bool IsPlayerBlocked => _isPlayerBlocked; // 新增
        public bool IsMoving => _speed2D > 0.1f;
        public bool IsGrounded => _isGrounded;
        public float ObstacleDetectionDistance => _obstacleDetectionDistance;
        public LayerMask ObstacleLayerMask => _obstacleLayerMask;
        public bool IsFollowingPath => _isFollowingPath;
        public bool EnablePathFollowing => _enablePathFollowing;
        public Transform[] Waypoints => _waypoints;
        public bool IsScanning => _isScanning;
        public bool IsScanningStationary => _isScanningStationary;
        public bool IsPlayerLocked => _isPlayerLocked;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _isStrafing = _alwaysStrafe;
            _behaviorTree = new NPCBehaviorTree(this);
            SwitchState(AnimationState.Locomotion);
        }

        private void Update()
        {
            UpdateVisionSystem();
            ScanForObstacles();
            ScanForPlayer();
            UpdatePathFollowing();

            _behaviorTree?.Update();

            switch (_currentState)
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

        private void OnDrawGizmosSelected()
        {
            // 障碍物检测可视化
            Gizmos.color = _hasObstacleAhead ? Color.red : Color.green;
            Vector3 leftBoundary = Quaternion.AngleAxis(-_obstacleDetectionAngle / 2f, Vector3.up) * transform.forward * _obstacleDetectionDistance;
            Vector3 rightBoundary = Quaternion.AngleAxis(_obstacleDetectionAngle / 2f, Vector3.up) * transform.forward * _obstacleDetectionDistance;
            Gizmos.DrawRay(transform.position, leftBoundary);
            Gizmos.DrawRay(transform.position, rightBoundary);

            // 玩家检测可视化 - 根据遮挡状态改变颜色
            if (_hasPlayerInSight)
            {
                Gizmos.color = _isPlayerBlocked ? Color.yellow : Color.blue; // 黄色表示被遮挡，蓝色表示可见
            }
            else
            {
                Gizmos.color = Color.gray; // 灰色表示未检测到
            }

            Vector3 headDirection = GetHeadLookDirection();
            Vector3 leftPlayerBoundary = Quaternion.AngleAxis(-_playerDetectionAngle / 2f, Vector3.up) * headDirection * _playerDetectionDistance;
            Vector3 rightPlayerBoundary = Quaternion.AngleAxis(_playerDetectionAngle / 2f, Vector3.up) * headDirection * _playerDetectionDistance;
            Gizmos.DrawRay(transform.position, leftPlayerBoundary);
            Gizmos.DrawRay(transform.position, rightPlayerBoundary);

            // 检测范围球体
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _playerDetectionDistance);

            // 如果检测到玩家，绘制到玩家的连线
            if (_detectedPlayer != null)
            {
                Gizmos.color = _isPlayerBlocked ? Color.yellow : Color.blue;
                Gizmos.DrawLine(transform.position + Vector3.up * 1.7f, _detectedPlayer.position + Vector3.up * 1f);
            }

            // 路径点可视化
            if (_waypoints != null && _waypoints.Length > 1)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < _waypoints.Length; i++)
                {
                    if (_waypoints[i] != null)
                    {
                        Gizmos.DrawWireSphere(_waypoints[i].position, 0.5f);
                        int nextIndex = (_loopWaypoints && i == _waypoints.Length - 1) ? 0 : i + 1;
                        if (nextIndex < _waypoints.Length && _waypoints[nextIndex] != null)
                        {
                            Gizmos.DrawLine(_waypoints[i].position, _waypoints[nextIndex].position);
                        }
                    }
                }
            }
        }

        #endregion

        #region Vision System

        private void UpdateVisionSystem()
        {
            UpdateHeadRotation();

            if (_isPlayerLocked && _detectedPlayer != null)
            {
                UpdatePlayerTracking();
            }
            else if (_isScanningStationary)
            {
                UpdateStationaryScanning();
            }
            else if (IsMoving && !_hasPlayerInSight)
            {
                UpdateWalkingScanning();
            }
            else if (!IsMoving && !_hasPlayerInSight)
            {
                CheckForStationaryScanning();
            }
            else
            {
                _targetHeadAngle = 0f;
            }
        }

        private void UpdateHeadRotation()
        {
            if (!_enableHeadTurn) return;

            _targetHeadAngle = Mathf.Clamp(_targetHeadAngle, -_maxHeadRotationAngle, _maxHeadRotationAngle);
            float rotationDelta = _headRotationSpeed * Time.deltaTime;
            _currentHeadAngle = Mathf.MoveTowards(_currentHeadAngle, _targetHeadAngle, rotationDelta);
            _headLookX = _currentHeadAngle / _maxHeadRotationAngle;
        }

        private void UpdatePlayerTracking()
        {
            if (_detectedPlayer == null)
            {
                _isPlayerLocked = false;
                return;
            }

            Vector3 directionToPlayer = (_detectedPlayer.position - transform.position).normalized;
            float angleToPlayer = Vector3.SignedAngle(transform.forward, directionToPlayer, Vector3.up);

            if (Mathf.Abs(angleToPlayer) <= _maxHeadRotationAngle)
            {
                _targetHeadAngle = angleToPlayer;
            }
            else if (Mathf.Abs(angleToPlayer) > _playerLockBreakAngle)
            {
                _isPlayerLocked = false;
                _targetHeadAngle = 0f;
            }
        }

        private void UpdateWalkingScanning()
        {
            _walkScanTimer += Time.deltaTime;
            float scanAngle = Mathf.Sin(_walkScanTimer * _walkScanSpeed * Mathf.Deg2Rad) * _walkScanRange;
            _targetHeadAngle = scanAngle;
        }

        private void CheckForStationaryScanning()
        {
            if (Time.time - _lastStationaryScanTime > _stationaryScanInterval)
            {
                StartStationaryScanning();
            }
        }

        public void StartStationaryScanning()
        {
            _isScanningStationary = true;
            _isScanning = true;
            _stationaryScanTimer = 0f;
            _scanTimer = 0f;
            _scanDirection = 1f;
            _targetHeadAngle = -_maxHeadRotationAngle;
            _lastStationaryScanTime = Time.time;
        }

        public void StopStationaryScanning()
        {
            _isScanningStationary = false;
            _isScanning = false;
            _targetHeadAngle = 0f;
        }

        private void UpdateStationaryScanning()
        {
            _stationaryScanTimer += Time.deltaTime;

            if (_stationaryScanTimer >= _stationaryScanDuration)
            {
                StopStationaryScanning();
                return;
            }

            _scanTimer += Time.deltaTime;

            if (_scanTimer >= _scanPauseTime)
            {
                _scanTimer = 0f;

                if (_targetHeadAngle >= _maxHeadRotationAngle)
                {
                    _scanDirection = -1f;
                }
                else if (_targetHeadAngle <= -_maxHeadRotationAngle)
                {
                    _scanDirection = 1f;
                }

                _targetHeadAngle += _scanDirection * (_maxHeadRotationAngle * 0.5f);
                _targetHeadAngle = Mathf.Clamp(_targetHeadAngle, -_maxHeadRotationAngle, _maxHeadRotationAngle);
            }
        }

        public void LockOntoPlayer(Transform player)
        {
            _detectedPlayer = player;
            _isPlayerLocked = true;
            _isScanning = false;
            _isScanningStationary = false;
        }

        public void ReleasePlayerLock()
        {
            _isPlayerLocked = false;
            _detectedPlayer = null;
        }

        public Vector3 GetHeadLookDirection()
        {
            return Quaternion.AngleAxis(_currentHeadAngle, Vector3.up) * transform.forward;
        }

        #endregion

        #region Detection Systems

        private void ScanForObstacles()
        {
            _hasObstacleAhead = false;
            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
            float angleStep = _obstacleDetectionAngle / (_obstacleRayCount - 1);
            float startAngle = -_obstacleDetectionAngle / 2f;

            for (int i = 0; i < _obstacleRayCount; i++)
            {
                float currentAngle = startAngle + (angleStep * i);
                Vector3 rayDirection = Quaternion.AngleAxis(currentAngle, Vector3.up) * transform.forward;

                if (Physics.Raycast(rayOrigin, rayDirection, _obstacleDetectionDistance, _obstacleLayerMask))
                {
                    _hasObstacleAhead = true;
                    Debug.DrawRay(rayOrigin, rayDirection * _obstacleDetectionDistance, Color.red);
                }
                else
                {
                    Debug.DrawRay(rayOrigin, rayDirection * _obstacleDetectionDistance, Color.green);
                }
            }
        }

        // 修改后的ScanForPlayer方法
        private void ScanForPlayer()
        {
            _hasPlayerInSight = false;
            _isPlayerBlocked = false; // 重置遮挡状态
            Transform previousPlayer = _detectedPlayer;
            _detectedPlayer = null;

            // 获取头部朝向，如果头部转动被禁用，则使用身体朝向
            Vector3 detectionDirection = _enableHeadTurn ? GetHeadLookDirection() : transform.forward;

            // 使用OverlapSphere检测范围内的所有碰撞体
            Collider[] colliders = Physics.OverlapSphere(transform.position, _playerDetectionDistance);

            Debug.Log($"NPC {gameObject.name}: 检测到 {colliders.Length} 个碰撞体"); // 调试信息

            foreach (Collider collider in colliders)
            {
                // 检查标签是否匹配
                if (collider.CompareTag(_playerTag))
                {
                    Debug.Log($"NPC {gameObject.name}: 找到玩家 {collider.name}"); // 调试信息

                    Vector3 directionToPlayer = (collider.transform.position - transform.position).normalized;
                    float angleToPlayer = Vector3.Angle(detectionDirection, directionToPlayer);

                    Debug.Log($"NPC {gameObject.name}: 到玩家的角度 {angleToPlayer:F1}°, 检测角度范围 {_playerDetectionAngle / 2f:F1}°"); // 调试信息

                    // 检查是否在检测角度范围内
                    if (angleToPlayer <= _playerDetectionAngle / 2f)
                    {
                        // 从NPC眼部位置发射射线到玩家
                        Vector3 eyePosition = transform.position + Vector3.up * 1.7f; // 假设眼部高度
                        Vector3 playerCenter = collider.bounds.center; // 使用玩家碰撞体中心
                        Vector3 rayDirection = (playerCenter - eyePosition).normalized;
                        float distanceToPlayer = Vector3.Distance(eyePosition, playerCenter);

                        Debug.DrawRay(eyePosition, rayDirection * distanceToPlayer, Color.blue, 0.1f); // 调试射线

                        RaycastHit hit;
                        if (Physics.Raycast(eyePosition, rayDirection, out hit, distanceToPlayer, ~0)) // 检测所有图层
                        {
                            Debug.Log($"NPC {gameObject.name}: 射线击中 {hit.collider.name}, 标签: {hit.collider.tag}"); // 调试信息

                            // 如果射线首先击中的是玩家，说明没有遮挡
                            if (hit.collider.CompareTag(_playerTag))
                            {
                                _hasPlayerInSight = true;
                                _detectedPlayer = collider.transform;
                                _isPlayerBlocked = false;

                                Debug.Log($"NPC {gameObject.name}: 玩家在视线中，无遮挡"); // 调试信息

                                // 如果之前没有锁定玩家，现在锁定
                                if (!_isPlayerLocked)
                                {
                                    LockOntoPlayer(collider.transform);
                                }

                                Debug.DrawLine(transform.position, collider.transform.position, Color.green, 0.1f);
                                break;
                            }
                            else
                            {
                                // 射线击中了其他物体，检查是否为阻挡物
                                if (IsBlockingObject(hit.collider))
                                {
                                    _hasPlayerInSight = true; // 能检测到玩家
                                    _detectedPlayer = collider.transform;
                                    _isPlayerBlocked = true; // 但被遮挡了

                                    Debug.Log($"NPC {gameObject.name}: 玩家被 {hit.collider.name} 遮挡"); // 调试信息

                                    // 仍然可以锁定玩家，但标记为被遮挡
                                    if (!_isPlayerLocked)
                                    {
                                        LockOntoPlayer(collider.transform);
                                    }

                                    Debug.DrawLine(transform.position, hit.point, Color.yellow, 0.1f);
                                    Debug.DrawLine(hit.point, collider.transform.position, Color.red, 0.1f);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // 射线没有击中任何东西，说明路径清晰
                            _hasPlayerInSight = true;
                            _detectedPlayer = collider.transform;
                            _isPlayerBlocked = false;

                            Debug.Log($"NPC {gameObject.name}: 射线未击中任何物体，玩家可见"); // 调试信息

                            if (!_isPlayerLocked)
                            {
                                LockOntoPlayer(collider.transform);
                            }

                            Debug.DrawLine(transform.position, collider.transform.position, Color.green, 0.1f);
                            break;
                        }
                    }
                    else
                    {
                        Debug.Log($"NPC {gameObject.name}: 玩家不在检测角度范围内"); // 调试信息
                    }
                }
            }

            // 如果之前检测到玩家但现在没有，释放锁定
            if (previousPlayer != null && _detectedPlayer == null && _isPlayerLocked)
            {
                ReleasePlayerLock();
                Debug.Log($"NPC {gameObject.name}: 释放玩家锁定"); // 调试信息
            }
        }

        // 新增方法：判断物体是否为阻挡物
        private bool IsBlockingObject(Collider collider)
        {
            // 检查图层是否在阻挡图层遮罩中
            int objectLayer = collider.gameObject.layer;
            return (_blockingLayerMask.value & (1 << objectLayer)) != 0;
        }

        #endregion

        #region Path Following

        public void StartPathFollowing()
        {
            if (_waypoints != null && _waypoints.Length > 0)
            {
                _isFollowingPath = true;
                _currentWaypointIndex = 0;
                SetNextWaypoint();
            }
        }

        public void StopPathFollowing()
        {
            _isFollowingPath = false;
        }

        private void SetNextWaypoint()
        {
            if (_waypoints == null || _waypoints.Length == 0) return;
            // Direct movement to waypoint, no NavMesh needed
        }

        private void UpdatePathFollowing()
        {
            if (!_isFollowingPath || _waypoints == null || _waypoints.Length == 0) return;

            if (_isWaitingAtWaypoint)
            {
                _waypointWaitTimer -= Time.deltaTime;
                if (_waypointWaitTimer <= 0f)
                {
                    _isWaitingAtWaypoint = false;
                    MoveToNextWaypoint();
                }
                return;
            }

            Vector3 currentTarget = _waypoints[_currentWaypointIndex].position;
            float distanceToWaypoint = Vector3.Distance(transform.position, currentTarget);

            if (distanceToWaypoint <= _waypointReachDistance)
            {
                _isWaitingAtWaypoint = true;
                _waypointWaitTimer = _waypointWaitTime;
                StopMovement();
            }
            else
            {
                // Direct movement to waypoint
                MoveTowards(currentTarget);
            }
        }

        private void MoveToNextWaypoint()
        {
            _currentWaypointIndex++;

            if (_currentWaypointIndex >= _waypoints.Length)
            {
                if (_loopWaypoints)
                {
                    _currentWaypointIndex = 0;
                }
                else
                {
                    _isFollowingPath = false;
                    return;
                }
            }

            SetNextWaypoint();
        }

        public Vector3 GetCurrentWaypointTarget()
        {
            if (_waypoints != null && _waypoints.Length > 0 && _currentWaypointIndex < _waypoints.Length)
            {
                return _waypoints[_currentWaypointIndex].position;
            }
            return transform.position;
        }

        public void InterruptPathFollowing()
        {
            // Simple interruption without NavMesh
        }

        public void ResumePathFollowing()
        {
            if (_isFollowingPath)
            {
                SetNextWaypoint();
            }
        }

        #endregion

        #region Movement Control

        public void SetMoveDirection(Vector3 direction)
        {
            _moveDirection = direction.normalized;
            _movementInputHeld = direction.magnitude > 0.1f;
            _movementInputPressed = _movementInputHeld;
            _movementInputTapped = false;
        }

        public void SetGaitState(NPCGaitState gait)
        {
            switch (gait)
            {
                case NPCGaitState.Walk:
                    _isWalking = true;
                    _isSprinting = false;
                    break;
                case NPCGaitState.Run:
                    _isWalking = false;
                    _isSprinting = false;
                    break;
                case NPCGaitState.Sprint:
                    _isWalking = false;
                    _isSprinting = true;
                    break;
                default:
                    _isWalking = false;
                    _isSprinting = false;
                    break;
            }
        }

        public void LookAtTarget(Vector3 targetPosition)
        {
            Vector3 lookDirection = (targetPosition - transform.position).normalized;
            lookDirection.y = 0;

            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSmoothing * Time.deltaTime);
            }
        }

        public void StopMovement()
        {
            SetMoveDirection(Vector3.zero);
        }

        public void MoveTowards(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0;
            SetMoveDirection(direction);
        }

        #endregion

        #region Animation State Machine

        private void SwitchState(AnimationState newState)
        {
            ExitCurrentState();
            EnterState(newState);
        }

        private void EnterState(AnimationState stateToEnter)
        {
            _currentState = stateToEnter;
            switch (_currentState)
            {
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
            // State cleanup if needed
        }

        private void EnterLocomotionState()
        {
            _previousRotation = transform.forward;
        }

        private void UpdateLocomotionState()
        {
            GroundedCheck();

            if (!_isGrounded)
            {
                SwitchState(AnimationState.Fall);
            }

            CalculateMoveDirection();
            CheckIfStarting();
            CheckIfStopped();
            FaceMoveDirection();
            Move();
            UpdateAnimatorController();
        }

        private void EnterJumpState()
        {
            _velocity = new Vector3(_velocity.x, _jumpForce, _velocity.z);
        }

        private void UpdateJumpState()
        {
            ApplyGravity();
            if (_velocity.y <= 0f)
            {
                SwitchState(AnimationState.Fall);
            }
            GroundedCheck();
            CalculateMoveDirection();
            FaceMoveDirection();
            Move();
            UpdateAnimatorController();
        }

        private void EnterFallState()
        {
            _fallStartTime = Time.time;
            _fallingDuration = 0f;
            _velocity.y = 0f;
        }

        private void UpdateFallState()
        {
            GroundedCheck();
            CalculateMoveDirection();
            FaceMoveDirection();
            ApplyGravity();
            Move();
            UpdateAnimatorController();

            if (_controller.isGrounded)
            {
                SwitchState(AnimationState.Locomotion);
            }

            _fallingDuration = Time.time - _fallStartTime;
        }

        private void EnterCrouchState()
        {
            // Crouch setup
        }

        private void UpdateCrouchState()
        {
            GroundedCheck();
            if (!_isGrounded)
            {
                SwitchState(AnimationState.Fall);
            }

            CalculateMoveDirection();
            CheckIfStarting();
            CheckIfStopped();
            FaceMoveDirection();
            Move();
            UpdateAnimatorController();
        }

        #endregion

        #region Core Movement

        private void CalculateMoveDirection()
        {
            if (!_isGrounded)
            {
                _targetMaxSpeed = _currentMaxSpeed;
            }
            else if (_isCrouching)
            {
                _targetMaxSpeed = _walkSpeed;
            }
            else if (_isSprinting)
            {
                _targetMaxSpeed = _sprintSpeed;
            }
            else if (_isWalking)
            {
                _targetMaxSpeed = _walkSpeed;
            }
            else
            {
                _targetMaxSpeed = _runSpeed;
            }

            _currentMaxSpeed = Mathf.Lerp(_currentMaxSpeed, _targetMaxSpeed, _ANIMATION_DAMP_TIME * Time.deltaTime);

            _targetVelocity.x = _moveDirection.x * _currentMaxSpeed;
            _targetVelocity.z = _moveDirection.z * _currentMaxSpeed;

            _velocity.z = Mathf.Lerp(_velocity.z, _targetVelocity.z, _speedChangeDamping * Time.deltaTime);
            _velocity.x = Mathf.Lerp(_velocity.x, _targetVelocity.x, _speedChangeDamping * Time.deltaTime);

            _speed2D = new Vector3(_velocity.x, 0f, _velocity.z).magnitude;
            _speed2D = Mathf.Round(_speed2D * 1000f) / 1000f;

            Vector3 playerForwardVector = transform.forward;
            _newDirectionDifferenceAngle = playerForwardVector != _moveDirection
                ? Vector3.SignedAngle(playerForwardVector, _moveDirection, Vector3.up)
                : 0f;

            CalculateGait();
        }

        private void CalculateGait()
        {
            float runThreshold = (_walkSpeed + _runSpeed) / 2;
            float sprintThreshold = (_runSpeed + _sprintSpeed) / 2;

            if (_speed2D < 0.01)
            {
                _currentGait = GaitState.Idle;
            }
            else if (_speed2D < runThreshold)
            {
                _currentGait = GaitState.Walk;
            }
            else if (_speed2D < sprintThreshold)
            {
                _currentGait = GaitState.Run;
            }
            else
            {
                _currentGait = GaitState.Sprint;
            }
        }

        private void FaceMoveDirection()
        {
            Vector3 faceDirection = new Vector3(_velocity.x, 0f, _velocity.z);
            if (faceDirection == Vector3.zero) return;

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(faceDirection),
                _rotationSmoothing * Time.deltaTime
            );
        }

        private void CheckIfStopped()
        {
            _isStopped = _moveDirection.magnitude == 0 && _speed2D < .5;
        }

        private void CheckIfStarting()
        {
            _locomotionStartTimer = VariableOverrideDelayTimer(_locomotionStartTimer);
            bool isStartingCheck = false;

            if (_locomotionStartTimer <= 0.0f)
            {
                if (_moveDirection.magnitude > 0.01 && _speed2D < 1 && !_isStrafing)
                {
                    isStartingCheck = true;
                }

                if (isStartingCheck)
                {
                    if (!_isStarting)
                    {
                        _locomotionStartDirection = _newDirectionDifferenceAngle;
                    }

                    float delayTime = 0.2f;
                    _leanDelay = delayTime;
                    _headLookDelay = delayTime;
                    _bodyLookDelay = delayTime;
                    _locomotionStartTimer = delayTime;
                }
            }
            else
            {
                isStartingCheck = true;
            }

            _isStarting = isStartingCheck;
        }

        private void Move()
        {
            _controller.Move(_velocity * Time.deltaTime);
        }

        private void ApplyGravity()
        {
            if (_velocity.y > Physics.gravity.y)
            {
                _velocity.y += Physics.gravity.y * _gravityMultiplier * Time.deltaTime;
            }
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(
                _controller.transform.position.x,
                _controller.transform.position.y - _groundedOffset,
                _controller.transform.position.z
            );
            _isGrounded = Physics.CheckSphere(spherePosition, _controller.radius, _groundLayerMask, QueryTriggerInteraction.Ignore);

            if (_isGrounded)
            {
                GroundInclineCheck();
            }
        }

        private void GroundInclineCheck()
        {
            if (_rearRayPos == null || _frontRayPos == null) return;

            float rayDistance = Mathf.Infinity;
            _rearRayPos.rotation = Quaternion.Euler(transform.rotation.x, 0, 0);
            _frontRayPos.rotation = Quaternion.Euler(transform.rotation.x, 0, 0);

            Physics.Raycast(_rearRayPos.position, _rearRayPos.TransformDirection(-Vector3.up), out RaycastHit rearHit, rayDistance, _groundLayerMask);
            Physics.Raycast(_frontRayPos.position, _frontRayPos.TransformDirection(-Vector3.up), out RaycastHit frontHit, rayDistance, _groundLayerMask);

            Vector3 hitDifference = frontHit.point - rearHit.point;
            float xPlaneLength = new Vector2(hitDifference.x, hitDifference.z).magnitude;

            _inclineAngle = Mathf.Lerp(_inclineAngle, Mathf.Atan2(hitDifference.y, xPlaneLength) * Mathf.Rad2Deg, 20f * Time.deltaTime);
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

        #region Animation Updates

        private void UpdateAnimatorController()
        {
            _animator.SetFloat(_leanValueHash, _leanValue);
            _animator.SetFloat(_headLookXHash, _headLookX);
            _animator.SetFloat(_headLookYHash, _headLookY);
            _animator.SetFloat(_bodyLookXHash, _bodyLookX);
            _animator.SetFloat(_bodyLookYHash, _bodyLookY);
            _animator.SetFloat(_isStrafingHash, _isStrafing ? 1.0f : 0.0f);
            _animator.SetFloat(_inclineAngleHash, _inclineAngle);
            _animator.SetFloat(_moveSpeedHash, _speed2D);
            _animator.SetInteger(_currentGaitHash, (int)_currentGait);
            _animator.SetFloat(_strafeDirectionXHash, _strafeDirectionX);
            _animator.SetFloat(_strafeDirectionZHash, _strafeDirectionZ);
            _animator.SetFloat(_forwardStrafeHash, _forwardStrafe);
            _animator.SetBool(_movementInputHeldHash, _movementInputHeld);
            _animator.SetBool(_movementInputPressedHash, _movementInputPressed);
            _animator.SetBool(_movementInputTappedHash, _movementInputTapped);
            _animator.SetBool(_isTurningInPlaceHash, _isTurningInPlace);
            _animator.SetBool(_isCrouchingHash, _isCrouching);
            _animator.SetFloat(_fallingDurationHash, _fallingDuration);
            _animator.SetBool(_isGroundedHash, _isGrounded);
            _animator.SetBool(_isWalkingHash, _isWalking);
            _animator.SetBool(_isStoppedHash, _isStopped);
            _animator.SetFloat(_locomotionStartDirectionHash, _locomotionStartDirection);
        }

        #endregion
    }

    #region Behavior Tree System

    public abstract class BehaviorNode
    {
        public enum NodeState
        {
            Running,
            Success,
            Failure
        }

        public abstract NodeState Evaluate();
    }

    public class SelectorNode : BehaviorNode
    {
        private List<BehaviorNode> children = new List<BehaviorNode>();

        public SelectorNode(params BehaviorNode[] nodes)
        {
            children.AddRange(nodes);
        }

        public override NodeState Evaluate()
        {
            foreach (BehaviorNode child in children)
            {
                NodeState result = child.Evaluate();
                if (result == NodeState.Success || result == NodeState.Running)
                {
                    return result;
                }
            }
            return NodeState.Failure;
        }
    }

    public class SequenceNode : BehaviorNode
    {
        private List<BehaviorNode> children = new List<BehaviorNode>();

        public SequenceNode(params BehaviorNode[] nodes)
        {
            children.AddRange(nodes);
        }

        public override NodeState Evaluate()
        {
            foreach (BehaviorNode child in children)
            {
                NodeState result = child.Evaluate();
                if (result == NodeState.Failure || result == NodeState.Running)
                {
                    return result;
                }
            }
            return NodeState.Success;
        }
    }

    public class ConditionNode : BehaviorNode
    {
        private System.Func<bool> condition;

        public ConditionNode(System.Func<bool> condition)
        {
            this.condition = condition;
        }

        public override NodeState Evaluate()
        {
            return condition() ? NodeState.Success : NodeState.Failure;
        }
    }

    public class ActionNode : BehaviorNode
    {
        private System.Func<NodeState> action;

        public ActionNode(System.Func<NodeState> action)
        {
            this.action = action;
        }

        public override NodeState Evaluate()
        {
            return action();
        }
    }

    public class NPCBehaviorTree
    {
        private NPCAnimationController npc;
        private BehaviorNode rootNode;
        private Vector3 patrolTarget;
        private float lastPatrolTime;
        private float patrolInterval = 3f;

        public NPCBehaviorTree(NPCAnimationController npcController)
        {
            npc = npcController;
            BuildBehaviorTree();
        }

        private void BuildBehaviorTree()
        {
            rootNode = new SelectorNode(
                // Highest priority: Avoid obstacles
                new SequenceNode(
                    new ConditionNode(() => npc.HasObstacleAhead),
                    new ActionNode(AvoidObstacle)
                ),

                // Medium priority: Follow path
                new SequenceNode(
                    new ConditionNode(() => npc.EnablePathFollowing && npc.Waypoints != null && npc.Waypoints.Length > 0),
                    new ActionNode(FollowPath)
                ),

                // Lower priority: Stationary scanning (only when not moving and no path)
                new SequenceNode(
                    new ConditionNode(() => !npc.IsMoving &&
                                           (!npc.EnablePathFollowing || npc.Waypoints == null || npc.Waypoints.Length == 0)),
                    new ActionNode(StationaryScanning)
                ),

                // Lowest priority: Patrol behavior
                new ActionNode(PatrolBehavior)
            );
        }

        public void Update()
        {
            rootNode?.Evaluate();
        }

        private BehaviorNode.NodeState StationaryScanning()
        {
            if (!npc.IsScanningStationary)
            {
                npc.StartStationaryScanning();
            }

            npc.StopMovement();

            if (npc.IsScanningStationary)
            {
                return BehaviorNode.NodeState.Running;
            }

            return BehaviorNode.NodeState.Success;
        }

        private BehaviorNode.NodeState AvoidObstacle()
        {
            npc.InterruptPathFollowing();
            npc.StopStationaryScanning();

            Vector3 rayOrigin = npc.transform.position + Vector3.up * 0.5f;

            Vector3 leftDirection = Quaternion.AngleAxis(-45f, Vector3.up) * npc.transform.forward;
            bool leftBlocked = Physics.Raycast(rayOrigin, leftDirection, npc.ObstacleDetectionDistance, npc.ObstacleLayerMask);

            Vector3 rightDirection = Quaternion.AngleAxis(45f, Vector3.up) * npc.transform.forward;
            bool rightBlocked = Physics.Raycast(rayOrigin, rightDirection, npc.ObstacleDetectionDistance, npc.ObstacleLayerMask);

            Vector3 avoidDirection;

            if (!rightBlocked && leftBlocked)
            {
                avoidDirection = npc.transform.right;
            }
            else if (!leftBlocked && rightBlocked)
            {
                avoidDirection = -npc.transform.right;
            }
            else if (leftBlocked && rightBlocked)
            {
                avoidDirection = -npc.transform.forward;
            }
            else
            {
                avoidDirection = npc.transform.right;
            }

            npc.SetMoveDirection(avoidDirection);
            npc.SetGaitState(NPCAnimationController.NPCGaitState.Walk);

            return BehaviorNode.NodeState.Success;
        }

        private BehaviorNode.NodeState FollowPath()
        {
            if (!npc.IsFollowingPath)
            {
                npc.StartPathFollowing();
            }

            Vector3 target = npc.GetCurrentWaypointTarget();
            float distanceToTarget = Vector3.Distance(npc.transform.position, target);

            if (distanceToTarget > 0.5f)
            {
                npc.SetGaitState(NPCAnimationController.NPCGaitState.Walk);
                return BehaviorNode.NodeState.Running;
            }

            return BehaviorNode.NodeState.Success;
        }

        private BehaviorNode.NodeState PatrolBehavior()
        {
            if (Time.time - lastPatrolTime > patrolInterval || Vector3.Distance(npc.transform.position, patrolTarget) < 1f)
            {
                patrolTarget = npc.transform.position + new Vector3(
                    Random.Range(-10f, 10f),
                    0f,
                    Random.Range(-10f, 10f)
                );
                lastPatrolTime = Time.time;
                patrolInterval = Random.Range(2f, 5f);
            }

            if (Vector3.Distance(npc.transform.position, patrolTarget) > 0.5f)
            {
                npc.MoveTowards(patrolTarget);
                npc.SetGaitState(NPCAnimationController.NPCGaitState.Walk);
            }
            else
            {
                npc.StopMovement();
            }

            return BehaviorNode.NodeState.Running;
        }
    }

    #endregion
}