using Synty.AnimationBaseLocomotion.Samples;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// VRͷ��׷����� - �����汾��֧����ҡ�����ȼ����ƣ�
/// ���˽ű���ӵ���Ľ�ɫGameObject�ϣ�ʵ��VR�豸ͷ��׷�ٺ�����̶�����
/// </summary>
public class VRHeadTracker : MonoBehaviour
{
    [Header("=== �������� ===")]
    [Tooltip("��ɫ��ͷ������ (ͨ����Head bone)")]
    public Transform headBone;

    [Tooltip("VR�����")]
    public Camera vrCamera;

    [Tooltip("XR Origin��Camera Offset (���ʹ��XRϵͳ)")]
    public Transform cameraOffset;

    [Space(10)]
    [Header("=== ���ƫ������ ===")]
    [Tooltip("��������ͷ����λ��ƫ�� (X:����, Y:����, Z:ǰ��)")]
    public Vector3 cameraPositionOffset = new Vector3(0f, 0.1f, 0.1f);

    [Tooltip("��������ͷ������תƫ�� (����)")]
    public Vector3 cameraRotationOffset = Vector3.zero;

    [Space(10)]
    [Header("=== ׷������ ===")]
    [Tooltip("�Ƿ�����ͷ����ת׷�٣�VRͷ��׷�ٵĺ��Ĺ��ܣ�")]
    public bool enableRotationTracking = true;

    [Tooltip("ͷ��׷�ٵ�ƽ���̶� (��ֵԽ��Խƽ������ӦԽ��)")]
    public float trackingSmoothing = 15f;

    [Space(10)]
    [Header("=== ��ת���� ===")]
    [Tooltip("ͷ������ת���ĽǶ����� (X:��С�Ƕ�, Y:���Ƕ�)")]
    public Vector2 headPitchLimits = new Vector2(-60f, 60f);

    [Tooltip("ͷ������ת���ĽǶ����� (X:��С�Ƕ�, Y:���Ƕ�)")]
    public Vector2 headYawLimits = new Vector2(-90f, 90f);

    [Space(10)]
    [Header("=== ���岹�� ===")]
    [Tooltip("��ͷ��ת��������ֵʱ�Ƿ��Զ���ת����")]
    public bool enableBodyCompensation = true;

    [Tooltip("����������ת��ͷ������ת����ֵ (����)")]
    public float bodyRotationThreshold = 60f;

    [Tooltip("������ת���ٶ�")]
    public float bodyRotationSpeed = 90f;

    [Space(10)]
    [Header("=== ����ѡ�� ===")]
    [Tooltip("��ʾ������Ϣ")]
    public bool showDebugInfo = false;

    [Tooltip("��ʾGizmos")]
    public bool showGizmos = true;

    // ˽�б���
    private Transform playerBody;
    private Vector3 initialHeadLocalPosition;
    private Quaternion initialHeadLocalRotation;
    private Vector3 initialCameraPosition;
    private Quaternion initialCameraRotation;

    private bool isInitialized = false;
    private bool vrDeviceConnected = false;

    // VR�豸��ƽ������
    private InputDevice headDevice;

    // VR�豸���루ֻ��Ҫ��ת���ݣ�
    private Quaternion currentHeadRotation;

    // ƽ��������ֻ��Ҫ��ת��
    private Quaternion smoothedHeadRotation;

