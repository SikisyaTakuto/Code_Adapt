using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform target;               // プレイヤーのTransform
    public Rigidbody targetRb;             // プレイヤーのRigidbody（速度取得用）

    [Header("カメラ距離設定")]
    public float minDistance = 5f;         // 最小距離
    public float maxDistance = 12f;        // 最大距離（高速時）
    public float followHeight = 2f;        // 高さ
    public float smoothSpeed = 5f;         // 補間速度

    [Header("速度の影響度")]
    public float speedFactor = 0.1f;       // 速度→距離への変換倍率

    private Vector3 currentVelocity;

    void LateUpdate()
    {
        if (target == null || targetRb == null) return;

        // プレイヤーの速度を取得し、距離を調整
        float speed = targetRb.linearVelocity.magnitude;
        float distance = Mathf.Lerp(minDistance, maxDistance, speed * speedFactor);

        // カメラの理想位置（ターゲットの後ろ＋高さ）
        Vector3 desiredPosition = target.position
                                - target.forward * distance
                                + Vector3.up * followHeight;

        // スムーズにカメラを移動
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1f / smoothSpeed);

        // ターゲットを常に見る
        transform.LookAt(target.position + Vector3.up * followHeight);
    }
}
