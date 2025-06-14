using UnityEngine;

public class CharacterStatus : MonoBehaviour
{
    [Header("状态设置")]
    public float stressLevel;
    public bool isSlackingAtWork;

    [Header("惩罚设置")]
    public int penaltyAmount = 50; // 每次扣工资的金额
    public float penaltyCooldown = 3f; // 惩罚冷却时间（秒）

    [Header("调试设置")]
    public bool enablePenaltyDebug = true; // 启用惩罚调试信息

    // 私有变量
    private float lastPenaltyTime = -999f; // 上次惩罚的时间
    private GameLogicSystem gameLogicSystem; // 游戏逻辑系统引用

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

            if (enablePenaltyDebug)
            {
                Debug.Log($"[CharacterStatus] 🚨 惩罚生效！扣除工资: ${penaltyAmount}");
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