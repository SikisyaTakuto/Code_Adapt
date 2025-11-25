using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 20f; // 弾速
    public float damage = 10f; // ダメージ量
    public float lifetime = 3f; // 💡 ここに設定した時間で消滅します

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 自身の前方(Z軸)に速度を設定
            rb.velocity = transform.forward * speed;
        }

        // 💡 lifetime秒後にGameObjectを破棄（自動消滅）
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter(Collider other)
    {
        // プレイヤーに当たったかチェック
        if (other.CompareTag("Player"))
        {
            Debug.Log("プレイヤーに弾丸がヒット！");
            // ダメージ処理（PlayerHealth.TakeDamageなど）を実装
        }

        // 弾丸を消滅させる（当たり判定の後にすぐ消す）
        Destroy(gameObject);
    }
}