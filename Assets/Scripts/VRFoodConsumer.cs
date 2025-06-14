using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;


public class VRFoodConsumer : MonoBehaviour
{
    [Header("����������")]
    public Transform headTransform; // ͷ��Transform��ͨ����Main Camera��
    public float laserLength = 0.5f; // ���ⳤ��
    public float coneAngle = 30f; // Բ׶�Ƕȣ��ȣ�
    public LayerMask foodLayerMask = -1; // ʳ��㼶����

    [Header("��������")]
    public float consumeDuration = 2f; // �Զ����ĳ���ʱ�䣨�룩
    public GameObject eatingEffect; // �Զ���ʱ����Ч���壨�������ϣ�

    [Header("UI����")]
    public Slider speedBoostSlider; // ����buff����ʱ���飨��ק���Slider�����

    [Header("��������")]
    public bool showDebugLaser = true; // �Ƿ���ʾ��������

    // ˽�б���
    private CharacterController characterController;
    private GameLogicSystem gameLogicSystem;
    private CharacterStatus characterStatus; // ��ӽ�ɫ״̬����

    // ����״̬
    private bool isEating = false; // �Ƿ����ڳԶ���
    private Coroutine eatingCoroutine;

    // �ٶȼӳ����
    private bool hasSpeedBoost = false;
    private float originalMoveSpeed = 2f; // ԭʼ�ƶ��ٶ�
    private float boostedMoveSpeed = 2f; // �ӳɺ���ƶ��ٶ�
    private Coroutine speedBoostCoroutine;

    void Start()
    {
        // ��ȡ���
        characterController = GetComponent<CharacterController>();
        gameLogicSystem = FindObjectOfType<GameLogicSystem>();
        characterStatus = GetComponent<CharacterStatus>(); // ��ȡ��ɫ״̬���

        // ���û��ָ��ͷ��Transform�������ҵ�Main Camera
        if (headTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                headTransform = mainCamera.transform;
            }
        }

        // ��ʼ����Ч״̬
        if (eatingEffect != null)
        {
            eatingEffect.SetActive(false);
        }

        // ��ʼ������buff UI״̬
        if (speedBoostSlider != null)
        {
            speedBoostSlider.gameObject.SetActive(false);
        }

