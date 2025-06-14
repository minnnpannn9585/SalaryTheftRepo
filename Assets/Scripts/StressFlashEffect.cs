using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StressFlashEffect : MonoBehaviour
{
    [Header("闪烁设置")]
    public Image flashImage; // 拖拽一个全屏的红色Image到这里

    [Header("压力阈值设置")]
    public float stressThreshold = 70f; // 开始闪烁的压力阈值

    [Header("闪烁强度设置")]
    public float minFlashSpeed = 1f; // 最小闪烁速度（压力70%时）
    public float maxFlashSpeed = 4f; // 最大闪烁速度（压力100%时）
    public float minAlpha = 0.1f; // 最小透明度（压力70%时）
    public float maxAlpha = 0.5f; // 最大透明度（压力100%时）

    [Header("惩罚闪烁设置")]
    public float penaltyFlashAlpha = 0.8f; // 惩罚闪烁的透明度
    public float penaltyFlashDuration = 0.15f; // 每次惩罚闪烁持续时间
    public int penaltyFlashCount = 2; // 惩罚闪烁次数

    // 私有变量
    private bool isFlashing = false;
    private Coroutine flashCoroutine;
    private Coroutine penaltyFlashCoroutine;
    private GameLogicSystem gameLogicSystem;
    private float currentFlashSpeed;
    private float currentMaxAlpha;

    void Start()
    {
        // 找到GameLogicSystem组件
        gameLogicSystem = FindObjectOfType<GameLogicSystem>();

        // 确保Image初始状态是透明的
        if (flashImage != null)
        {
            Color color = flashImage.color;
            color.a = 0f;
            flashImage.color = color;
        }

        // 订阅压力变化事件
        GameLogicSystem.OnStressChanged += OnStressChanged;
        // 订阅压力惩罚事件
        GameLogicSystem.OnStressPenalty += OnStressPenalty;
    }

    void OnDestroy()
    {
        // 取消订阅事件
        GameLogicSystem.OnStressChanged -= OnStressChanged;
        GameLogicSystem.OnStressPenalty -= OnStressPenalty;
    }

    /// <summary>
    /// 当压力值改变时调用
    /// </summary>
    /// <param name="newStressLevel">新的压力值</param>
    private void OnStressChanged(float newStressLevel)
    {
        if (newStressLevel >= stressThreshold && !isFlashing)
        {
            StartFlashing(newStressLevel);
        }
        else if (newStressLevel < stressThreshold && isFlashing)
        {
            StopFlashing();
        }
        else if (isFlashing)
        {
            // 更新闪烁强度
            UpdateFlashIntensity(newStressLevel);
        }
    }

    /// <summary>
    /// 当压力因惩罚增加时调用
    /// </summary>
    /// <param name="penaltyAmount">惩罚增加的压力值</param>
    private void OnStressPenalty(float penaltyAmount)
    {
        // 触发惩罚闪烁效果
        if (penaltyFlashCoroutine != null)
        {
            StopCoroutine(penaltyFlashCoroutine);
        }
        penaltyFlashCoroutine = StartCoroutine(PenaltyFlashCoroutine());
    }

    /// <summary>
    /// 惩罚闪烁协程
    /// </summary>
    private IEnumerator PenaltyFlashCoroutine()
    {
        // 保存当前透明度
        float originalAlpha = flashImage != null ? flashImage.color.a : 0f;

        for (int i = 0; i < penaltyFlashCount; i++)
        {
            // 快速闪烁到高透明度
            yield return StartCoroutine(FastFadeToAlpha(penaltyFlashAlpha, penaltyFlashDuration * 0.3f));

            // 快速淡出
            yield return StartCoroutine(FastFadeToAlpha(0f, penaltyFlashDuration * 0.7f));

            // 如果不是最后一次闪烁，稍微等待一下
            if (i < penaltyFlashCount - 1)
            {
                yield return new WaitForSeconds(penaltyFlashDuration * 0.2f);
            }
        }

        // 恢复到原始透明度（如果正在正常闪烁的话）
        if (isFlashing && flashImage != null)
        {
            Color color = flashImage.color;
            color.a = originalAlpha;
            flashImage.color = color;
        }

        penaltyFlashCoroutine = null;
    }

    /// <summary>
    /// 快速淡化到指定透明度（用于惩罚闪烁）
    /// </summary>
    /// <param name="targetAlpha">目标透明度</param>
    /// <param name="duration">持续时间</param>
    private IEnumerator FastFadeToAlpha(float targetAlpha, float duration)
    {
        if (flashImage == null) yield break;

        Color color = flashImage.color;
        float startAlpha = color.a;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            color.a = alpha;
            flashImage.color = color;
            yield return null;
        }

        color.a = targetAlpha;
        flashImage.color = color;
    }

    /// <summary>
    /// 开始闪烁效果
    /// </summary>
    /// <param name="stressLevel">当前压力值</param>
    public void StartFlashing(float stressLevel)
    {
        if (flashImage == null) return;

        isFlashing = true;
        UpdateFlashIntensity(stressLevel);

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashCoroutine());
    }

    /// <summary>
    /// 停止闪烁效果
    /// </summary>
    public void StopFlashing()
    {
        isFlashing = false;
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        // 淡出效果（但不影响正在进行的惩罚闪烁）
        if (flashImage != null && penaltyFlashCoroutine == null)
            StartCoroutine(FadeOut());
    }

    /// <summary>
    /// 根据压力值更新闪烁强度
    /// </summary>
    /// <param name="stressLevel">当前压力值</param>
    private void UpdateFlashIntensity(float stressLevel)
    {
        // 计算压力从70%到100%的进度 (0-1)
        float stressProgress = Mathf.Clamp01((stressLevel - stressThreshold) / (100f - stressThreshold));

        // 根据压力进度插值计算闪烁参数
        currentFlashSpeed = Mathf.Lerp(minFlashSpeed, maxFlashSpeed, stressProgress);
        currentMaxAlpha = Mathf.Lerp(minAlpha, maxAlpha, stressProgress);
    }

    /// <summary>
    /// 闪烁协程
    /// </summary>
    private IEnumerator FlashCoroutine()
    {
        while (isFlashing)
        {
            // 淡入
            yield return StartCoroutine(FadeToAlpha(currentMaxAlpha));
            // 淡出
            yield return StartCoroutine(FadeToAlpha(0f));
        }
    }

    /// <summary>
    /// 淡化到指定透明度
    /// </summary>
    /// <param name="targetAlpha">目标透明度</param>
    private IEnumerator FadeToAlpha(float targetAlpha)
    {
        if (flashImage == null) yield break;

        Color color = flashImage.color;
        float startAlpha = color.a;
        float time = 0f;
        float duration = 1f / currentFlashSpeed;

        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            color.a = alpha;
            flashImage.color = color;
            yield return null;
        }

        color.a = targetAlpha;
        flashImage.color = color;
    }

    /// <summary>
    /// 淡出效果
    /// </summary>
    private IEnumerator FadeOut()
    {
        yield return StartCoroutine(FadeToAlpha(0f));
    }

    /// <summary>
    /// 手动触发惩罚闪烁（用于测试或外部调用）
    /// </summary>
    public void TriggerPenaltyFlash()
    {
        OnStressPenalty(0f);
    }
}