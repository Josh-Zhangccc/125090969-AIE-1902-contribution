using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 临时 WASD 移动脚本，用于测试触发系统。后续由正式玩家控制器替代。
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float lookSpeed = 2f;
    public bool disableMouseLook;

    private float yaw;

    private void Start()
    {
        yaw = transform.eulerAngles.y;
    }

    private void Update()
    {
        if (Keyboard.current == null || Mouse.current == null)
            return;

        // 鼠标右键旋转视角
        if (Mouse.current.rightButton.isPressed && !disableMouseLook)
        {
            yaw += Mouse.current.delta.x.ReadValue() * lookSpeed;
            transform.rotation = Quaternion.Euler(0, yaw, 0);
        }

        // WASD 移动
        var move = Vector3.zero;
        if (Keyboard.current.wKey.isPressed) move.z += 1;
        if (Keyboard.current.sKey.isPressed) move.z -= 1;
        if (Keyboard.current.aKey.isPressed) move.x -= 1;
        if (Keyboard.current.dKey.isPressed) move.x += 1;

        transform.Translate(speed * Time.deltaTime * move, Space.Self);
    }
}
