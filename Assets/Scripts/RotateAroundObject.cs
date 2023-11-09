using UnityEngine;

public class RotateWithReverse : MonoBehaviour
{
    public Transform targetObject; // ��������Ҫ������ת��Ŀ������
    public float rotationSpeed = 30.0f; // ��ת�ٶ�
    public float maxRotationAngle = 90.0f; // �����ת�Ƕȣ���������ǶȺ�������ת

    private float currentAngle = 0.0f;
    private bool isReversing = false;

    public bool limit = false;

    private void Update()
    {
        // ȷ���ѷ���Ŀ������
        if (targetObject != null)
        {
            // ������ת�Ƕȣ�ÿ�룩
            float rotationAngle = rotationSpeed * Time.deltaTime;

            // ���δ�ﵽ�����ת�Ƕȣ�����������ת
            if (!isReversing)
            {
                currentAngle += rotationAngle;
                if (currentAngle >= maxRotationAngle)
                {
                    currentAngle = maxRotationAngle;
                    isReversing = true;
                }
            }
            // ����Ѿ��ﵽ�����ת�Ƕȣ���ʼ������ת
            else
            {
                currentAngle -= rotationAngle;
                if (currentAngle <= -maxRotationAngle)
                {
                    currentAngle = -maxRotationAngle;
                    isReversing = false;
                }
            }

            if (!limit)
                isReversing = false;

            // ʹ��Transform.RotateAround������Ŀ��������ת
            transform.RotateAround(targetObject.position, Vector3.up, isReversing ? -rotationAngle : rotationAngle);
        }
        else
        {
            Debug.LogWarning("�����һ��Ŀ�����壡");
        }
    }
}
