using UnityEngine;

public class CharacterStatus : MonoBehaviour
{
    [Header("状态设置")]
    public bool isSlackingAtWork;

    [Header("惩罚设置")]
    public int penaltyAmount = 50; // 每次扣工资的金额
    public float penaltyCooldown = 3f; // 惩罚冷却时间（秒）
    public float stressPenalty = 20f; // 每次惩罚增加的压力值

    [Header("音效设置")]
    [SerializeField] private AudioSource penaltyAudioSource; // 惩罚音效播放器
    [SerializeField] private AudioClip penaltySound; // 惩罚音效文件
    [SerializeField, Range(0f, 1f)] private float penaltyVolume = 1f; // 惩罚音效音量

    [Header("调试设置")]
    public bool enablePenaltyDebug = true; // 启用惩罚调试信息

    // 私有变量
    private float lastPenaltyTime = -999f; // 上次惩罚的时间
    private GameLogicSystem gameLogicSystem; // 游戏逻辑系统引用

    /// <summary>
    /// 手动测试惩罚音效（用于调试）
    /// </summary>
    [ContextMenu("测试惩罚音效")]
    public void TestPenaltySound()
    {
        PlayPenaltySound();
    }

    // 属性访问器：获取当前压力值（从GameLogicSystem）
    public float stressLevel => gameLogicSystem != null ? gameLogicSystem.StressLevel : 0f;

    void Start()
    {
        // 获取GameLogicSystem组件
        gameLogicSystem = FindObjectOfType<GameLogicSystem>();
        if (gameLogicSystem == null)
        {
            Debug.LogWarning("[CharacterStatus] 未找到GameLogicSystem组件！");
        }
    }

    /// <summary>
    /// 对玩家进行惩罚（扣工资）
    /// </summary>
    /// <returns>是否成功扣除工资</returns>
    public bool ApplyPenalty()
    {
        // 检查冷却时间
        if (Time.time - lastPenaltyTime < penaltyCooldown)
        {
            if (enablePenaltyDebug)
            {
                float remainingCooldown = penaltyCooldown - (Time.time - lastPenaltyTime);
                Debug.Log($"[CharacterStatus] 惩罚在冷却中，剩余时间: {remainingCooldown:F1}秒");
            }
            return false;
        }

        // 检查GameLogicSystem是否存在
        if (gameLogicSystem == null)
        {
            Debug.LogError("[CharacterStatus] GameLogicSystem未找到，无法执行惩罚！");
            return false;
        }

        // 执行扣工资
        bool success = gameLogicSystem.DeductSalary(penaltyAmount);

        if (success)
        {
            // 更新上次惩罚时间
            lastPenaltyTime = Time.time;

            // 增加压力值（标记为外部调用）
            gameLogicSystem.AddStress(stressPenalty, true);

            // 播放惩罚音效
            PlayPenaltySound();

            if (enablePenaltyDebug)
            {
                Debug.Log($"[CharacterStatus] 🚨 惩罚生效！扣除工资: ${penaltyAmount}, 增加压力: {stressPenalty}");
            }
        }
        else
        {
            if (enablePenaltyDebug)
            {
                Debug.Log("[CharacterStatus] ❌ 惩罚失败（可能工资不足）");
            }
        }

        return success;
    }

    /// <summary>
    /// 检查是否可以进行惩罚
    /// </summary>
    /// <returns>是否可以惩罚</returns>
    public bool CanApplyPenalty()
    {
        return Time.time - lastPenaltyTime >= penaltyCooldown;
    }

    /// <summary>
    /// 获取剩余冷却时间
    /// </summary>
    /// <returns>剩余冷却时间（秒）</returns>
    public float GetPenaltyRemainingCooldown()
    {
        float remainingTime = penaltyCooldown - (Time.time - lastPenaltyTime);
        return Mathf.Max(0f, remainingTime);
    }

    /// <summary>
    /// 播放惩罚音效
    /// </summary>
    private void PlayPenaltySound()
    {
        // 方式1：使用AudioSource组件播放
        if (penaltyAudioSource != null && penaltySound != null)
        {
            penaltyAudioSource.clip = penaltySound;
            penaltyAudioSource.volume = penaltyVolume;
            penaltyAudioSource.Play();

            if (enablePenaltyDebug)
            {
                Debug.Log("[CharacterStatus] 🔊 播放惩罚音效");
            }
        }
        // 方式2：使用AudioSource直接播放（如果已经设置了clip）
        else if (penaltyAudioSource != null)
        {
            penaltyAudioSource.volume = penaltyVolume;
            penaltyAudioSource.Play();

            if (enablePenaltyDebug)
            {
                Debug.Log("[CharacterStatus] 🔊 播放惩罚音效（使用预设clip）");
            }
        }
        // 方式3：使用AudioSource.PlayClipAtPoint（3D音效）
        else if (penaltySound != null)
        {
            AudioSource.PlayClipAtPoint(penaltySound, transform.position, penaltyVolume);

            if (enablePenaltyDebug)
            {
                Debug.Log("[CharacterStatus] 🔊 播放惩罚音效（3D位置音效）");
            }
        }
        else if (enablePenaltyDebug)
        {
            Debug.LogWarning("[CharacterStatus] ⚠️ 无法播放惩罚音效：未设置AudioSource或AudioClip");
        }
    }

    /// <summary>
    /// 重置惩罚冷却时间（用于调试或特殊情况）
    /// </summary>
    public void ResetPenaltyCooldown()
    {
        lastPenaltyTime = -999f;
        if (enablePenaltyDebug)
        {
            Debug.Log("[CharacterStatus] 惩罚冷却时间已重置");
        }
    }

    // 属性访问器
    public bool IsPenaltyOnCooldown => !CanApplyPenalty();
    public float PenaltyRemainingCooldown => GetPenaltyRemainingCooldown();
}