using UnityEngine;

public class SecurityCamera : MonoBehaviour
{
    [Header("��ת����")]
    public Transform head; // Ҫ��ת��ͷ������
    public float rotationSpeed = 30f; // ��ת�ٶȣ���/�룩
    public float maxRotationAngle = 60f; // �����ת�Ƕȣ����Ҹ�60�ȣ�

    [Header("�������")]
    public float detectionRange = 10f; // ��ⷶΧ
    public float detectionAngle = 45f; // ���Ƕȣ�Բ׶�ĽǶȣ�
    public float detectXAngle = 0f; // ��ⷶΧ��X���ϵ���ת�Ƕȣ�����/������б��
    public string playerTag = "Player"; // ��ұ�ǩ

    [Header("Spotlight����")]
    public Light spotlight; // Spotlight���
    public Color normalColor = Color.yellow; // ����״̬��ɫ
    public Color alertColor = Color.red; // �������ʱ����ɫ
    [Tooltip("�Ƿ񽫼�����ͬ����Spotlight�������Ǵ�Spotlightͬ������⣩")]
    public bool syncToSpotlight = false; // �Ƿ񽫼�����ͬ����Spotlight

    [Header("��������")]
    public bool enableDebug = true; // ���õ�����Ϣ

    [Header("״̬")]
    public bool playerDetected = false; // �Ƿ��⵽���

    private float currentRotation = 0f;
    private bool rotatingRight = true;

    void Start()
    {
        // ����Ƿ������head����
        if (head == null)
        {
            Debug.LogWarning("Head����δ���䣬��ʹ�ýű����صĶ�����ΪĬ��ֵ");
            head = transform;
        }

        // ��ʼ��Spotlight��ɫ
        if (spotlight != null)
        {
            spotlight.color = normalColor;

            // �������ͬ����Spotlight������Spotlight����
            if (syncToSpotlight)
            {
                spotlight.range = detectionRange;
                spotlight.spotAngle = detectionAngle;
            }
        }
    }

    void Update()
    {
        // �������ͬ����Spotlight����������Ӧ�õ�Spotlight
        if (syncToSpotlight && spotlight != null)
        {
            spotlight.range = detectionRange;
            spotlight.spotAngle = detectionAngle;
        }
        // ���ڼ�������ȫ��Inspector�е�ֵ���ƣ����ᱻ����

        // ��ת����ͷ
        RotateCamera();

        // ������
        DetectPlayer();

        // ����Spotlight��ɫ
        UpdateSpotlightColor();
    }

    void RotateCamera()
    {
        // ������ת
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

        // Ӧ����ת��head����
        head.localRotation = Quaternion.Euler(0, currentRotation, 0);
    }

    void DetectPlayer()
    {
        playerDetected = false;

        // �����ⷽ��Ӧ��X����ת��
        Vector3 detectionForward = Quaternion.AngleAxis(detectXAngle, head.right) * head.forward;

        // ʹ��Բ׶�μ�ⷶΧ
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange);

        if (enableDebug)
        {
            Debug.Log($"[SecurityCamera] ��⵽ {colliders.Length} ����ײ���� {detectionRange}m ��Χ��");
            Debug.Log($"[SecurityCamera] ��ⷽ��: {detectionForward}");
            Debug.Log($"[SecurityCamera] ���Ƕ�: {detectionAngle}��");
        }

