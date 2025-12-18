using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 50f;
    public float damage = 10f;
    public float lifetime = 3f;

    // 💡 Rigidbodyコンポーネネントへの参照を追加
    private Rigidbody rb;
    private bool isBeingDestroyed = false;

    void Awake()
    {
        // Rigidbodyを取得
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Bullet プレハブには Rigidbody コンポーネントが必要です。", this);
            enabled = false; // Rigidbodyがない場合、スクリプトを無効にする
        }
    }

    void Start()
    {
        Destroy(gameObject, lifetime);

        if (rb != null)
        {
            // 💡 もし弾が「お尻」を向けて飛んでいるなら 
            // transform.forward の代わりに -transform.forward (マイナス) を使う
            rb.linearVelocity = transform.forward * speed;

            // 💡 もし「横」を向いて飛んでいるなら
            // rb.velocity = transform.right * speed; 
            // など、ここを書き換えるだけで飛ぶ方向を調整できます。
        }
    }

    // 💡 Update() 関数は、移動処理がないため、このままでは不要ですが、残しておきます。
    void Update()
    {
        // 💡 移動処理はRigidbodyが担当するため、Translateは不要になりました。
    }

    void OnTriggerEnter(Collider other)
    {
        if (isBeingDestroyed) return;

        // プレイヤーに当たったかチェック
        if (other.CompareTag("Player"))
        {
            Debug.Log("プレイヤーに弾丸がヒット！");
            // ダメージ処理を実装
        }

        // 💡 破壊フラグを設定し、即時破棄を実行
        isBeingDestroyed = true;

        // 衝突で弾丸が停止した後に少し遅延を設けて破壊するなど、演出に合わせて調整可能
        Destroy(gameObject);
    }
}