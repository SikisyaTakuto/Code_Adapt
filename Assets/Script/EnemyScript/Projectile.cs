using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 50f;
    public float damage = 10f;
    public float lifetime = 3f;

    void Start()
    {
        // ライフタイムを設定
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // 前方に直進
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        // プレイヤーのタグをチェック (例: PlayerControllerがあるオブジェクト)
        if (other.CompareTag("Player"))
        {
            // プレイヤーにダメージを与えるロジックを呼び出し (TakeDamageメソッドを想定)
            // if (other.TryGetComponent<PlayerController>(out var player))
            // {
            //     player.TakeDamage(damage);
            // }

            // 弾を削除
            Destroy(gameObject);
        }
        // 他のオブジェクト (壁など) に当たったら削除
        else if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}