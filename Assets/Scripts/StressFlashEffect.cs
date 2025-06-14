using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StressFlashEffect : MonoBehaviour
{
    [Header("��˸����")]
    public Image flashImage; // ��קһ��ȫ���ĺ�ɫImage������

    [Header("ѹ����ֵ����")]
    public float stressThreshold = 70f; // ��ʼ��˸��ѹ����ֵ

    [Header("��˸ǿ������")]
    public float minFlashSpeed = 1f; // ��С��˸�ٶȣ�ѹ��70%ʱ��
    public float maxFlashSpeed = 4f; // �����˸�ٶȣ�ѹ��100%ʱ��
    public float minAlpha = 0.1f; // ��С͸���ȣ�ѹ��70%ʱ��
    public float maxAlpha = 0.5f; // ���͸���ȣ�ѹ��100%ʱ��

    [Header("�ͷ���˸����")]
    public float penaltyFlashAlpha = 0.8f; // �ͷ���˸��͸����
    public float penaltyFlashDuration = 0.15f; // ÿ�γͷ���˸����ʱ��
    public int penaltyFlashCount = 2; // �ͷ���˸����

    // ˽�б���
    private bool isFlashing = false;
    private Coroutine flashCoroutine;
    private Coroutine penaltyFlashCoroutine;
    private GameLogicSystem gameLogicSystem;
    private float currentFlashSpeed;
    private float currentMaxAlpha;

    void Start()
    {
        // �ҵ�GameLogicSystem���
        gameLogicSystem = FindObjectOfType<GameLogicSystem>();

        // ȷ��Image��ʼ״̬��͸����
        if (flashImage != null)
        {
            Color color = flashImage.color;
            color.a = 0f;
            flashImage.color = color;
        }

        // ����ѹ���仯�¼�
        GameLogicSystem.OnStressChanged += OnStressChanged;
        // ����ѹ���ͷ��¼�
        GameLogicSystem.OnStressPenalty += OnStressPenalty;
    }

    void OnDestroy()
    {
        // ȡ�������¼�
        GameLogicSystem.OnStressChanged -= OnStressChanged;
        GameLogicSystem.OnStressPenalty -= OnStressPenalty;
    }

    /// <summary>
    /// ��ѹ��ֵ�ı�ʱ����
    /// </summary>
    /// <param name="newStressLevel">�µ�ѹ��ֵ</param>
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
            // ������˸ǿ��
            UpdateFlashIntensity(newStressLevel);
        }
    }

    /// <summary>
    /// ��ѹ����ͷ�����ʱ����
    /// </summary>
    /// <param name="penaltyAmount">�ͷ����ӵ�ѹ��ֵ</param>
    private void OnStressPenalty(float penaltyAmount)
    {
        // �����ͷ���˸Ч��
        if (penaltyFlashCoroutine != null)
        {
            StopCoroutine(penaltyFlashCoroutine);
        }
        penaltyFlashCoroutine = StartCoroutine(PenaltyFlashCoroutine());
    }

    /// <summary>
    /// �ͷ���˸Э��
    /// </summary>
    private IEnumerator PenaltyFlashCoroutine()
    {
        // ���浱ǰ͸����
        float originalAlpha = flashImage != null ? flashImage.color.a : 0f;

        for (int i = 0; i < penaltyFlashCount; i++)
        {
            // ������˸����͸����
            yield return StartCoroutine(FastFadeToAlpha(penaltyFlashAlpha, penaltyFlashDuration * 0.3f));

            // ���ٵ���
            yield return StartCoroutine(FastFadeToAlpha(0f, penaltyFlashDuration * 0.7f));

            // ����������һ����˸����΢�ȴ�һ��
            if (i < penaltyFlashCount - 1)
            {
                yield return new WaitForSeconds(penaltyFlashDuration * 0.2f);
            }
        }

        // �ָ���ԭʼ͸���ȣ��������������˸�Ļ���
        if (isFlashing && flashImage != null)
        {
            Color color = flashImage.color;
            color.a = originalAlpha;
            flashImage.color = color;
        }

        penaltyFlashCoroutine = null;
    }

    /// <summary>
    /// ���ٵ�����ָ��͸���ȣ����ڳͷ���˸��
    /// </summary>
    /// <param name="targetAlpha">Ŀ��͸����</param>
    /// <param name="duration">����ʱ��</param>
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
    /// ��ʼ��˸Ч��
    /// </summary>
    /// <param name="stressLevel">��ǰѹ��ֵ</param>
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
    /// ֹͣ��˸Ч��
    /// </summary>
    public void StopFlashing()
    {
        isFlashing = false;
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        // ����Ч��������Ӱ�����ڽ��еĳͷ���˸��
        if (flashImage != null && penaltyFlashCoroutine == null)
            StartCoroutine(FadeOut());
    }

    /// <summary>
    /// ����ѹ��ֵ������˸ǿ��
    /// </summary>
    /// <param name="stressLevel">��ǰѹ��ֵ</param>
    private void UpdateFlashIntensity(float stressLevel)
    {
        // ����ѹ����70%��100%�Ľ��� (0-1)
        float stressProgress = Mathf.Clamp01((stressLevel - stressThreshold) / (100f - stressThreshold));

        // ����ѹ�����Ȳ�ֵ������˸����
        currentFlashSpeed = Mathf.Lerp(minFlashSpeed, maxFlashSpeed, stressProgress);
        currentMaxAlpha = Mathf.Lerp(minAlpha, maxAlpha, stressProgress);
    }

    /// <summary>
    /// ��˸Э��
    /// </summary>
    private IEnumerator FlashCoroutine()
    {
        while (isFlashing)
        {
            // ����
            yield return StartCoroutine(FadeToAlpha(currentMaxAlpha));
            // ����
            yield return StartCoroutine(FadeToAlpha(0f));
        }
    }

    /// <summary>
    /// ������ָ��͸����
    /// </summary>
    /// <param name="targetAlpha">Ŀ��͸����</param>
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
    /// ����Ч��
    /// </summary>
    private IEnumerator FadeOut()
    {
        yield return StartCoroutine(FadeToAlpha(0f));
    }

    /// <summary>
    /// �ֶ������ͷ���˸�����ڲ��Ի��ⲿ���ã�
    /// </summary>
    public void TriggerPenaltyFlash()
    {
        OnStressPenalty(0f);
    }
}