using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset; // プレイヤーからの相対的な位置 (X, Y, Z)
    public float smoothSpeed = 5.0f; // 追従の滑らかさ
    public float smoothRotation = 5.0f; // 回転の滑らかさ

    void LateUpdate()
    {
        if (target == null) return;

        // 1. 目標位置の計算 (プレイヤーの回転を考慮)
        // オフセットをプレイヤーのローカル座標からワールド座標に変換する
        Vector3 desiredPosition = target.position + target.rotation * offset;

        // 2. 位置を滑らかに移動
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );
        transform.position = smoothedPosition;

        // 3. 目標の回転の計算 (プレイヤーの向きに追従)
        // カメラの回転を、プレイヤーの回転に合わせる (または滑らかに向かせる)
        Quaternion desiredRotation = target.rotation;

        // 4. 回転を滑らかに適用
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            smoothRotation * Time.deltaTime
        );
    }
}