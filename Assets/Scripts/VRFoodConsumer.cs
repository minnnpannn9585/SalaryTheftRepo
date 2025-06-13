using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class VRFoodConsumer : MonoBehaviour
{
    [Header("����������")]
    public Transform headTransform; // ͷ��Transform��ͨ����Main Camera��
    public float laserLength = 0.5f; // ���ⳤ��
    public LayerMask foodLayerMask = -1; // ʳ��㼶����

    [Header("��������")]
    public float consumeDuration = 2f; // �Զ����ĳ���ʱ�䣨�룩
    public GameObject eatingEffect; // �Զ���ʱ����Ч���壨�������ϣ�

    [Header("��������")]
    public bool showDebugLaser = true; // �Ƿ���ʾ��������

    // ˽�б���
    private CharacterController characterController;
    private GameLogicSystem gameLogicSystem;

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
    /// ������ʳ�ﲢ�Զ�����
    /// </summary>
    private void DetectAndConsumeFood()
    {
        if (headTransform == null || isEating) return;

        // ��ͷ����ǰ���伤��
        Ray ray = new Ray(headTransform.position, headTransform.forward);

        // �����ü�����ʾ
        if (showDebugLaser)
        {
            Debug.DrawRay(ray.origin, ray.direction * laserLength, Color.red);
        }

        // �����ײ
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, laserLength, foodLayerMask))
        {
            FoodItem foodItem = hit.collider.GetComponent<FoodItem>();
            if (foodItem != null)
            {
                // ��������ʳ���ֱ�ӿ�ʼ��
                if (eatingCoroutine != null)
                {
                    StopCoroutine(eatingCoroutine);
                }
                eatingCoroutine = StartCoroutine(ConsumeFood(foodItem));
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

        // Ӧ���ٶȼӳɵ��ƶ����
        // ע�⣺������Ҫ������ʵ��ʹ�õ��ƶ��ű�������
        var moveProvider = GetComponent<ActionBasedContinuousMoveProvider>();
        if (moveProvider != null)
        {
            moveProvider.moveSpeed = boostedMoveSpeed;
        }

        // �ȴ�����ʱ��
        yield return new WaitForSeconds(duration);

        // �ָ�ԭʼ�ٶ�
        hasSpeedBoost = false;
        if (moveProvider != null)
        {
            moveProvider.moveSpeed = originalMoveSpeed;
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

        // ��ͷ����ǰ���伤��
        Ray ray = new Ray(headTransform.position, headTransform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, laserLength, foodLayerMask))
        {
            FoodItem foodItem = hit.collider.GetComponent<FoodItem>();
            if (foodItem != null)
            {
                if (eatingCoroutine != null)
                {
                    StopCoroutine(eatingCoroutine);
                }
                eatingCoroutine = StartCoroutine(ConsumeFood(foodItem));
            }
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
            eatingCoroutine = null;
            Debug.Log("ֹͣʳ��");
        }
    }

    // ���Է�����
    public bool IsEating => isEating;
    public bool HasSpeedBoost => hasSpeedBoost;
    public float CurrentMoveSpeed => hasSpeedBoost ? boostedMoveSpeed : originalMoveSpeed;

    // ������ʾ
    void OnDrawGizmosSelected()
    {
        if (headTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(headTransform.position, headTransform.forward * laserLength);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(headTransform.position + headTransform.forward * laserLength, 0.1f);
        }
    }
}