using UnityEngine;

public class SecurityCamera : MonoBehaviour
{
    [Header("旋转设置")]
    public Transform head; // 要旋转的头部对象
    public float rotationSpeed = 30f; // 旋转速度（度/秒）
    public float maxRotationAngle = 60f; // 最大旋转角度（左右各60度）

    [Header("检测设置")]
    public float detectionRange = 10f; // 检测范围
    public float detectionAngle = 45f; // 检测角度（圆锥的角度）
    public float detectXAngle = 0f; // 检测范围在X轴上的旋转角度（向上/向下倾斜）
    public string playerTag = "Player"; // 玩家标签

    [Header("Spotlight设置")]
    public Light spotlight; // Spotlight组件
    public Color normalColor = Color.yellow; // 正常状态颜色
    public Color alertColor = Color.red; // 发现玩家时的颜色
    [Tooltip("是否将检测参数同步到Spotlight（而不是从Spotlight同步到检测）")]
    public bool syncToSpotlight = false; // 是否将检测参数同步到Spotlight

    [Header("调试设置")]
    public bool enableDebug = true; // 启用调试信息

    [Header("状态")]
    public bool playerDetected = false; // 是否检测到玩家

    private float currentRotation = 0f;
    private bool rotatingRight = true;

    void Start()
    {
        // 检查是否分配了head对象
        if (head == null)
        {
            Debug.LogWarning("Head对象未分配，将使用脚本挂载的对象作为默认值");
            head = transform;
        }

        // 初始化Spotlight颜色
        if (spotlight != null)
        {
            spotlight.color = normalColor;

            // 如果启用同步到Spotlight，设置Spotlight参数
            if (syncToSpotlight)
            {
                spotlight.range = detectionRange;
                spotlight.spotAngle = detectionAngle;
            }
        }
    }

    void Update()
    {
        // 如果启用同步到Spotlight，将检测参数应用到Spotlight
        if (syncToSpotlight && spotlight != null)
        {
            spotlight.range = detectionRange;
            spotlight.spotAngle = detectionAngle;
        }
        // 现在检测参数完全由Inspector中的值控制，不会被覆盖

        // 旋转摄像头
        RotateCamera();

        // 检测玩家
        DetectPlayer();

        // 更新Spotlight颜色
        UpdateSpotlightColor();
    }

    void RotateCamera()
    {
        // 计算旋转
        float rotationDelta = rotationSpeed * Time.deltaTime;

        if (rotatingRight)
        {
            currentRotation += rotationDelta;
            if (currentRotation >= maxRotationAngle)
            {
                currentRotation = maxRotationAngle;
                rotatingRight = false;
            }
        }
        else
        {
            currentRotation -= rotationDelta;
            if (currentRotation <= -maxRotationAngle)
            {
                currentRotation = -maxRotationAngle;
                rotatingRight = true;
            }
        }

        // 应用旋转到head对象
        head.localRotation = Quaternion.Euler(0, currentRotation, 0);
    }

    void DetectPlayer()
    {
        playerDetected = false;

        // 计算检测方向（应用X轴旋转）
        Vector3 detectionForward = Quaternion.AngleAxis(detectXAngle, head.right) * head.forward;

        // 使用圆锥形检测范围
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange);

        if (enableDebug)
        {
            Debug.Log($"[SecurityCamera] 检测到 {colliders.Length} 个碰撞体在 {detectionRange}m 范围内");
            Debug.Log($"[SecurityCamera] 检测方向: {detectionForward}");
            Debug.Log($"[SecurityCamera] 检测角度: {detectionAngle}°");
        }