    // ������ת���
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
    /// ��ʼ��VRͷ��׷��ϵͳ
    /// </summary>
    private void InitializeHeadTracking()
    {
        // ��ȡ�������Transform
        playerBody = transform;

        // �Զ�����ͷ�����������û���ֶ����ã�
        if (headBone == null)
        {
            headBone = FindHeadBone();
        }

        // �Զ�����VR��������û���ֶ����ã�
        if (vrCamera == null)
        {
            vrCamera = FindVRCamera();
        }

        // �Զ�����Camera Offset�����û���ֶ����ã�
        if (cameraOffset == null)
        {
            cameraOffset = FindCameraOffset();
        }

        if (headBone != null)
        {
            // ��¼ͷ���ĳ�ʼ״̬
            initialHeadLocalPosition = headBone.localPosition;
            initialHeadLocalRotation = headBone.localRotation;

            // ��¼����ĳ�ʼ״̬
            if (vrCamera != null)
            {
                initialCameraPosition = vrCamera.transform.localPosition;
                initialCameraRotation = vrCamera.transform.localRotation;
            }

            // ��ʼ��ƽ��������ֻ��Ҫ��ת��
            smoothedHeadRotation = initialHeadLocalRotation;

            isInitialized = true;

            Debug.Log("VRͷ��׷�ٳ�ʼ���ɹ���");
            Debug.Log($"ͷ������: {headBone.name}");
            Debug.Log($"VR���: {(vrCamera != null ? vrCamera.name : "δ�ҵ�")}");
            Debug.Log($"Camera Offset: {(cameraOffset != null ? cameraOffset.name : "δ�ҵ�")}");
        }
        else
        {
            Debug.LogError("VRͷ��׷�ٳ�ʼ��ʧ�ܣ��Ҳ���ͷ��������");
            Debug.LogError("����Inspector���ֶ�ָ��Head Bone����ȷ����ɫ�㼶������Ϊ'Head'�Ĺ�����");
        }
    }

