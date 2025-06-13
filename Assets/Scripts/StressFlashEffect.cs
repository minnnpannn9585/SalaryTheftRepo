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

    // 私有变量
    private bool isFlashing = false;
    private Coroutine flashCoroutine;
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
    }

    void OnDestroy()
    {
        // 取消订阅事件
        GameLogicSystem.OnStressChanged -= OnStressChanged;
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

        // 淡出效果
        if (flashImage != null)
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
}