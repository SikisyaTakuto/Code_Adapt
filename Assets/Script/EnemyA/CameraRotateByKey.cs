using UnityEngine;

public class CameraRotateByKey : MonoBehaviour
{
    [Tooltip("カメラの回転速度")]
    public float rotationSpeed = 90f; // 1秒間に回転する角度 (例: 90度)

    [Tooltip("左回転キー")]
    public KeyCode rotateLeftKey = KeyCode.Q;

    [Tooltip("右回転キー")]
    public KeyCode rotateRightKey = KeyCode.E;

    void Update()
    {
        float rotationAmount = 0f;

        // 1. 左回転の入力チェック
        if (Input.GetKey(rotateLeftKey))
        {
            // Qキーが押されている場合、左（Y軸のマイナス方向）へ回転
            rotationAmount = -rotationSpeed;
        }
        // 2. 右回転の入力チェック
        else if (Input.GetKey(rotateRightKey))
        {
            // Eキーが押されている場合、右（Y軸のプラス方向）へ回転
            rotationAmount = rotationSpeed;
        }

        // 3. 実際に回転を適用
        if (rotationAmount != 0f)
        {
            // transform.Rotate(回転軸 * 角度 * 時間)
            transform.Rotate(Vector3.up * rotationAmount * Time.deltaTime);
        }
    }
}