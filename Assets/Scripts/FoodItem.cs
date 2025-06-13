using UnityEngine;

[System.Serializable]
public class FoodEffect
{
    [Header("移动速度加成")]
    public bool hasSpeedBoost = false;
    [Range(0.1f, 5f)]
    public float speedMultiplier = 1.5f; // 速度倍数
    [Range(1f, 60f)]
    public float speedBoostDuration = 10f; // 持续时间（秒）

    [Header("压力减少")]
    public bool hasStressReduction = false;
    [Range(1f, 50f)]
    public float stressReduction = 10f; // 减少的压力值
}

public class FoodItem : MonoBehaviour
{
    [Header("食物属性")]
    public string foodName = "食物";
    public FoodEffect foodEffect;

    [Header("消费效果")]
    public GameObject consumeEffect; // 消费时的特效（可选）
    public AudioClip consumeSound; // 消费时的音效（可选）

    /// <summary>
    /// 被消费时调用
    /// </summary>
    public void OnConsume()
    {
        // 播放特效
        if (consumeEffect != null)
        {
            Instantiate(consumeEffect, transform.position, transform.rotation);
        }

        // 播放音效
        if (consumeSound != null)
        {
            AudioSource.PlayClipAtPoint(consumeSound, transform.position);
        }

        // 删除物体
        Destroy(gameObject);
    }

    /// <summary>
    /// 获取食物效果
    /// </summary>
    public FoodEffect GetFoodEffect()
    {
        return foodEffect;
    }
}
