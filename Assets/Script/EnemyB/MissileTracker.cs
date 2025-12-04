using UnityEngine;

public class MissileTracker : MonoBehaviour
{
    // --- 追尾設定 ---
    public float trackingSpeed = 10f;  // 追尾速度
    public float rotationSpeed = 3f;   // 追尾時のミサイルの回転速度
    public float lifetime = 5f;        // 寿命 (長すぎるといつまでも飛び続けるため)

    private Transform target;
    private Rigidbody rb;

    // 💡 【ここを追加】爆発エフェクトのプレハブ
    public GameObject explosionPrefab;

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

    [System.Obsolete]
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

    void OnCollisionEnter(Collision collision)
    {
        // 1. 爆発エフェクトの生成
        if (explosionPrefab != null)
        {
            // 衝突が発生した場所（最初の接触点）に爆発を生成
            // 💡 Quaternion.identity は回転なしを意味します
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        // 2. ミサイル本体の破棄（消滅）
        // プレイヤー、床、壁など、何に当たっても即座に消滅します。
        Destroy(gameObject);

        // 注意: 衝突したのが敵ロボット自身だった場合、ミサイルが消えるのが早すぎる可能性があります。
        // もし敵ロボット自身に当たっても消滅させたくない場合は、
        // if (collision.gameObject.CompareTag("Enemy")) return; 
        // のような除外処理を追加してください。
    }
}