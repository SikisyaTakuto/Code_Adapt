using UnityEngine;

public class bullet : MonoBehaviour
{
    [SerializeField] private float lifeTime = 5f; // 弾丸の寿命 (5秒後に自動で消える)

    void Start()
    {
        // 寿命が来たら自分自身を破壊する
        Destroy(gameObject, lifeTime);
    }

    // 他のオブジェクトに衝突したときの処理
    private void OnCollisionEnter(Collision collision)
    {
        // 例: プレイヤーに当たった場合
        if (collision.gameObject.CompareTag("Player"))
        {
            // プレイヤーにダメージを与える処理などを記述...
            Debug.Log("Player Hit!");
        }

        // 弾丸は衝突したら消滅させる
        Destroy(gameObject);
    }
}