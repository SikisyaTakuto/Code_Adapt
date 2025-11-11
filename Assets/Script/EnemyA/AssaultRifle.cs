using UnityEngine;

public class Bullet : MonoBehaviour
{
    // 弾の移動速度
    public float speed = 10f;

    void Update()
    {
        // 弾を前方向（ローカルZ軸）に移動させる
        // transform.forward は、そのオブジェクトの現在の前方向ベクトル
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    // 衝突時の処理 (オプション: 敵の弾なのでプレイヤーに当たったら消えるなど)
    private void OnTriggerEnter(Collider other)
    {
        // 例: プレイヤーに当たったら弾を消す
        if (other.CompareTag("Player"))
        {
            // プレイヤーにダメージを与える処理などをここに追加...
            Destroy(gameObject);
        }
    }
}