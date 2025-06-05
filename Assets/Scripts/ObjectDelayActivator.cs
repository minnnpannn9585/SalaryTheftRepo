using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectDelayActivator : MonoBehaviour
{
    [Header("延迟设置")]
    [SerializeField] private float minDelay = 0.5f;
    [SerializeField] private float maxDelay = 2.0f;

    [Header("对象列表")]
    [SerializeField] private List<GameObject> objectsToActivate = new List<GameObject>();

    [Header("状态")]
    [SerializeField] private bool isActivationComplete = false;

    // 外部访问属性
    public bool IsActivationComplete => isActivationComplete;

    // 私有变量
    private Coroutine activationCoroutine;
    private int currentIndex = 0;

    void Start()
    {
        // 确保所有对象初始时都是非激活状态
        DeactivateAllObjects();

        // 直接开始激活序列
        StartActivation();
    }

    /// <summary>
    /// 开始延迟激活序列
    /// </summary>
    public void StartActivation()
    {
        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
        }

        ResetActivation();
        activationCoroutine = StartCoroutine(ActivateObjectsWithDelay());
    }

    /// <summary>
    /// 停止激活序列
    /// </summary>
    public void StopActivation()
    {
        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
            activationCoroutine = null;
        }
    }

    /// <summary>
    /// 重置激活状态
    /// </summary>
    public void ResetActivation()
    {
        currentIndex = 0;
        isActivationComplete = false;
    }

    /// <summary>
    /// 将所有对象设为非激活状态
    /// </summary>
    public void DeactivateAllObjects()
    {
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
        ResetActivation();
    }

    /// <summary>
    /// 延迟激活对象的协程
    /// </summary>
    private IEnumerator ActivateObjectsWithDelay()
    {
        for (int i = 0; i < objectsToActivate.Count; i++)
        {
            if (objectsToActivate[i] != null)
            {
                // 生成随机延迟时间
                float randomDelay = Random.Range(minDelay, maxDelay);

                // 等待延迟时间
                yield return new WaitForSeconds(randomDelay);

                // 激活当前对象
                objectsToActivate[i].SetActive(true);
                currentIndex = i + 1;

                Debug.Log($"激活对象: {objectsToActivate[i].name}, 延迟: {randomDelay:F2}秒");
            }
        }

        // 所有对象激活完毕
        isActivationComplete = true;
        Debug.Log("所有对象激活完毕！");
    }

    /// <summary>
    /// 获取当前激活进度（0-1）
    /// </summary>
    public float GetActivationProgress()
    {
        if (objectsToActivate.Count == 0) return 1f;
        return (float)currentIndex / objectsToActivate.Count;
    }

    /// <summary>
    /// 获取已激活的对象数量
    /// </summary>
    public int GetActivatedCount()
    {
        return currentIndex;
    }

    /// <summary>
    /// 获取总对象数量
    /// </summary>
    public int GetTotalCount()
    {
        return objectsToActivate.Count;
    }

    // Inspector中的按钮方法（需要在Inspector中手动添加Button组件并绑定）
    [ContextMenu("开始激活")]
    public void StartActivationFromMenu()
    {
        StartActivation();
    }

    [ContextMenu("停止激活")]
    public void StopActivationFromMenu()
    {
        StopActivation();
    }

    [ContextMenu("重置状态")]
    public void ResetActivationFromMenu()
    {
        StopActivation();
        DeactivateAllObjects();
    }

    // 验证延迟值的合理性
    void OnValidate()
    {
        if (minDelay < 0) minDelay = 0;
        if (maxDelay < minDelay) maxDelay = minDelay;
    }
}