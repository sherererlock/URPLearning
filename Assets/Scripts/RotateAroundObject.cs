using UnityEngine;

public class RotateWithReverse : MonoBehaviour
{
    public Transform targetObject; // 这是你想要绕其旋转的目标物体
    public float rotationSpeed = 30.0f; // 旋转速度
    public float maxRotationAngle = 90.0f; // 最大旋转角度，超过这个角度后逆向旋转

    private float currentAngle = 0.0f;
    private bool isReversing = false;

    public bool limit = false;

    private void Update()
    {
        // 确保已分配目标物体
        if (targetObject != null)
        {
            // 计算旋转角度（每秒）
            float rotationAngle = rotationSpeed * Time.deltaTime;

            // 如果未达到最大旋转角度，继续正向旋转
            if (!isReversing)
            {
                currentAngle += rotationAngle;
                if (currentAngle >= maxRotationAngle)
                {
                    currentAngle = maxRotationAngle;
                    isReversing = true;
                }
            }
            // 如果已经达到最大旋转角度，开始逆向旋转
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

            // 使用Transform.RotateAround方法绕目标物体旋转
            transform.RotateAround(targetObject.position, Vector3.up, isReversing ? -rotationAngle : rotationAngle);
        }
        else
        {
            Debug.LogWarning("请分配一个目标物体！");
        }
    }
}
