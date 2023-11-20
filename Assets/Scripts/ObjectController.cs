using UnityEngine;

public class ObjectController : MonoBehaviour
{
    public float rotationSpeed = 30f; // �Զ���ת�ٶ�
    public float movementSpeed = 2f;  // �Զ��ƶ��ٶ�

    void Update()
    {
        // ����1������ʱ���Զ���ת����
        AutoRotate();

        // ����2������ʱ���Զ������ƶ�����
        AutoMove();
    }

    void AutoRotate()
    {
        // �Զ���ת
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    void AutoMove()
    {
        // ���������ƶ�
        float horizontalMovement = Mathf.Sin(Time.time) * movementSpeed;

        // Ӧ�������ƶ�
        transform.Translate(new Vector3(horizontalMovement, 0f, 0f) * Time.deltaTime);
    }
}
