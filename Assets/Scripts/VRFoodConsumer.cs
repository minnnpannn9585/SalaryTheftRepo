using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class VRFoodConsumer : MonoBehaviour
{
    [Header("激光检测设置")]
    public Transform headTransform; // 头部Transform（通常是Main Camera）
    public float laserLength = 0.5f; // 激光长度
    public LayerMask foodLayerMask = -1; // 食物层级遮罩

    [Header("消费设置")]
    public float consumeDuration = 2f; // 吃东西的持续时间（秒）
    public GameObject eatingEffect; // 吃东西时的特效物体（挂在脸上）

    [Header("调试设置")]
    public bool showDebugLaser = true; // 是否显示激光射线

    // 私有变量
    private CharacterController characterController;
    private GameLogicSystem gameLogicSystem;

    // 消费状态
    private bool isEating = false; // 是否正在吃东西
    private Coroutine eatingCoroutine;

    // 速度加成相关
    private bool hasSpeedBoost = false;
    private float originalMoveSpeed = 2f; // 原始移动速度
    private float boostedMoveSpeed = 2f; // 加成后的移动速度
    private Coroutine speedBoostCoroutine;

    void Start()
    {
        // 获取组件
        characterController = GetComponent<CharacterController>();
        gameLogicSystem = FindObjectOfType<GameLogicSystem>();

        // 如果没有指定头部Transform，尝试找到Main Camera
        if (headTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                headTransform = mainCamera.transform;
            }
        }

        // 初始化特效状态
        if (eatingEffect != null)
        {
            eatingEffect.SetActive(false);
        }

        // 获取Character Controller的原始移动速度
        // 注意：Character Controller本身没有moveSpeed属性
        // 你可能需要根据你的移动脚本来调整这部分
        if (characterController != null)
        {
            // 这里假设你有一个移动脚本，你需要根据实际情况调整
            var moveProvider = GetComponent<ActionBasedContinuousMoveProvider>();
            if (moveProvider != null)
            {
                originalMoveSpeed = moveProvider.moveSpeed;
            }
        }
    }

    void Update()
    {
        // 激光检测食物并自动消费
        DetectAndConsumeFood();
    }

    /// <summary>
    /// 激光检测食物并自动消费
    /// </summary>
    private void DetectAndConsumeFood()
    {
        if (headTransform == null || isEating) return;

        // 从头部向前发射激光
        Ray ray = new Ray(headTransform.position, headTransform.forward);

        // 调试用激光显示
        if (showDebugLaser)
        {
            Debug.DrawRay(ray.origin, ray.direction * laserLength, Color.red);
        }

        // 检测碰撞
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, laserLength, foodLayerMask))
        {
            FoodItem foodItem = hit.collider.GetComponent<FoodItem>();
            if (foodItem != null)
            {
                // 激光碰到食物就直接开始吃
                if (eatingCoroutine != null)
                {
                    StopCoroutine(eatingCoroutine);
                }
                eatingCoroutine = StartCoroutine(ConsumeFood(foodItem));
            }
        }
    }

    /// <summary>
    /// 消费食物协程（带延迟）
    /// </summary>
    /// <param name="foodItem">要消费的食物</param>
    private IEnumerator ConsumeFood(FoodItem foodItem)
    {
        isEating = true;

        // 显示吃东西特效
        if (eatingEffect != null)
        {
            eatingEffect.SetActive(true);
        }

        Debug.Log($"开始食用: {foodItem.foodName}");

        // 获取食物效果
        FoodEffect effect = foodItem.GetFoodEffect();

        // 如果有压力减少效果，在吃东西过程中渐进式减少压力
        float stressReductionPerSecond = 0f;
        if (effect.hasStressReduction)
        {
            stressReductionPerSecond = effect.stressReduction / consumeDuration;
        }

        // 吃东西过程
        float elapsedTime = 0f;
        while (elapsedTime < consumeDuration)
        {
            elapsedTime += Time.deltaTime;

            // 渐进式减少压力
            if (effect.hasStressReduction && gameLogicSystem != null)
            {
                float stressToReduce = stressReductionPerSecond * Time.deltaTime;
                gameLogicSystem.ReduceStress(stressToReduce);
            }

            yield return null;
        }

        // 吃完后立即应用速度加成
        if (effect.hasSpeedBoost)
        {
            ApplySpeedBoost(effect.speedMultiplier, effect.speedBoostDuration);
            Debug.Log($"获得速度加成: {effect.speedMultiplier}x, 持续{effect.speedBoostDuration}秒");
        }

        // 隐藏吃东西特效
        if (eatingEffect != null)
        {
            eatingEffect.SetActive(false);
        }

        // 消费食物（删除物体）
        foodItem.OnConsume();

        Debug.Log($"食用完成: {foodItem.foodName}");

        isEating = false;
        eatingCoroutine = null;
    }

    /// <summary>
    /// 应用食物效果（现在只处理速度加成，压力减少在协程中处理）
    /// </summary>
    /// <param name="effect">食物效果</param>
    private void ApplyFoodEffect(FoodEffect effect)
    {
        // 速度加成现在在协程结束时应用
        // 压力减少现在在协程过程中渐进式处理
    }

    /// <summary>
    /// 应用速度加成
    /// </summary>
    /// <param name="multiplier">速度倍数</param>
    /// <param name="duration">持续时间</param>
    private void ApplySpeedBoost(float multiplier, float duration)
    {
        // 如果已经有速度加成，先停止之前的
        if (speedBoostCoroutine != null)
        {
            StopCoroutine(speedBoostCoroutine);
        }

        speedBoostCoroutine = StartCoroutine(SpeedBoostCoroutine(multiplier, duration));
    }

    /// <summary>
    /// 速度加成协程
    /// </summary>
    private IEnumerator SpeedBoostCoroutine(float multiplier, float duration)
    {
        hasSpeedBoost = true;
        boostedMoveSpeed = originalMoveSpeed * multiplier;

        // 应用速度加成到移动组件
        // 注意：这里需要根据你实际使用的移动脚本来调整
        var moveProvider = GetComponent<ActionBasedContinuousMoveProvider>();
        if (moveProvider != null)
        {
            moveProvider.moveSpeed = boostedMoveSpeed;
        }

        // 等待持续时间
        yield return new WaitForSeconds(duration);

        // 恢复原始速度
        hasSpeedBoost = false;
        if (moveProvider != null)
        {
            moveProvider.moveSpeed = originalMoveSpeed;
        }

        speedBoostCoroutine = null;
        Debug.Log("速度加成效果结束");
    }

    /// <summary>
    /// 手动触发吃东西（如果需要的话）
    /// 现在主要是激光自动触发，这个方法作为备用
    /// </summary>
    public void ManualConsumeFood()
    {
        if (headTransform == null || isEating) return;

        // 从头部向前发射激光
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
    /// 停止吃东西（如果需要中断的话）
    /// </summary>
    public void StopEating()
    {
        if (isEating && eatingCoroutine != null)
        {
            StopCoroutine(eatingCoroutine);

            // 隐藏特效
            if (eatingEffect != null)
            {
                eatingEffect.SetActive(false);
            }

            isEating = false;
            eatingCoroutine = null;
            Debug.Log("停止食用");
        }
    }

    // 属性访问器
    public bool IsEating => isEating;
    public bool HasSpeedBoost => hasSpeedBoost;
    public float CurrentMoveSpeed => hasSpeedBoost ? boostedMoveSpeed : originalMoveSpeed;

    // 调试显示
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