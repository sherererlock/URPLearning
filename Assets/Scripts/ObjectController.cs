using UnityEngine;

public class ObjectController : MonoBehaviour
{
    public float rotationSpeed = 30f; // 自动旋转速度
    public float movementSpeed = 2f;  // 自动移动速度

    void Update()
    {
        // 功能1：随着时间自动旋转物体
        AutoRotate();

        // 功能2：随着时间自动左右移动物体
        AutoMove();
    }

    void AutoRotate()
    {
        // 自动旋转
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    void AutoMove()
    {
        // 计算左右移动
        float horizontalMovement = Mathf.Sin(Time.time) * movementSpeed;

        // 应用左右移动
        transform.Translate(new Vector3(horizontalMovement, 0f, 0f) * Time.deltaTime);
    }
}
