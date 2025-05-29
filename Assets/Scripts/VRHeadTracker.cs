using Synty.AnimationBaseLocomotion.Samples;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// VR头部追踪组件 - 独立版本（支持右摇杆优先级控制）
/// 将此脚本添加到你的角色GameObject上，实现VR设备头部追踪和相机固定功能
/// </summary>
public class VRHeadTracker : MonoBehaviour
{
    [Header("=== 引用设置 ===")]
    [Tooltip("角色的头部骨骼 (通常是Head bone)")]
    public Transform headBone;

    [Tooltip("VR主相机")]
    public Camera vrCamera;

    [Tooltip("XR Origin的Camera Offset (如果使用XR系统)")]
    public Transform cameraOffset;

    [Space(10)]
    [Header("=== 相机偏移设置 ===")]
    [Tooltip("相机相对于头部的位置偏移 (X:左右, Y:上下, Z:前后)")]
    public Vector3 cameraPositionOffset = new Vector3(0f, 0.1f, 0.1f);

    [Tooltip("相机相对于头部的旋转偏移 (度数)")]
    public Vector3 cameraRotationOffset = Vector3.zero;

    [Space(10)]
    [Header("=== 追踪设置 ===")]
    [Tooltip("是否启用头部旋转追踪（VR头部追踪的核心功能）")]
    public bool enableRotationTracking = true;

    [Tooltip("头部追踪的平滑程度 (数值越高越平滑但响应越慢)")]
    public float trackingSmoothing = 15f;

    [Space(10)]
    [Header("=== 旋转限制 ===")]
    [Tooltip("头部上下转动的角度限制 (X:最小角度, Y:最大角度)")]
    public Vector2 headPitchLimits = new Vector2(-60f, 60f);

    [Tooltip("头部左右转动的角度限制 (X:最小角度, Y:最大角度)")]
    public Vector2 headYawLimits = new Vector2(-90f, 90f);

    [Space(10)]
    [Header("=== 身体补偿 ===")]
    [Tooltip("当头部转动超过阈值时是否自动旋转身体")]
    public bool enableBodyCompensation = true;

    [Tooltip("触发身体旋转的头部左右转动阈值 (度数)")]
    public float bodyRotationThreshold = 60f;

    [Tooltip("身体旋转的速度")]
    public float bodyRotationSpeed = 90f;

    [Space(10)]
    [Header("=== 调试选项 ===")]
    [Tooltip("显示调试信息")]
    public bool showDebugInfo = false;

    [Tooltip("显示Gizmos")]
    public bool showGizmos = true;

    // 私有变量
    private Transform playerBody;
    private Vector3 initialHeadLocalPosition;
    private Quaternion initialHeadLocalRotation;
    private Vector3 initialCameraPosition;
    private Quaternion initialCameraRotation;

    private bool isInitialized = false;
    private bool vrDeviceConnected = false;

    // VR设备和平滑变量
    private InputDevice headDevice;

    // VR设备输入（只需要旋转数据）
    private Quaternion currentHeadRotation;

    // 平滑变量（只需要旋转）
    private Quaternion smoothedHeadRotation;

    // 身体旋转相关
    private float accumulatedBodyRotation = 0f;

    void Start()
    {
        InitializeHeadTracking();
    }

    void Update()
    {
        CheckVRDeviceConnection();
    }

    void LateUpdate()
    {
        if (isInitialized)
        {
            UpdateVRHeadTracking();
            UpdateCameraPosition();
        }
    }

    /// <summary>
    /// 初始化VR头部追踪系统
    /// </summary>
    private void InitializeHeadTracking()
    {
        // 获取玩家身体Transform
        playerBody = transform;

        // 自动查找头部骨骼（如果没有手动设置）
        if (headBone == null)
        {
            headBone = FindHeadBone();
        }

        // 自动查找VR相机（如果没有手动设置）
        if (vrCamera == null)
        {
            vrCamera = FindVRCamera();
        }

        // 自动查找Camera Offset（如果没有手动设置）
        if (cameraOffset == null)
        {
            cameraOffset = FindCameraOffset();
        }

        if (headBone != null)
        {
            // 记录头部的初始状态
            initialHeadLocalPosition = headBone.localPosition;
            initialHeadLocalRotation = headBone.localRotation;

            // 记录相机的初始状态
            if (vrCamera != null)
            {
                initialCameraPosition = vrCamera.transform.localPosition;
                initialCameraRotation = vrCamera.transform.localRotation;
            }

            // 初始化平滑变量（只需要旋转）
            smoothedHeadRotation = initialHeadLocalRotation;

            isInitialized = true;

            Debug.Log("VR头部追踪初始化成功！");
            Debug.Log($"头部骨骼: {headBone.name}");
            Debug.Log($"VR相机: {(vrCamera != null ? vrCamera.name : "未找到")}");
            Debug.Log($"Camera Offset: {(cameraOffset != null ? cameraOffset.name : "未找到")}");
        }
        else
        {
            Debug.LogError("VR头部追踪初始化失败：找不到头部骨骼！");
            Debug.LogError("请在Inspector中手动指定Head Bone，或确保角色层级中有名为'Head'的骨骼。");
        }
    }

