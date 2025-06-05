using UnityEngine;

public class BulbSway : MonoBehaviour
{
    [Header("摇摆设置")]
    [SerializeField] private float swayIntensity = 0.5f;        // 摇摆强度
    [SerializeField] private float swaySpeed = 1.0f;            // 摇摆速度
    [SerializeField] private float randomness = 0.3f;           // 随机性强度

    [Header("旋转轴设置")]
    [SerializeField] private bool enableX = true;               // 是否在X轴摇摆
    [SerializeField] private bool enableZ = true;               // 是否在Z轴摇摆
    [SerializeField] private bool enableY = false;              // 是否在Y轴摇摆

    [Header("高级设置")]
    [SerializeField] private float dampening = 0.95f;           // 阻尼系数
    [SerializeField] private float windInfluence = 1.0f;        // 风力影响

    private Vector3 originalRotation;                           // 原始旋转
    private Vector3 currentVelocity;                            // 当前速度
    private float timeOffset;                                   // 时间偏移（用于随机性）

    void Start()
    {
        // 记录初始旋转
        originalRotation = transform.localEulerAngles;

        // 为每个灯泡生成不同的时间偏移，避免同步摇摆
        timeOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        // 计算基于时间的摇摆
        float time = Time.time + timeOffset;

        // 生成柏林噪声用于更自然的随机摇摆
        float noiseX = Mathf.PerlinNoise(time * swaySpeed, 0f) - 0.5f;
        float noiseZ = Mathf.PerlinNoise(0f, time * swaySpeed) - 0.5f;
        float noiseY = Mathf.PerlinNoise(time * swaySpeed * 0.7f, time * swaySpeed * 0.7f) - 0.5f;

        // 添加随机突发性摇摆（模拟风吹）
        if (Random.Range(0f, 1f) < randomness * Time.deltaTime)
        {
            currentVelocity += new Vector3(
                Random.Range(-1f, 1f) * windInfluence,
                Random.Range(-1f, 1f) * windInfluence * 0.3f,
                Random.Range(-1f, 1f) * windInfluence
            );
        }

        // 计算目标旋转
        Vector3 targetRotation = new Vector3(
            enableX ? noiseX * swayIntensity : 0f,
            enableY ? noiseY * swayIntensity * 0.5f : 0f,
            enableZ ? noiseZ * swayIntensity : 0f
        );

        // 添加速度影响
        targetRotation += currentVelocity;

        // 应用阻尼
        currentVelocity *= dampening;

        // 平滑插值到目标旋转
        Vector3 newRotation = originalRotation + targetRotation;
        transform.localRotation = Quaternion.Lerp(
            transform.localRotation,
            Quaternion.Euler(newRotation),
            Time.deltaTime * 2f
        );
    }

    // 可选：添加外部风力影响的方法
    public void AddWindForce(Vector3 windDirection, float strength)
    {
        currentVelocity += windDirection * strength;
    }

    // 可选：重置到原始位置
    public void ResetToOriginal()
    {
        transform.localRotation = Quaternion.Euler(originalRotation);
        currentVelocity = Vector3.zero;
    }

    // 在编辑器中显示摇摆范围
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, swayIntensity * 0.1f);
    }
}