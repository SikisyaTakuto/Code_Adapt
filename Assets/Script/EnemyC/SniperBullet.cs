using UnityEngine;

public class SniperBullet : MonoBehaviour
{
    [Header("弾の設定")]
    public float speed = 100f;      // 💡 弾速：スナイパーなら 100 くらい速くてもOK
    public float lifeTime = 10.0f;  // 💡 生存時間：ここを「10」以上にすれば、まず消えません
    public float damage = 20f;

    void Start()
    {
        // 指定した時間（lifeTime）が経過したら、届いていなくても消去する
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // 弾を真っ直ぐ進ませる
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // プレイヤーに当たった場合
        if (other.CompareTag("Player"))
        {
            // ダメージ処理（もしあれば）
            // other.GetComponent<PlayerHealth>()?.TakeDamage(damage);
            Destroy(gameObject);
        }
        // 壁や床に当たった場合
        else if (other.CompareTag("Ground") || other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}