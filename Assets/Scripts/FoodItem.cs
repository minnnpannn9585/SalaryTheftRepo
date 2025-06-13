using UnityEngine;

[System.Serializable]
public class FoodEffect
{
    [Header("�ƶ��ٶȼӳ�")]
    public bool hasSpeedBoost = false;
    [Range(0.1f, 5f)]
    public float speedMultiplier = 1.5f; // �ٶȱ���
    [Range(1f, 60f)]
    public float speedBoostDuration = 10f; // ����ʱ�䣨�룩

    [Header("ѹ������")]
    public bool hasStressReduction = false;
    [Range(1f, 50f)]
    public float stressReduction = 10f; // ���ٵ�ѹ��ֵ
}

public class FoodItem : MonoBehaviour
{
    [Header("ʳ������")]
    public string foodName = "ʳ��";
    public FoodEffect foodEffect;

    [Header("����Ч��")]
    public GameObject consumeEffect; // ����ʱ����Ч����ѡ��
    public AudioClip consumeSound; // ����ʱ����Ч����ѡ��

    /// <summary>
    /// ������ʱ����
    /// </summary>
    public void OnConsume()
    {
        // ������Ч
        if (consumeEffect != null)
        {
            Instantiate(consumeEffect, transform.position, transform.rotation);
        }

        // ������Ч
        if (consumeSound != null)
        {
            AudioSource.PlayClipAtPoint(consumeSound, transform.position);
        }

        // ɾ������
        Destroy(gameObject);
    }

    /// <summary>
    /// ��ȡʳ��Ч��
    /// </summary>
    public FoodEffect GetFoodEffect()
    {
        return foodEffect;
    }
}