    /// <summary>
    /// 检查VR设备连接状态
    /// </summary>
    private void CheckVRDeviceConnection()
    {
        headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        bool deviceConnected = headDevice.isValid;

        if (deviceConnected != vrDeviceConnected)
        {
            vrDeviceConnected = deviceConnected;

            if (vrDeviceConnected)
            {
                Debug.Log("VR设备已连接：" + headDevice.name);
            }
            else
            {
                Debug.Log("VR设备已断开连接");
            }
        }
    }

    /// <summary>
    /// 更新VR头部追踪（修正版本：支持右摇杆优先级）
    /// </summary>
    private void UpdateVRHeadTracking()
    {
        if (headBone == null) return;

        // 新增：检查右摇杆是否正在控制旋转
        VRPlayerAnimationController playerController = GetComponent<VRPlayerAnimationController>();
        if (playerController != null && playerController.IsThumbstickControllingRotation())
        {
            // 右摇杆优先，完全暂停VR头部追踪
            if (showDebugInfo)
            {
                Debug.Log("右摇杆优先：暂停VR头部追踪");
            }
            return;
        }

        bool hasVRInput = false;

        // 尝试获取VR设备输入
        if (vrDeviceConnected)
        {
            hasVRInput = GetVRHeadInput();
        }

        // 如果没有VR输入，使用相机作为备用
        if (!hasVRInput && vrCamera != null)
        {
            GetCameraInput();
            hasVRInput = true;
        }

        if (hasVRInput)
        {
            // 应用头部追踪
            ApplyHeadTracking();

            // 处理身体补偿
            if (enableBodyCompensation)
            {
                HandleBodyCompensation();
            }
        }

        // 显示调试信息
        if (showDebugInfo)
        {
            DisplayDebugInfo();
        }
    }

    /// <summary>
    /// 获取VR设备头部输入
    /// </summary>
    private bool GetVRHeadInput()
    {
        // VR头部追踪只需要获取旋转数据
        bool gotRotation = false;

        if (enableRotationTracking)
        {
            gotRotation = headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out currentHeadRotation);
        }

