using UnityEngine;

public class HeavyMissile : MonoBehaviour
{
    [Header("Missile Settings")]
    [Tooltip("ミサイルが与えるダメージ量。")]
    public float damageAmount = 100f; // このミサイルが与えるダメージ
    [Tooltip("ミサイルの移動速度。")]
    public float missileSpeed = 15f; // ミサイルの速度
    [Tooltip("ミサイルがターゲットを追尾する時間。")]
    public float trackingDuration = 3.0f; // 自動追尾する時間
    [Tooltip("ミサイルが消滅するまでの総時間。")]
    public float lifeTime = 5.0f; // ミサイルの寿命（追尾時間＋α）
    [Tooltip("ミサイルがダメージを与える対象のタグ。")]
    public string targetTag = "Player"; // 当たり判定の対象とするタグ
    [Tooltip("ミサイルの旋回速度（追尾の滑らかさ）。")]
    public float turnSpeed = 5f; // 追尾時の旋回速度

    private Transform target; // 追尾するターゲット（プレイヤー）
    private float trackingTimer; // 追尾時間計測用

    void Awake()
    {
        // プレイヤーオブジェクトをタグで検索してターゲットに設定
        GameObject playerObject = GameObject.FindGameObjectWithTag(targetTag);
        if (playerObject != null)
        {
            target = playerObject.transform;
        }
        else
        {
            Debug.LogWarning($"ミサイルのターゲット ({targetTag}) が見つかりません。ミサイルは追尾しません。");
        }
    }

    void Start()
    {
        trackingTimer = trackingDuration; // 追尾タイマーを初期化
        Destroy(gameObject, lifeTime); // ミサイルの寿命を設定
    }

    void Update()
    {
        // 追尾時間内かつターゲットが存在する場合
        if (trackingTimer > 0 && target != null)
        {
            // ターゲット方向へ徐々に回転
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * turnSpeed);

            trackingTimer -= Time.deltaTime; // タイマーを減らす
        }

        // ミサイルを前方に移動させる
        transform.position += transform.forward * missileSpeed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        // 衝突したオブジェクトのタグがターゲットタグと一致するか確認
        if (other.CompareTag(targetTag))
        {
            //PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            //if (playerHealth != null)
            //{
            //    playerHealth.TakeDamage(damageAmount);
            //    Debug.Log($"ミサイルが {other.name} に {damageAmount} ダメージを与えました。");
            //}
            // ミサイルは衝突したら消滅
            Destroy(gameObject);
        }
    }

    // デバッグ表示用
    void OnDrawGizmos()
    {
        if (target != null && trackingTimer > 0)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}