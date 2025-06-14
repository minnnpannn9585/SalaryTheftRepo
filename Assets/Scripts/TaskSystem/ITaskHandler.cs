using UnityEngine;

/// <summary>
/// ���������ӿ�
/// ����������������Ҫʵ�ִ˽ӿ�
/// </summary>
public interface ITaskHandler
{
    /// <summary>
    /// ��ʼ����������
    /// </summary>
    /// <param name="taskManager">�������������</param>
    void Initialize(TaskManager taskManager);

    /// <summary>
    /// ��ʼִ������
    /// </summary>
    /// <param name="taskData">��������</param>
    /// <param name="taskIndex">��������</param>
    void StartTask(TaskData taskData, int taskIndex);

    /// <summary>
    /// ����������ض���
    /// </summary>
    void CleanupTasks();
}