        foreach (Collider collider in colliders)
        {
            if (enableDebug)
            {
                Debug.Log($"[SecurityCamera] �����ײ��: {collider.name}, Tag: '{collider.tag}'");
            }

            if (collider.CompareTag(playerTag))
            {
                Vector3 directionToPlayer = (collider.transform.position - transform.position);
                float distanceToPlayer = directionToPlayer.magnitude;
                directionToPlayer.Normalize();

                // ����Ƿ���Բ׶�Ƕ���
                float angleToPlayer = Vector3.Angle(detectionForward, directionToPlayer);

                if (enableDebug)
                {
                    Debug.Log($"[SecurityCamera] �ҵ����: {collider.name}");
                    Debug.Log($"[SecurityCamera] ��Ҿ���: {distanceToPlayer:F2}m");
                    Debug.Log($"[SecurityCamera] ��ҽǶ�: {angleToPlayer:F2}��, �������Ƕ�: {detectionAngle / 2f:F2}��");
                }

                // ֻ���Ƕȣ������ϰ���
                if (angleToPlayer <= detectionAngle / 2f)
                {
                    playerDetected = true;
                    if (enableDebug)
                    {
                        Debug.Log("[SecurityCamera] ? ��Ҽ��ɹ����������ϰ��");
                    }
                    break;
                }
                else
                {
                    if (enableDebug)
                    {
                        Debug.Log($"[SecurityCamera] ? ��ҽǶ� {angleToPlayer:F2}�� ������Χ {detectionAngle / 2f:F2}��");
                    }
                }
            }
        }

        if (enableDebug && colliders.Length == 0)
        {
            Debug.Log("[SecurityCamera] ?? û�м�⵽�κ���ײ�壬���detectionRange����");
        }

        if (enableDebug && !playerDetected)
        {
            Debug.Log("[SecurityCamera] ? ���ս����δ��⵽���");
        }
    }

    void UpdateSpotlightColor()
    {
        if (spotlight != null)
        {
            spotlight.color = playerDetected ? alertColor : normalColor;
        }
    }

    // ��Scene��ͼ�л���Բ׶�μ�ⷶΧ
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

        // ����Բ׶�μ�ⷶΧ
        DrawConeGizmo();
    }

    void DrawConeGizmo()
    {
        // ���û��head����ʹ��Ĭ�ϵ�transform
        Transform targetTransform = head != null ? head : transform;

        // �����ⷽ��Ӧ��X����ת��
        Vector3 forward = Quaternion.AngleAxis(detectXAngle, targetTransform.right) * targetTransform.forward;
        Vector3 position = transform.position;

        // ����Բ׶�ײ��İ뾶
        float coneRadius = detectionRange * Mathf.Tan(detectionAngle * 0.5f * Mathf.Deg2Rad);

        // ����Բ׶��������
        int segments = 16;
        Vector3[] circlePoints = new Vector3[segments];

        // ����Բ׶�ײ�Բ�ĵ�
        Vector3 endPoint = position + forward * detectionRange;
        Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
        Vector3 up = Vector3.Cross(right, forward).normalized;

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * 2f * Mathf.PI;
            Vector3 circlePoint = endPoint + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * coneRadius;
            circlePoints[i] = circlePoint;

            // ���ƴӶ��㵽Բ�ܵ���
            Gizmos.DrawLine(position, circlePoint);

            // ����Բ��
            if (i > 0)
            {
                Gizmos.DrawLine(circlePoints[i - 1], circlePoints[i]);
            }
        }

        // �պ�Բ��
        if (segments > 0)
        {
            Gizmos.DrawLine(circlePoints[segments - 1], circlePoints[0]);
        }

        // ������������
        Gizmos.DrawLine(position, endPoint);

        // ����һЩ����ԲȦ����ʾԲ׶�����״
        for (int i = 1; i <= 3; i++)
        {
            float distance = detectionRange * i / 4f;
            float radius = distance * Mathf.Tan(detectionAngle * 0.5f * Mathf.Deg2Rad);
            Vector3 center = position + forward * distance;

            // ���Ƹ���ԲȦ
            for (int j = 0; j < segments; j++)
            {
                float angle1 = (float)j / segments * 2f * Mathf.PI;
                float angle2 = (float)(j + 1) / segments * 2f * Mathf.PI;

                Vector3 point1 = center + (right * Mathf.Cos(angle1) + up * Mathf.Sin(angle1)) * radius;
                Vector3 point2 = center + (right * Mathf.Cos(angle2) + up * Mathf.Sin(angle2)) * radius;

                Gizmos.DrawLine(point1, point2);
            }
        }

        // ����X����ת�Ĳο��ߣ���ʾԭʼ����͵�����ĳ���
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(position, position + targetTransform.forward * detectionRange * 0.5f);

        // �ָ�ԭ������ɫ
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