        return gotRotation;
    }

    /// <summary>
    /// 使用相机输入作为备用（非VR模式）
    /// </summary>
    private void GetCameraInput()
    {
        if (vrCamera == null) return;

        // 只获取相机的旋转，不获取位置
        currentHeadRotation = vrCamera.transform.localRotation;
    }

    /// <summary>
    /// 应用头部追踪到角色
    /// </summary>
    private void ApplyHeadTracking()
    {
        // 头部位置始终保持初始位置不变（VR中头部骨骼位置不应该因为头显移动而改变）
        Vector3 targetPosition = initialHeadLocalPosition;
        Quaternion targetRotation = initialHeadLocalRotation;

        // 只处理旋转追踪（这才是正确的VR头部追踪方式）
        if (enableRotationTracking)
        {
            // 将VR头部旋转转换为相对于身体的旋转
            Quaternion relativeRotation = Quaternion.Inverse(playerBody.rotation) * currentHeadRotation;

            // 应用旋转限制
            Vector3 eulerAngles = relativeRotation.eulerAngles;

            // 转换角度到-180到180范围
            if (eulerAngles.x > 180f) eulerAngles.x -= 360f;
            if (eulerAngles.y > 180f) eulerAngles.y -= 360f;
            if (eulerAngles.z > 180f) eulerAngles.z -= 360f;

            // 应用限制
            eulerAngles.x = Mathf.Clamp(eulerAngles.x, headPitchLimits.x, headPitchLimits.y);
            eulerAngles.y = Mathf.Clamp(eulerAngles.y, headYawLimits.x, headYawLimits.y);
            eulerAngles.z = 0f; // 通常不允许头部侧倾

            targetRotation = initialHeadLocalRotation * Quaternion.Euler(eulerAngles);
        }

        // 平滑插值应用到头部骨骼
        // 位置保持不变，只改变旋转
        smoothedHeadRotation = Quaternion.Lerp(smoothedHeadRotation, targetRotation, trackingSmoothing * Time.deltaTime);

        // 头部位置保持初始位置不变
        headBone.localPosition = initialHeadLocalPosition;
        headBone.localRotation = smoothedHeadRotation;
    }

    /// <summary>
    /// 处理身体补偿旋转（修正版本：支持右摇杆优先级控制）
    /// </summary>
    private void HandleBodyCompensation()
    {
        // 新增：检查右摇杆是否正在控制旋转
        VRPlayerAnimationController playerController = GetComponent<VRPlayerAnimationController>();
        if (playerController != null && playerController.IsThumbstickControllingRotation())
        {
            // 右摇杆优先，跳过身体补偿
            return;
        }

        // 原有逻辑：检查是否启用身体补偿
        if (!enableBodyCompensation) return;

        // 计算头部相对身体的Y轴旋转
        float headYaw = GetHeadYawAngle();

        // 如果头部转动超过阈值，旋转身体
        if (Mathf.Abs(headYaw) > bodyRotationThreshold)
        {
            float excessAngle = headYaw - Mathf.Sign(headYaw) * bodyRotationThreshold;
            float rotationAmount = excessAngle * bodyRotationSpeed * Time.deltaTime * 0.1f; // 缓慢旋转

            playerBody.Rotate(0, rotationAmount, 0);
            accumulatedBodyRotation += rotationAmount;
        }
    }

    /// <summary>
    /// 更新相机位置，使其固定在头部（修正版本：支持右摇杆优先级）
    /// </summary>
    private void UpdateCameraPosition()
    {
        if (headBone == null) return;

        // 新增：检查右摇杆是否正在控制
        VRPlayerAnimationController playerController = GetComponent<VRPlayerAnimationController>();
        if (playerController != null && playerController.IsThumbstickControllingRotation())
        {
            // 右摇杆控制时，不更新相机位置（让右摇杆控制相机）
            if (showDebugInfo)
            {
                Debug.Log("右摇杆优先：暂停相机位置更新");
            }
            return;
        }

        // 计算目标相机位置和旋转
        Vector3 targetCameraPosition = headBone.position + headBone.TransformDirection(cameraPositionOffset);
        Quaternion targetCameraRotation = headBone.rotation * Quaternion.Euler(cameraRotationOffset);

        // 根据使用的相机系统更新位置
        if (cameraOffset != null)
        {
            // 使用XR系统的Camera Offset
            UpdateCameraOffset(targetCameraPosition, targetCameraRotation);
        }
        else if (vrCamera != null)
        {
            // 直接控制相机
            UpdateCameraTransform(targetCameraPosition, targetCameraRotation);
        }
    }

    /// <summary>
    /// 更新Camera Offset位置和旋转
    /// </summary>
    private void UpdateCameraOffset(Vector3 targetPosition, Quaternion targetRotation)
    {
        cameraOffset.position = Vector3.Lerp(
            cameraOffset.position,
            targetPosition,
            trackingSmoothing * Time.deltaTime
        );

        cameraOffset.rotation = Quaternion.Lerp(
            cameraOffset.rotation,
            targetRotation,
            trackingSmoothing * Time.deltaTime
        );
    }

    /// <summary>
    /// 直接更新相机Transform
    /// </summary>
    private void UpdateCameraTransform(Vector3 targetPosition, Quaternion targetRotation)
    {
        vrCamera.transform.position = Vector3.Lerp(
            vrCamera.transform.position,
            targetPosition,
            trackingSmoothing * Time.deltaTime
        );

        vrCamera.transform.rotation = Quaternion.Lerp(
            vrCamera.transform.rotation,
            targetRotation,
            trackingSmoothing * Time.deltaTime
        );
    }

    /// <summary>
    /// 自动查找头部骨骼
    /// </summary>
    private Transform FindHeadBone()
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>();

        // 优先查找确切名称
        foreach (Transform child in allChildren)
        {
            if (child.name == "Head")
            {
                return child;
            }
        }

        // 然后查找包含head的名称
        foreach (Transform child in allChildren)
        {
            if (child.name.ToLower().Contains("head"))
            {
                return child;
            }
        }

        return null;
    }

    /// <summary>
    /// 自动查找VR相机
    /// </summary>
    private Camera FindVRCamera()
    {
        // 查找主相机
        Camera mainCam = Camera.main;
        if (mainCam != null) return mainCam;

        // 查找场景中的第一个相机
        Camera[] cameras = FindObjectsOfType<Camera>();
        if (cameras.Length > 0) return cameras[0];

        return null;
    }

    /// <summary>
    /// 自动查找Camera Offset
    /// </summary>
    private Transform FindCameraOffset()
    {
        // 查找XR Origin
        GameObject[] xrObjects = GameObject.FindGameObjectsWithTag("MainCamera");
        foreach (GameObject obj in xrObjects)
        {
            if (obj.name.Contains("XR Origin") || obj.name.Contains("XR Rig"))
            {
                Transform cameraOffsetChild = obj.transform.Find("Camera Offset");
                if (cameraOffsetChild != null) return cameraOffsetChild;
            }
        }

        // 直接查找Camera Offset对象
        GameObject cameraOffsetObj = GameObject.Find("Camera Offset");
        if (cameraOffsetObj != null) return cameraOffsetObj.transform;

        return null;
    }

    /// <summary>
    /// 获取头部相对身体的Y轴旋转角度
    /// </summary>
    public float GetHeadYawAngle()
    {
        if (headBone == null || playerBody == null) return 0f;

        Vector3 headForward = headBone.forward;
        Vector3 bodyForward = playerBody.forward;

        // 移除Y轴分量，只计算水平面上的角度
        headForward.y = 0;
        bodyForward.y = 0;

        return Vector3.SignedAngle(bodyForward, headForward, Vector3.up);
    }

    /// <summary>
    /// 重置头部追踪到初始状态
    /// </summary>
    public void ResetHeadTracking()
    {
        if (headBone != null)
        {
            headBone.localPosition = initialHeadLocalPosition;
            headBone.localRotation = initialHeadLocalRotation;

            smoothedHeadRotation = initialHeadLocalRotation;

            accumulatedBodyRotation = 0f;
        }

        Debug.Log("头部追踪已重置");
    }

    /// <summary>
    /// 显示调试信息
    /// </summary>
    private void DisplayDebugInfo()
    {
        if (headBone == null) return;

        Vector3 headEuler = headBone.localEulerAngles;
        float headYaw = GetHeadYawAngle();

        Debug.Log($"头部旋转: {headEuler:F1}, 头部偏航: {headYaw:F1}°, VR连接: {vrDeviceConnected}");
    }

    // Gizmos绘制用于调试
    void OnDrawGizmosSelected()
    {
        if (!showGizmos || headBone == null) return;

        // 绘制头部位置和朝向
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(headBone.position, 0.05f);
        Gizmos.DrawRay(headBone.position, headBone.forward * 0.3f);

        // 绘制相机目标位置
        Vector3 cameraTargetPos = headBone.position + headBone.TransformDirection(cameraPositionOffset);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(cameraTargetPos, 0.03f);
        Gizmos.DrawLine(headBone.position, cameraTargetPos);

        // 绘制旋转限制范围
        Gizmos.color = Color.yellow;
        Vector3 leftLimit = headBone.position + Quaternion.Euler(0, headYawLimits.x, 0) * headBone.forward * 0.2f;
        Vector3 rightLimit = headBone.position + Quaternion.Euler(0, headYawLimits.y, 0) * headBone.forward * 0.2f;
        Gizmos.DrawLine(headBone.position, leftLimit);
        Gizmos.DrawLine(headBone.position, rightLimit);
    }

    // 公共方法用于外部调用
    public bool IsVRConnected() => vrDeviceConnected;
    public bool IsInitialized() => isInitialized;
    public float GetCurrentHeadYaw() => GetHeadYawAngle();
    public Vector3 GetHeadPosition() => headBone != null ? headBone.position : Vector3.zero;
    public Quaternion GetHeadRotation() => headBone != null ? headBone.rotation : Quaternion.identity;

    /// <summary>
    /// 检查头部追踪是否被锁定
    /// </summary>
    /// <returns>是否被锁定</returns>
    public bool IsHeadTrackingLocked()
    {
        VRPlayerAnimationController playerController = GetComponent<VRPlayerAnimationController>();
        return playerController != null && playerController.IsThumbstickControllingRotation();
    }

    /// <summary>
    /// 手动设置头部bone引用
    /// </summary>
    public void SetHeadBone(Transform headTransform)
    {
        headBone = headTransform;
        if (headBone != null)
        {
            initialHeadLocalPosition = headBone.localPosition;
            initialHeadLocalRotation = headBone.localRotation;
        }
    }

    /// <summary>
    /// 手动设置相机引用
    /// </summary>
    public void SetVRCamera(Camera camera)
    {
        vrCamera = camera;
    }

    /// <summary>
    /// 手动设置Camera Offset引用
    /// </summary>
    public void SetCameraOffset(Transform offset)
    {
        cameraOffset = offset;
    }
}