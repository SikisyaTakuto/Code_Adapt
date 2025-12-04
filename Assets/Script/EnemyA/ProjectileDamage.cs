using UnityEngine;

public class ProjectileDamage : MonoBehaviour
{
    public float damageAmount = 10f;
    public float lifeTime = 5f;

    // 💡 衝突バグ回避のため、ダメージを与えた弾は即座に停止し、破棄します。
    private bool hasDealtDamage = false;

    void Start()
    {
        // 寿命設定（5秒後に自動消滅）
        Destroy(gameObject, lifeTime);
    }

    [System.Obsolete]
    void OnTriggerEnter(Collider other)
    {
        // 💡 衝突した相手のオブジェクトが、まだ有効に存在しているかチェック (必須)
        if (other == null || other.gameObject == null)
        {
            return;
        }

        // 既に処理済み、または自分自身(Player)の弾の場合はスキップ
        if (hasDealtDamage || other.CompareTag("Player"))
        {
            return;
        }

        // -----------------------------------------------------------------
        // 敵 (Enemyタグ) に当たった場合
        // -----------------------------------------------------------------

        // 💡 ターゲットタグを Enemy に変えるため、ここではタグのチェックは不要
        EnemyHealth enemyHealth = other.gameObject.GetComponent<EnemyHealth>();

        if (enemyHealth != null)
        {
            // ... (敵へのダメージ処理は省略) ...
            enemyHealth.TakeDamage(damageAmount);

            // 衝突バグ回避処理 (必須)
            hasDealtDamage = true;

            // 物理演算を停止し、弾を即座に破棄
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            Destroy(gameObject);
        }
        // -----------------------------------------------------------------
        // 💡 敵ではない、床/壁などに当たった場合（タグを問わず）
        // -----------------------------------------------------------------
        else
        {
            // 敵 (EnemyHealth) がない、またはタグが Enemy でないオブジェクトに当たったら、
            // 処理をせずに即座に破棄する (貫通させない設定)
            Destroy(gameObject);
        }
    }
}