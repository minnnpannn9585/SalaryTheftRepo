using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// VR��ӡ����ť������
/// ����VR�����еĴ�ӡ����ť����
/// </summary>
public class VRPrinterButton : MonoBehaviour
{
    [Header("��ӡ������")]
    [SerializeField] private PrinterSystem printerSystem; // ��ӡ��ϵͳ����
    [SerializeField] private TaskManager taskManager; // �������������

    [Header("��������")]
    [SerializeField] private XRSimpleInteractable buttonInteractable; // XR�������
    [SerializeField] private Transform buttonTransform; // ��ť�任����������Ӿ�������

    [Header("�Ӿ�����")]
    [SerializeField] private Vector3 pressedPosition = new Vector3(0, -0.01f, 0); // ����ʱ��λ��ƫ��
    [SerializeField] private Color normalColor = Color.white; // ������ɫ
    [SerializeField] private Color pressedColor = Color.green; // ����ʱ����ɫ
    [SerializeField] private Color disabledColor = Color.gray; // ����ʱ����ɫ
    [SerializeField] private Renderer buttonRenderer; // ��ť��Ⱦ��

    [Header("��Ƶ����")]
    [SerializeField] private AudioSource audioSource; // ��ƵԴ
    [SerializeField] private AudioClip buttonPressSound; // ��ť������Ч
    [SerializeField] private AudioClip buttonErrorSound; // ��ť������Ч

    [Header("��������")]
    [SerializeField] private float hapticIntensity = 0.5f; // ��������ǿ��
    [SerializeField] private float hapticDuration = 0.1f; // ������������ʱ��

    [Header("��������")]
    [SerializeField] private bool enableDebugLog = true; // ���õ�����־

    // ˽�б���
    private Vector3 originalPosition; // ԭʼλ��
    private Material buttonMaterial; // ��ť����
    private bool isPressed = false; // �Ƿ����ڰ���
    private bool canInteract = true; // �Ƿ���Խ���

    void Start()
    {
        InitializeButton();
    }

    /// <summary>
    /// ��ʼ����ť
    /// </summary>
    private void InitializeButton()
    {
        // ��ȡ�򴴽���Ҫ���
        if (buttonInteractable == null)
            buttonInteractable = GetComponent<XRSimpleInteractable>();

        if (buttonTransform == null)
            buttonTransform = transform;

        if (buttonRenderer == null)
            buttonRenderer = GetComponent<Renderer>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // ��¼ԭʼλ��
        originalPosition = buttonTransform.localPosition;

        // ���ð�ť����
        if (buttonRenderer != null)
        {
            buttonMaterial = buttonRenderer.material;
            SetButtonColor(normalColor);
        }

        // �󶨽����¼�
        if (buttonInteractable != null)
        {
            // �Ƽ�ʹ�� Select Events
            buttonInteractable.selectEntered.AddListener(OnButtonPressed);
            buttonInteractable.selectExited.AddListener(OnButtonReleased);

            // ��ѡ����� Hover Events ������ͣЧ��
            buttonInteractable.hoverEntered.AddListener(OnButtonHover);
            buttonInteractable.hoverExited.AddListener(OnButtonHoverExit);
        }

        if (enableDebugLog)
            Debug.Log("[VRPrinterButton] VR��ӡ����ť�ѳ�ʼ��");
    }

    /// <summary>
    /// ��ť������ʱ����
    /// </summary>
    /// <param name="args">ѡ���¼�����</param>
    private void OnButtonPressed(SelectEnterEventArgs args)
    {
        if (!canInteract || isPressed)
            return;

        isPressed = true;

        if (enableDebugLog)
            Debug.Log("[VRPrinterButton] ��ť������");

        // ����Ƿ���Դ�ӡ
        if (CanPrint())
        {
            // ִ�д�ӡ����
            ExecutePrint();

            // �Ӿ�����
            SetButtonPressed(true);
            SetButtonColor(pressedColor);

            // ��Ƶ����
            PlaySound(buttonPressSound);

            // ��������
            SendHapticFeedback(args.interactorObject);
        }
        else
        {
            // �޷���ӡʱ�ķ���
            SetButtonColor(disabledColor);
            PlaySound(buttonErrorSound);

            if (enableDebugLog)
                Debug.Log("[VRPrinterButton] �޷���ӡ��û�еȴ��Ĵ�ӡ����");
        }
    }

    /// <summary>
    /// ��ť�ͷ�ʱ����
    /// </summary>
    /// <param name="args">ѡ���˳��¼�����</param>
    private void OnButtonReleased(SelectExitEventArgs args)
    {
        if (!isPressed)
            return;

        isPressed = false;

        if (enableDebugLog)
            Debug.Log("[VRPrinterButton] ��ť�ͷ�");

        // �ָ���ť״̬
        SetButtonPressed(false);
        SetButtonColor(normalColor);
    }

    /// <summary>
    /// �����ͣ����
    /// </summary>
    /// <param name="args">��ͣ�����¼�����</param>
    private void OnButtonHover(HoverEnterEventArgs args)
    {
        if (!canInteract)
            return;

        // ��ͣʱ����΢����Ч��
        if (!isPressed)
        {
            Color hoverColor = Color.Lerp(normalColor, pressedColor, 0.3f);
            SetButtonColor(hoverColor);
        }

        if (enableDebugLog)
            Debug.Log("[VRPrinterButton] ��ͣ����");
    }

