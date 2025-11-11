using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset; // プレイヤーからの相対的な位置 (X, Y, Z)
    public float smoothSpeed = 5.0f; // 追従の滑らかさ

    void LateUpdate()
    {
        if (target == null) return;

        // 目標位置 = プレイヤーの位置 + オフセット
        Vector3 desiredPosition = target.position + offset;

        // 滑らかに移動
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        transform.position = smoothedPosition;
    }
}