using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectDelayActivator : MonoBehaviour
{
    [Header("�ӳ�����")]
    [SerializeField] private float minDelay = 0.5f;
    [SerializeField] private float maxDelay = 2.0f;

    [Header("�����б�")]
    [SerializeField] private List<GameObject> objectsToActivate = new List<GameObject>();

    [Header("״̬")]
    [SerializeField] private bool isActivationComplete = false;

    // �ⲿ��������
    public bool IsActivationComplete => isActivationComplete;

    // ˽�б���
    private Coroutine activationCoroutine;
    private int currentIndex = 0;

    void Start()
    {
        // ȷ�����ж����ʼʱ���ǷǼ���״̬
        DeactivateAllObjects();

        // ֱ�ӿ�ʼ��������
        StartActivation();
    }

    /// <summary>
    /// ��ʼ�ӳټ�������
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
    /// ֹͣ��������
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
    /// ���ü���״̬
    /// </summary>
    public void ResetActivation()
    {
        currentIndex = 0;
        isActivationComplete = false;
    }

    /// <summary>
    /// �����ж�����Ϊ�Ǽ���״̬
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
    /// �ӳټ�������Э��
    /// </summary>
    private IEnumerator ActivateObjectsWithDelay()
    {
        for (int i = 0; i < objectsToActivate.Count; i++)
        {
            if (objectsToActivate[i] != null)
            {
                // ��������ӳ�ʱ��
                float randomDelay = Random.Range(minDelay, maxDelay);

                // �ȴ��ӳ�ʱ��
                yield return new WaitForSeconds(randomDelay);

                // ���ǰ����
                objectsToActivate[i].SetActive(true);
                currentIndex = i + 1;

                Debug.Log($"�������: {objectsToActivate[i].name}, �ӳ�: {randomDelay:F2}��");
            }
        }

        // ���ж��󼤻����
        isActivationComplete = true;
        Debug.Log("���ж��󼤻���ϣ�");
    }

    /// <summary>
    /// ��ȡ��ǰ������ȣ�0-1��
    /// </summary>
    public float GetActivationProgress()
    {
        if (objectsToActivate.Count == 0) return 1f;
        return (float)currentIndex / objectsToActivate.Count;
    }

    /// <summary>
    /// ��ȡ�Ѽ���Ķ�������
    /// </summary>
    public int GetActivatedCount()
    {
        return currentIndex;
    }

    /// <summary>
    /// ��ȡ�ܶ�������
    /// </summary>
    public int GetTotalCount()
    {
        return objectsToActivate.Count;
    }

    // Inspector�еİ�ť��������Ҫ��Inspector���ֶ����Button������󶨣�
    [ContextMenu("��ʼ����")]
    public void StartActivationFromMenu()
    {
        StartActivation();
    }

    [ContextMenu("ֹͣ����")]
    public void StopActivationFromMenu()
    {
        StopActivation();
    }

    [ContextMenu("����״̬")]
    public void ResetActivationFromMenu()
    {
        StopActivation();
        DeactivateAllObjects();
    }

    // ��֤�ӳ�ֵ�ĺ�����
    void OnValidate()
    {
        if (minDelay < 0) minDelay = 0;
        if (maxDelay < minDelay) maxDelay = minDelay;
    }
}