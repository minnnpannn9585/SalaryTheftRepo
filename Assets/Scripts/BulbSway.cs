using UnityEngine;

public class BulbSway : MonoBehaviour
{
    [Header("ҡ������")]
    [SerializeField] private float swayIntensity = 0.5f;        // ҡ��ǿ��
    [SerializeField] private float swaySpeed = 1.0f;            // ҡ���ٶ�
    [SerializeField] private float randomness = 0.3f;           // �����ǿ��

    [Header("��ת������")]
    [SerializeField] private bool enableX = true;               // �Ƿ���X��ҡ��
    [SerializeField] private bool enableZ = true;               // �Ƿ���Z��ҡ��
    [SerializeField] private bool enableY = false;              // �Ƿ���Y��ҡ��

    [Header("�߼�����")]
    [SerializeField] private float dampening = 0.95f;           // ����ϵ��
    [SerializeField] private float windInfluence = 1.0f;        // ����Ӱ��

    private Vector3 originalRotation;                           // ԭʼ��ת
    private Vector3 currentVelocity;                            // ��ǰ�ٶ�
    private float timeOffset;                                   // ʱ��ƫ�ƣ���������ԣ�

    void Start()
    {
        // ��¼��ʼ��ת
        originalRotation = transform.localEulerAngles;

        // Ϊÿ���������ɲ�ͬ��ʱ��ƫ�ƣ�����ͬ��ҡ��
        timeOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        // �������ʱ���ҡ��
        float time = Time.time + timeOffset;

        // ���ɰ����������ڸ���Ȼ�����ҡ��
        float noiseX = Mathf.PerlinNoise(time * swaySpeed, 0f) - 0.5f;
        float noiseZ = Mathf.PerlinNoise(0f, time * swaySpeed) - 0.5f;
        float noiseY = Mathf.PerlinNoise(time * swaySpeed * 0.7f, time * swaySpeed * 0.7f) - 0.5f;

        // ������ͻ����ҡ�ڣ�ģ��紵��
        if (Random.Range(0f, 1f) < randomness * Time.deltaTime)
        {
            currentVelocity += new Vector3(
                Random.Range(-1f, 1f) * windInfluence,
                Random.Range(-1f, 1f) * windInfluence * 0.3f,
                Random.Range(-1f, 1f) * windInfluence
            );
        }

        // ����Ŀ����ת
        Vector3 targetRotation = new Vector3(
            enableX ? noiseX * swayIntensity : 0f,
            enableY ? noiseY * swayIntensity * 0.5f : 0f,
            enableZ ? noiseZ * swayIntensity : 0f
        );

        // ����ٶ�Ӱ��
        targetRotation += currentVelocity;

        // Ӧ������
        currentVelocity *= dampening;

        // ƽ����ֵ��Ŀ����ת
        Vector3 newRotation = originalRotation + targetRotation;
        transform.localRotation = Quaternion.Lerp(
            transform.localRotation,
            Quaternion.Euler(newRotation),
            Time.deltaTime * 2f
        );
    }

    // ��ѡ������ⲿ����Ӱ��ķ���
    public void AddWindForce(Vector3 windDirection, float strength)
    {
        currentVelocity += windDirection * strength;
    }

    // ��ѡ�����õ�ԭʼλ��
    public void ResetToOriginal()
    {
        transform.localRotation = Quaternion.Euler(originalRotation);
        currentVelocity = Vector3.zero;
    }

    // �ڱ༭������ʾҡ�ڷ�Χ
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, swayIntensity * 0.1f);
    }
}