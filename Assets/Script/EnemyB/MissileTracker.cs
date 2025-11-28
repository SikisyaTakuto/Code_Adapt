using UnityEngine;

public class MissileTracker : MonoBehaviour
{
    // --- 追尾設定 ---
    public float trackingSpeed = 10f;  // 追尾速度
    public float rotationSpeed = 3f;   // 追尾時のミサイルの回転速度
    public float lifetime = 5f;        // 寿命 (長すぎるといつまでも飛び続けるため)

    private Transform target;
    private Rigidbody rb;

    void Start()
    {
        // プレイヤーをターゲットとして自動的に取得
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            target = playerObj.transform;
        }
        else
        {
            // プレイヤーが見つからない場合は直進する
            Destroy(gameObject, lifetime);
            return;
        }

        rb = GetComponent<Rigidbody>();

        // 寿命設定
        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        if (target == null || rb == null) return;

        // 1. プレイヤーの方向を計算
        Vector3 directionToTarget = (target.position - transform.position).normalized;

        // 2. 追尾のために回転
        // 常にターゲットの方向を向くように回転
        Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * rotationSpeed);

        // 3. 追尾速度で移動 (現在のRigidbodyの速度を上書き)
        // 追尾方向に速度を設定
        rb.velocity = transform.forward * trackingSpeed;
    }
}