        // ��ȡCharacter Controller��ԭʼ�ƶ��ٶ�
        // ע�⣺Character Controller����û��moveSpeed����
        // �������Ҫ��������ƶ��ű��������ⲿ��
        if (characterController != null)
        {
            // �����������һ���ƶ��ű�������Ҫ����ʵ���������
            var moveProvider = GetComponent<ActionBasedContinuousMoveProvider>();
            if (moveProvider != null)
            {
                originalMoveSpeed = moveProvider.moveSpeed;
            }
        }
    }

    void Update()
    {
        // ������ʳ�ﲢ�Զ�����
        DetectAndConsumeFood();
    }

    /// <summary>
    /// ������ʳ�ﲢ�Զ����ѣ�Բ׶�μ�⣩
    /// </summary>
    private void DetectAndConsumeFood()
    {
        if (headTransform == null || isEating) return;

        // ������Բ׶������ʾ
        if (showDebugLaser)
        {
            DrawDebugCone();
        }

        // Բ׶�μ��
        FoodItem closestFood = DetectFoodInCone();

        if (closestFood != null)
        {
            // ��������ʳ���ֱ�ӿ�ʼ��
            if (eatingCoroutine != null)
            {
                StopCoroutine(eatingCoroutine);
            }
            eatingCoroutine = StartCoroutine(ConsumeFood(closestFood));
        }
    }

    /// <summary>
    /// ��Բ׶��Χ�ڼ��ʳ��
    /// </summary>
    /// <returns>�����ʳ����û���򷵻�null</returns>
    private FoodItem DetectFoodInCone()
    {
        Vector3 headPosition = headTransform.position;
        Vector3 headForward = headTransform.forward;

        // ʹ��OverlapSphere�ҵ���Χ�ڵ�������ײ��
        Collider[] colliders = Physics.OverlapSphere(headPosition, laserLength, foodLayerMask);

        FoodItem closestFood = null;
        float closestDistance = float.MaxValue;

        foreach (Collider collider in colliders)
        {
            // ���㵽ʳ��ķ���
            Vector3 directionToFood = (collider.transform.position - headPosition).normalized;

            // ����Ƕ�
            float angle = Vector3.Angle(headForward, directionToFood);

            // ����Ƿ���Բ׶�Ƕ���
            if (angle <= coneAngle * 0.5f)
            {
                // ����Ƿ���ʳ�����
                FoodItem foodItem = collider.GetComponent<FoodItem>();
                if (foodItem != null)
                {
                    // �ҵ������ʳ��
                    float distance = Vector3.Distance(headPosition, collider.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestFood = foodItem;
                    }
                }
            }
        }

        return closestFood;
    }

    /// <summary>
    /// ���Ƶ����õ�Բ׶����
    /// </summary>
    private void DrawDebugCone()
    {
        Vector3 headPosition = headTransform.position;
        Vector3 headForward = headTransform.forward;
        Vector3 headUp = headTransform.up;
        Vector3 headRight = headTransform.right;

        // ����Բ׶ĩ�˵�Բ�ΰ뾶
        float coneRadius = laserLength * Mathf.Tan(coneAngle * 0.5f * Mathf.Deg2Rad);

        // Բ׶ĩ�����ĵ�
        Vector3 coneEndCenter = headPosition + headForward * laserLength;

        // ����������
        Debug.DrawRay(headPosition, headForward * laserLength, Color.red);

        // ����Բ׶��Ե�ߣ�8�����γ�Բ׶������
        int segments = 8;
        for (int i = 0; i < segments; i++)
        {
            float angle = (360f / segments) * i * Mathf.Deg2Rad;
            Vector3 direction = headUp * Mathf.Sin(angle) + headRight * Mathf.Cos(angle);
            Vector3 coneEdgePoint = coneEndCenter + direction * coneRadius;

            // ��ͷ����Բ׶��Ե����
            Debug.DrawLine(headPosition, coneEdgePoint, Color.yellow);

            // Բ׶ĩ�˵�Բ������
            if (i < segments - 1)
            {
                float nextAngle = (360f / segments) * (i + 1) * Mathf.Deg2Rad;
                Vector3 nextDirection = headUp * Mathf.Sin(nextAngle) + headRight * Mathf.Cos(nextAngle);
                Vector3 nextConeEdgePoint = coneEndCenter + nextDirection * coneRadius;
                Debug.DrawLine(coneEdgePoint, nextConeEdgePoint, Color.green);
            }
            else
            {
                // �������һ���ߵ���һ����
                Vector3 firstDirection = headUp * Mathf.Sin(0) + headRight * Mathf.Cos(0);
                Vector3 firstConeEdgePoint = coneEndCenter + firstDirection * coneRadius;
                Debug.DrawLine(coneEdgePoint, firstConeEdgePoint, Color.green);
            }
        }
    }

    /// <summary>
    /// ����ʳ��Э�̣����ӳ٣�
    /// </summary>
    /// <param name="foodItem">Ҫ���ѵ�ʳ��</param>
    private IEnumerator ConsumeFood(FoodItem foodItem)
    {
        isEating = true;

        // ��������״̬Ϊtrue
        if (characterStatus != null)
        {
            characterStatus.isSlackingAtWork = true;
        }

        // ��ʾ�Զ�����Ч
        if (eatingEffect != null)
        {
            eatingEffect.SetActive(true);
        }

        Debug.Log($"��ʼʳ��: {foodItem.foodName}");

        // ��ȡʳ��Ч��
        FoodEffect effect = foodItem.GetFoodEffect();

        // �����ѹ������Ч�����ڳԶ��������н���ʽ����ѹ��
        float stressReductionPerSecond = 0f;
        if (effect.hasStressReduction)
        {
            stressReductionPerSecond = effect.stressReduction / consumeDuration;
        }

        // �Զ�������
        float elapsedTime = 0f;
        while (elapsedTime < consumeDuration)
        {
            elapsedTime += Time.deltaTime;

            // ����ʽ����ѹ��
            if (effect.hasStressReduction && gameLogicSystem != null)
            {
                float stressToReduce = stressReductionPerSecond * Time.deltaTime;
                gameLogicSystem.ReduceStress(stressToReduce);
            }

            yield return null;
        }

        // ���������Ӧ���ٶȼӳ�
        if (effect.hasSpeedBoost)
        {
            ApplySpeedBoost(effect.speedMultiplier, effect.speedBoostDuration);
            Debug.Log($"����ٶȼӳ�: {effect.speedMultiplier}x, ����{effect.speedBoostDuration}��");
        }

        // ���سԶ�����Ч
        if (eatingEffect != null)
        {
            eatingEffect.SetActive(false);
        }

        // ����ʳ�ɾ�����壩
        foodItem.OnConsume();

        Debug.Log($"ʳ�����: {foodItem.foodName}");

        isEating = false;

        // ��������״̬Ϊfalse
        if (characterStatus != null)
        {
            characterStatus.isSlackingAtWork = false;
        }

        eatingCoroutine = null;
    }

    /// <summary>
    /// Ӧ��ʳ��Ч��������ֻ�����ٶȼӳɣ�ѹ��������Э���д���
    /// </summary>
    /// <param name="effect">ʳ��Ч��</param>
    private void ApplyFoodEffect(FoodEffect effect)
    {
        // �ٶȼӳ�������Э�̽���ʱӦ��
        // ѹ������������Э�̹����н���ʽ����
    }

    /// <summary>
    /// Ӧ���ٶȼӳ�
    /// </summary>
    /// <param name="multiplier">�ٶȱ���</param>
    /// <param name="duration">����ʱ��</param>
    private void ApplySpeedBoost(float multiplier, float duration)
    {
        // ����Ѿ����ٶȼӳɣ���ֹ֮ͣǰ��
        if (speedBoostCoroutine != null)
        {
            StopCoroutine(speedBoostCoroutine);
        }

        speedBoostCoroutine = StartCoroutine(SpeedBoostCoroutine(multiplier, duration));
    }

    /// <summary>
    /// �ٶȼӳ�Э��
    /// </summary>
    private IEnumerator SpeedBoostCoroutine(float multiplier, float duration)
    {
        hasSpeedBoost = true;
        boostedMoveSpeed = originalMoveSpeed * multiplier;

        // ��ʾ����buff UI
        if (speedBoostSlider != null)
        {
            speedBoostSlider.gameObject.SetActive(true);
            speedBoostSlider.maxValue = duration;
            speedBoostSlider.value = duration;
        }

        // Ӧ���ٶȼӳɵ��ƶ����
        // ע�⣺������Ҫ������ʵ��ʹ�õ��ƶ��ű�������
        var moveProvider = GetComponent<ActionBasedContinuousMoveProvider>();
        if (moveProvider != null)
        {
            moveProvider.moveSpeed = boostedMoveSpeed;
        }

        // ����ʱ����slider
        float remainingTime = duration;
        while (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            remainingTime = Mathf.Max(0, remainingTime);

            // ����sliderֵ
            if (speedBoostSlider != null)
            {
                speedBoostSlider.value = remainingTime;
            }

            yield return null;
        }

        // �ָ�ԭʼ�ٶ�
        hasSpeedBoost = false;
        if (moveProvider != null)
        {
            moveProvider.moveSpeed = originalMoveSpeed;
        }

        // ���ؼ���buff UI
        if (speedBoostSlider != null)
        {
            speedBoostSlider.gameObject.SetActive(false);
        }

        speedBoostCoroutine = null;
        Debug.Log("�ٶȼӳ�Ч������");
    }

    /// <summary>
    /// �ֶ������Զ����������Ҫ�Ļ���
    /// ������Ҫ�Ǽ����Զ����������������Ϊ����
    /// </summary>
    public void ManualConsumeFood()
    {
        if (headTransform == null || isEating) return;

        // ʹ��Բ׶�μ��
        FoodItem closestFood = DetectFoodInCone();

        if (closestFood != null)
        {
            if (eatingCoroutine != null)
            {
                StopCoroutine(eatingCoroutine);
            }
            eatingCoroutine = StartCoroutine(ConsumeFood(closestFood));
        }
    }

    /// <summary>
    /// ֹͣ�Զ����������Ҫ�жϵĻ���
    /// </summary>
    public void StopEating()
    {
        if (isEating && eatingCoroutine != null)
        {
            StopCoroutine(eatingCoroutine);

            // ������Ч
            if (eatingEffect != null)
            {
                eatingEffect.SetActive(false);
            }

            isEating = false;

            // ��������״̬Ϊfalse
            if (characterStatus != null)
            {
                characterStatus.isSlackingAtWork = false;
            }

            eatingCoroutine = null;
            Debug.Log("ֹͣʳ��");
        }
    }

    /// <summary>
    /// ֹͣ�ٶȼӳɣ������Ҫ�жϵĻ���
    /// </summary>
    public void StopSpeedBoost()
    {
        if (hasSpeedBoost && speedBoostCoroutine != null)
        {
            StopCoroutine(speedBoostCoroutine);

            // �ָ�ԭʼ�ٶ�
            hasSpeedBoost = false;
            var moveProvider = GetComponent<ActionBasedContinuousMoveProvider>();
            if (moveProvider != null)
            {
                moveProvider.moveSpeed = originalMoveSpeed;
            }

            // ���ؼ���buff UI
            if (speedBoostSlider != null)
            {
                speedBoostSlider.gameObject.SetActive(false);
            }

            speedBoostCoroutine = null;
            Debug.Log("�ٶȼӳ�Ч�����ж�");
        }
    }

    // ���Է�����
    public bool IsEating => isEating;
    public bool HasSpeedBoost => hasSpeedBoost;
    public float CurrentMoveSpeed => hasSpeedBoost ? boostedMoveSpeed : originalMoveSpeed;
    public float SpeedBoostTimeRemaining => speedBoostSlider != null ? speedBoostSlider.value : 0f;

    // ������ʾ
    void OnDrawGizmosSelected()
    {
        if (headTransform != null)
        {
            Vector3 headPosition = headTransform.position;
            Vector3 headForward = headTransform.forward;
            Vector3 headUp = headTransform.up;
            Vector3 headRight = headTransform.right;

            // ����Բ׶ĩ�˵�Բ�ΰ뾶
            float coneRadius = laserLength * Mathf.Tan(coneAngle * 0.5f * Mathf.Deg2Rad);
            Vector3 coneEndCenter = headPosition + headForward * laserLength;

            // ����Բ׶����
            Gizmos.color = Color.red;

            // ������
            Gizmos.DrawRay(headPosition, headForward * laserLength);

            // Բ׶��Ե��
            int segments = 12;
            Vector3[] conePoints = new Vector3[segments];

            for (int i = 0; i < segments; i++)
            {
                float angle = (360f / segments) * i * Mathf.Deg2Rad;
                Vector3 direction = headUp * Mathf.Sin(angle) + headRight * Mathf.Cos(angle);
                Vector3 coneEdgePoint = coneEndCenter + direction * coneRadius;
                conePoints[i] = coneEdgePoint;

                // ��ͷ����Բ׶��Ե����
                Gizmos.DrawLine(headPosition, coneEdgePoint);
            }

            // ����Բ׶ĩ�˵�Բ��
            Gizmos.color = Color.yellow;
            for (int i = 0; i < segments; i++)
            {
                int nextIndex = (i + 1) % segments;
                Gizmos.DrawLine(conePoints[i], conePoints[nextIndex]);
            }

            // ����Բ׶ĩ�˵����ĵ�
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(coneEndCenter, 0.05f);
        }
    }
}