        foreach (Collider collider in colliders)
        {
            if (enableDebug)
            {
                Debug.Log($"[SecurityCamera] 检查碰撞体: {collider.name}, Tag: '{collider.tag}'");
            }

            if (collider.CompareTag(playerTag))
            {
                Vector3 directionToPlayer = (collider.transform.position - transform.position);
                float distanceToPlayer = directionToPlayer.magnitude;
                directionToPlayer.Normalize();

                // 检查是否在圆锥角度内
                float angleToPlayer = Vector3.Angle(detectionForward, directionToPlayer);

                if (enableDebug)
                {
                    Debug.Log($"[SecurityCamera] 找到玩家: {collider.name}");
                    Debug.Log($"[SecurityCamera] 玩家距离: {distanceToPlayer:F2}m");
                    Debug.Log($"[SecurityCamera] 玩家角度: {angleToPlayer:F2}°, 最大允许角度: {detectionAngle / 2f:F2}°");
                }

                // 只检查角度，无视障碍物
                if (angleToPlayer <= detectionAngle / 2f)
                {
                    playerDetected = true;
                    if (enableDebug)
                    {
                        Debug.Log("[SecurityCamera] ? 玩家检测成功！（无视障碍物）");
                    }
                    break;
                }
                else
                {
                    if (enableDebug)
                    {
                        Debug.Log($"[SecurityCamera] ? 玩家角度 {angleToPlayer:F2}° 超出范围 {detectionAngle / 2f:F2}°");
                    }
                }
            }
        }

        if (enableDebug && colliders.Length == 0)
        {
            Debug.Log("[SecurityCamera] ?? 没有检测到任何碰撞体，检查detectionRange设置");
        }

        if (enableDebug && !playerDetected)
        {
            Debug.Log("[SecurityCamera] ? 最终结果：未检测到玩家");
        }
    }

    void UpdateSpotlightColor()
    {
        if (spotlight != null)
        {
            spotlight.color = playerDetected ? alertColor : normalColor;
        }
    }

    // 在Scene视图中绘制圆锥形检测范围
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = playerDetected ? alertColor : normalColor;
        }
        else
        {
            Gizmos.color = normalColor;
        }

        // 绘制圆锥形检测范围
        DrawConeGizmo();
    }

    void DrawConeGizmo()
    {
        // 如果没有head对象，使用默认的transform
        Transform targetTransform = head != null ? head : transform;

        // 计算检测方向（应用X轴旋转）
        Vector3 forward = Quaternion.AngleAxis(detectXAngle, targetTransform.right) * targetTransform.forward;
        Vector3 position = transform.position;

        // 计算圆锥底部的半径
        float coneRadius = detectionRange * Mathf.Tan(detectionAngle * 0.5f * Mathf.Deg2Rad);

        // 绘制圆锥的轮廓线
        int segments = 16;
        Vector3[] circlePoints = new Vector3[segments];

        // 计算圆锥底部圆的点
        Vector3 endPoint = position + forward * detectionRange;
        Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
        Vector3 up = Vector3.Cross(right, forward).normalized;

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * 2f * Mathf.PI;
            Vector3 circlePoint = endPoint + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * coneRadius;
            circlePoints[i] = circlePoint;

            // 绘制从顶点到圆周的线
            Gizmos.DrawLine(position, circlePoint);

            // 绘制圆周
            if (i > 0)
            {
                Gizmos.DrawLine(circlePoints[i - 1], circlePoints[i]);
            }
        }

        // 闭合圆周
        if (segments > 0)
        {
            Gizmos.DrawLine(circlePoints[segments - 1], circlePoints[0]);
        }

        // 绘制中心轴线
        Gizmos.DrawLine(position, endPoint);

        // 绘制一些辅助圆圈来显示圆锥体的形状
        for (int i = 1; i <= 3; i++)
        {
            float distance = detectionRange * i / 4f;
            float radius = distance * Mathf.Tan(detectionAngle * 0.5f * Mathf.Deg2Rad);
            Vector3 center = position + forward * distance;

            // 绘制辅助圆圈
            for (int j = 0; j < segments; j++)
            {
                float angle1 = (float)j / segments * 2f * Mathf.PI;
                float angle2 = (float)(j + 1) / segments * 2f * Mathf.PI;

                Vector3 point1 = center + (right * Mathf.Cos(angle1) + up * Mathf.Sin(angle1)) * radius;
                Vector3 point2 = center + (right * Mathf.Cos(angle2) + up * Mathf.Sin(angle2)) * radius;

                Gizmos.DrawLine(point1, point2);
            }
        }

        // 绘制X轴旋转的参考线（显示原始朝向和调整后的朝向）
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(position, position + targetTransform.forward * detectionRange * 0.5f);

        // 恢复原来的颜色
        if (Application.isPlaying)
        {
            Gizmos.color = playerDetected ? alertColor : normalColor;
        }
        else
        {
            Gizmos.color = normalColor;
        }
    }
}