    /// <summary>
    /// ���VR�豸����״̬
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
                Debug.Log("VR�豸�����ӣ�" + headDevice.name);
            }
            else
            {
                Debug.Log("VR�豸�ѶϿ�����");
            }
        }
    }

    /// <summary>
    /// ����VRͷ��׷�٣������汾��֧����ҡ�����ȼ���
    /// </summary>
    private void UpdateVRHeadTracking()
    {
        if (headBone == null) return;

        // �����������ҡ���Ƿ����ڿ�����ת
        VRPlayerAnimationController playerController = GetComponent<VRPlayerAnimationController>();
        if (playerController != null && playerController.IsThumbstickControllingRotation())
        {
            // ��ҡ�����ȣ���ȫ��ͣVRͷ��׷��
            if (showDebugInfo)
            {
                Debug.Log("��ҡ�����ȣ���ͣVRͷ��׷��");
            }
            return;
        }

        bool hasVRInput = false;

        // ���Ի�ȡVR�豸����
        if (vrDeviceConnected)
        {
            hasVRInput = GetVRHeadInput();
        }

        // ���û��VR���룬ʹ�������Ϊ����
        if (!hasVRInput && vrCamera != null)
        {
            GetCameraInput();
            hasVRInput = true;
        }

        if (hasVRInput)
        {
            // Ӧ��ͷ��׷��
            ApplyHeadTracking();

            // �������岹��
            if (enableBodyCompensation)
            {
                HandleBodyCompensation();
            }
        }

        // ��ʾ������Ϣ
        if (showDebugInfo)
        {
            DisplayDebugInfo();
        }
    }

    /// <summary>
    /// ��ȡVR�豸ͷ������
    /// </summary>
    private bool GetVRHeadInput()
    {
        // VRͷ��׷��ֻ��Ҫ��ȡ��ת����
        bool gotRotation = false;

        if (enableRotationTracking)
        {
            gotRotation = headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out currentHeadRotation);
        }

        return gotRotation;
    }

    /// <summary>
    /// ʹ�����������Ϊ���ã���VRģʽ��
    /// </summary>
    private void GetCameraInput()
    {
        if (vrCamera == null) return;

        // ֻ��ȡ�������ת������ȡλ��
        currentHeadRotation = vrCamera.transform.localRotation;
    }

    /// <summary>
    /// Ӧ��ͷ��׷�ٵ���ɫ
    /// </summary>
    private void ApplyHeadTracking()
    {
        // ͷ��λ��ʼ�ձ��ֳ�ʼλ�ò��䣨VR��ͷ������λ�ò�Ӧ����Ϊͷ���ƶ����ı䣩
        Vector3 targetPosition = initialHeadLocalPosition;
        Quaternion targetRotation = initialHeadLocalRotation;

        // ֻ������ת׷�٣��������ȷ��VRͷ��׷�ٷ�ʽ��
        if (enableRotationTracking)
        {
            // ��VRͷ����תת��Ϊ������������ת
            Quaternion relativeRotation = Quaternion.Inverse(playerBody.rotation) * currentHeadRotation;

            // Ӧ����ת����
            Vector3 eulerAngles = relativeRotation.eulerAngles;

            // ת���Ƕȵ�-180��180��Χ
            if (eulerAngles.x > 180f) eulerAngles.x -= 360f;
            if (eulerAngles.y > 180f) eulerAngles.y -= 360f;
            if (eulerAngles.z > 180f) eulerAngles.z -= 360f;

            // Ӧ������
            eulerAngles.x = Mathf.Clamp(eulerAngles.x, headPitchLimits.x, headPitchLimits.y);
            eulerAngles.y = Mathf.Clamp(eulerAngles.y, headYawLimits.x, headYawLimits.y);
            eulerAngles.z = 0f; // ͨ��������ͷ������

            targetRotation = initialHeadLocalRotation * Quaternion.Euler(eulerAngles);
        }

        // ƽ����ֵӦ�õ�ͷ������
        // λ�ñ��ֲ��䣬ֻ�ı���ת
        smoothedHeadRotation = Quaternion.Lerp(smoothedHeadRotation, targetRotation, trackingSmoothing * Time.deltaTime);

        // ͷ��λ�ñ��ֳ�ʼλ�ò���
        headBone.localPosition = initialHeadLocalPosition;
        headBone.localRotation = smoothedHeadRotation;
    }

    /// <summary>
    /// �������岹����ת�������汾��֧����ҡ�����ȼ����ƣ�
    /// </summary>
    private void HandleBodyCompensation()
    {
        // �����������ҡ���Ƿ����ڿ�����ת
        VRPlayerAnimationController playerController = GetComponent<VRPlayerAnimationController>();
        if (playerController != null && playerController.IsThumbstickControllingRotation())
        {
            // ��ҡ�����ȣ��������岹��
            return;
        }

        // ԭ���߼�������Ƿ��������岹��
        if (!enableBodyCompensation) return;

        // ����ͷ����������Y����ת
        float headYaw = GetHeadYawAngle();

        // ���ͷ��ת��������ֵ����ת����
        if (Mathf.Abs(headYaw) > bodyRotationThreshold)
        {
            float excessAngle = headYaw - Mathf.Sign(headYaw) * bodyRotationThreshold;
            float rotationAmount = excessAngle * bodyRotationSpeed * Time.deltaTime * 0.1f; // ������ת

            playerBody.Rotate(0, rotationAmount, 0);
            accumulatedBodyRotation += rotationAmount;
        }
    }

    /// <summary>
    /// �������λ�ã�ʹ��̶���ͷ���������汾��֧����ҡ�����ȼ���
    /// </summary>
    private void UpdateCameraPosition()
    {
        if (headBone == null) return;

        // �����������ҡ���Ƿ����ڿ���
        VRPlayerAnimationController playerController = GetComponent<VRPlayerAnimationController>();
        if (playerController != null && playerController.IsThumbstickControllingRotation())
        {
            // ��ҡ�˿���ʱ�����������λ�ã�����ҡ�˿��������
            if (showDebugInfo)
            {
                Debug.Log("��ҡ�����ȣ���ͣ���λ�ø���");
            }
            return;
        }

        // ����Ŀ�����λ�ú���ת
        Vector3 targetCameraPosition = headBone.position + headBone.TransformDirection(cameraPositionOffset);
        Quaternion targetCameraRotation = headBone.rotation * Quaternion.Euler(cameraRotationOffset);

        // ����ʹ�õ����ϵͳ����λ��
        if (cameraOffset != null)
        {
            // ʹ��XRϵͳ��Camera Offset
            UpdateCameraOffset(targetCameraPosition, targetCameraRotation);
        }
        else if (vrCamera != null)
        {
            // ֱ�ӿ������
            UpdateCameraTransform(targetCameraPosition, targetCameraRotation);
        }
    }

    /// <summary>
    /// ����Camera Offsetλ�ú���ת
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
    /// ֱ�Ӹ������Transform
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
    /// �Զ�����ͷ������
    /// </summary>
    private Transform FindHeadBone()
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>();

        // ���Ȳ���ȷ������
        foreach (Transform child in allChildren)
        {
            if (child.name == "Head")
            {
                return child;
            }
        }

        // Ȼ����Ұ���head������
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
    /// �Զ�����VR���
    /// </summary>
    private Camera FindVRCamera()
    {
        // ���������
        Camera mainCam = Camera.main;
        if (mainCam != null) return mainCam;

        // ���ҳ����еĵ�һ�����
        Camera[] cameras = FindObjectsOfType<Camera>();
        if (cameras.Length > 0) return cameras[0];

        return null;
    }

    /// <summary>
    /// �Զ�����Camera Offset
    /// </summary>
    private Transform FindCameraOffset()
    {
        // ����XR Origin
        GameObject[] xrObjects = GameObject.FindGameObjectsWithTag("MainCamera");
        foreach (GameObject obj in xrObjects)
        {
            if (obj.name.Contains("XR Origin") || obj.name.Contains("XR Rig"))
            {
                Transform cameraOffsetChild = obj.transform.Find("Camera Offset");
                if (cameraOffsetChild != null) return cameraOffsetChild;
            }
        }

        // ֱ�Ӳ���Camera Offset����
        GameObject cameraOffsetObj = GameObject.Find("Camera Offset");
        if (cameraOffsetObj != null) return cameraOffsetObj.transform;

        return null;
    }

    /// <summary>
    /// ��ȡͷ����������Y����ת�Ƕ�
    /// </summary>
    public float GetHeadYawAngle()
    {
        if (headBone == null || playerBody == null) return 0f;

        Vector3 headForward = headBone.forward;
        Vector3 bodyForward = playerBody.forward;

        // �Ƴ�Y�������ֻ����ˮƽ���ϵĽǶ�
        headForward.y = 0;
        bodyForward.y = 0;

        return Vector3.SignedAngle(bodyForward, headForward, Vector3.up);
    }

    /// <summary>
    /// ����ͷ��׷�ٵ���ʼ״̬
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

        Debug.Log("ͷ��׷��������");
    }

    /// <summary>
    /// ��ʾ������Ϣ
    /// </summary>
    private void DisplayDebugInfo()
    {
        if (headBone == null) return;

        Vector3 headEuler = headBone.localEulerAngles;
        float headYaw = GetHeadYawAngle();

        Debug.Log($"ͷ����ת: {headEuler:F1}, ͷ��ƫ��: {headYaw:F1}��, VR����: {vrDeviceConnected}");
    }

    // Gizmos�������ڵ���
    void OnDrawGizmosSelected()
    {
        if (!showGizmos || headBone == null) return;

        // ����ͷ��λ�úͳ���
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(headBone.position, 0.05f);
        Gizmos.DrawRay(headBone.position, headBone.forward * 0.3f);

        // �������Ŀ��λ��
        Vector3 cameraTargetPos = headBone.position + headBone.TransformDirection(cameraPositionOffset);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(cameraTargetPos, 0.03f);
        Gizmos.DrawLine(headBone.position, cameraTargetPos);

        // ������ת���Ʒ�Χ
        Gizmos.color = Color.yellow;
        Vector3 leftLimit = headBone.position + Quaternion.Euler(0, headYawLimits.x, 0) * headBone.forward * 0.2f;
        Vector3 rightLimit = headBone.position + Quaternion.Euler(0, headYawLimits.y, 0) * headBone.forward * 0.2f;
        Gizmos.DrawLine(headBone.position, leftLimit);
        Gizmos.DrawLine(headBone.position, rightLimit);
    }

    // �������������ⲿ����
    public bool IsVRConnected() => vrDeviceConnected;
    public bool IsInitialized() => isInitialized;
    public float GetCurrentHeadYaw() => GetHeadYawAngle();
    public Vector3 GetHeadPosition() => headBone != null ? headBone.position : Vector3.zero;
    public Quaternion GetHeadRotation() => headBone != null ? headBone.rotation : Quaternion.identity;

    /// <summary>
    /// ���ͷ��׷���Ƿ�����
    /// </summary>
    /// <returns>�Ƿ�����</returns>
    public bool IsHeadTrackingLocked()
    {
        VRPlayerAnimationController playerController = GetComponent<VRPlayerAnimationController>();
        return playerController != null && playerController.IsThumbstickControllingRotation();
    }

    /// <summary>
    /// �ֶ�����ͷ��bone����
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
    /// �ֶ������������
    /// </summary>
    public void SetVRCamera(Camera camera)
    {
        vrCamera = camera;
    }

    /// <summary>
    /// �ֶ�����Camera Offset����
    /// </summary>
    public void SetCameraOffset(Transform offset)
    {
        cameraOffset = offset;
    }
}