using UnityEngine;

public class EnemyBeamController : MonoBehaviour
{
    [Header("Visual Settings")]
    public float lifetime = 2.0f; // 弾が消えるまでの時間
    public float speed = 30f;     // 弾の移動速度

    [Header("Damage Settings")]
    public float damageAmount = 100f;

    private bool hasDealtDamage = false;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // トリガーとして判定するため、Rigidbodyの設定をコードで保証
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true; // 物理演算で飛ばすならfalse、自前で動かすならtrue
        }
    }

    // 発射時に呼ばれる（移動方向をセット）
    public void Launch(Vector3 direction)
    {
        // 速度ベクトルをセット
        if (rb != null)
        {
            // direction方向に真っ直ぐ飛ばす
            transform.forward = direction;
        }
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // 弾を前方に移動させる
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // すでに何かに当たっていたら無視
        if (hasDealtDamage) return;

        // PlayerStatusを探す
        PlayerStatus status = other.GetComponentInParent<PlayerStatus>() ??
                             other.GetComponentInChildren<PlayerStatus>() ??
                             other.GetComponent<PlayerStatus>();

        if (status != null)
        {
            hasDealtDamage = true;

            // 防御倍率などの取得（以前のロジック）
            float defense = 1.0f;
            // ※必要に応じて各Controllerの取得処理を追加

            status.TakeDamage(damageAmount, defense);
            Debug.Log($"[Effect Hit] {other.name} にヒット！");

            // ヒットしたらエフェクトを消す（貫通させたいならここを消す）
            Destroy(gameObject);
        }
        else if (other.CompareTag("Wall")) // 壁に当たった場合
        {
            Destroy(gameObject);
        }
    }
}