    /// <summary>
    /// �����ͣ�˳�
    /// </summary>
    /// <param name="args">��ͣ�˳��¼�����</param>
    private void OnButtonHoverExit(HoverExitEventArgs args)
    {
        if (!canInteract)
            return;

        // �ָ�������ɫ
        if (!isPressed)
        {
            SetButtonColor(normalColor);
        }

        if (enableDebugLog)
            Debug.Log("[VRPrinterButton] ��ͣ�˳�");
    }

    /// <summary>
    /// ����Ƿ���Դ�ӡ
    /// </summary>
    /// <returns>�Ƿ���Դ�ӡ</returns>
    private bool CanPrint()
    {
        // ����ӡ��ϵͳ�Ƿ�����ҵȴ���ӡ����
        if (printerSystem == null)
        {
            if (enableDebugLog)
                Debug.LogWarning("[VRPrinterButton] ��ӡ��ϵͳ����Ϊ��");
            return false;
        }

        // ���������Ӹ������߼�
        // ���磺����Ƿ���ֽ�š�īˮ��
        return true;
    }

    /// <summary>
    /// ִ�д�ӡ����
    /// </summary>
    private void ExecutePrint()
    {
        if (printerSystem != null)
        {
            // ������ӡ��ϵͳ�Ĵ�ӡ����
            // ���������Ҫ�������PrinterSystem�ľ���ʵ��������
            printerSystem.StartPrinting();

            if (enableDebugLog)
                Debug.Log("[VRPrinterButton] ��ӡ������ִ��");
        }

        if (taskManager != null)
        {
            // �����Ҫ֪ͨ���������
            // taskManager.OnPrintButtonPressed();
        }
    }

    /// <summary>
    /// ���ð�ť����״̬
    /// </summary>
    /// <param name="pressed">�Ƿ���</param>
    private void SetButtonPressed(bool pressed)
    {
        if (buttonTransform != null)
        {
            Vector3 targetPosition = pressed ? originalPosition + pressedPosition : originalPosition;
            buttonTransform.localPosition = targetPosition;
        }
    }

    /// <summary>
    /// ���ð�ť��ɫ
    /// </summary>
    /// <param name="color">Ŀ����ɫ</param>
    private void SetButtonColor(Color color)
    {
        if (buttonMaterial != null)
        {
            buttonMaterial.color = color;
        }
    }

    /// <summary>
    /// ������Ч
    /// </summary>
    /// <param name="clip">��Ƶ����</param>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// ���ʹ�������
    /// </summary>
    /// <param name="interactor">����������</param>
    private void SendHapticFeedback(IXRInteractor interactor)
    {
        if (interactor is XRBaseControllerInteractor controllerInteractor)
        {
            // ���ʹ���������������
            if (controllerInteractor.xrController is ActionBasedController actionController)
            {
                actionController.SendHapticImpulse(hapticIntensity, hapticDuration);
            }
            else if (controllerInteractor.xrController is XRController xrController)
            {
                xrController.SendHapticImpulse(hapticIntensity, hapticDuration);
            }
        }
    }

    /// <summary>
    /// ���ð�ť����״̬
    /// </summary>
    /// <param name="canInteract">�Ƿ���Խ���</param>
    public void SetInteractable(bool canInteract)
    {
        this.canInteract = canInteract;

        if (buttonInteractable != null)
        {
            buttonInteractable.enabled = canInteract;
        }

        // �����Ӿ�״̬
        Color targetColor = canInteract ? normalColor : disabledColor;
        SetButtonColor(targetColor);

        if (enableDebugLog)
            Debug.Log($"[VRPrinterButton] ��ť����״̬����Ϊ: {canInteract}");
    }

    /// <summary>
    /// �ֶ�������ӡ�����ڲ��ԣ�
    /// </summary>
    [ContextMenu("�ֶ�������ӡ")]
    public void ManualTriggerPrint()
    {
        if (CanPrint())
        {
            ExecutePrint();
            Debug.Log("[VRPrinterButton] �ֶ�������ӡ�ɹ�");
        }
        else
        {
            Debug.Log("[VRPrinterButton] �ֶ�������ӡʧ�ܣ��޷���ӡ");
        }
    }

    /// <summary>
    /// ��鰴ť״̬�������ã�
    /// </summary>
    [ContextMenu("��鰴ť״̬")]
    public void CheckButtonStatus()
    {
        Debug.Log($"[VRPrinterButton] === ��ť״̬��� ===");
        Debug.Log($"�Ƿ���Խ���: {canInteract}");
        Debug.Log($"�Ƿ����ڰ���: {isPressed}");
        Debug.Log($"��ӡ��ϵͳ����: {(printerSystem != null ? "������" : "δ����")}");
        Debug.Log($"�������������: {(taskManager != null ? "������" : "δ����")}");
        Debug.Log($"XR�������: {(buttonInteractable != null ? "������" : "δ����")}");
        Debug.Log($"�Ƿ���Դ�ӡ: {CanPrint()}");
    }

    void OnDestroy()
    {
        // �����¼�����
        if (buttonInteractable != null)
        {
            buttonInteractable.selectEntered.RemoveListener(OnButtonPressed);
            buttonInteractable.selectExited.RemoveListener(OnButtonReleased);
            buttonInteractable.hoverEntered.RemoveListener(OnButtonHover);
            buttonInteractable.hoverExited.RemoveListener(OnButtonHoverExit);
        }
    }
}