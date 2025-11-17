using UnityEngine;
// using System; // Action<T>を使わないため、Systemを削除

public class bullet : MonoBehaviour
{
    // ★ 速度はEnemyAI側でRigidbodyに設定することが推奨されますが、ここではtransform移動として残します
    public float moveSpeed = 10f;

    // private Action<Bullet> returnAction; // ★ 削除: プールを使わないため不要

    // 弾の寿命 (Destroyで使う)
    // EnemyAI側で制御しているため、このスクリプトでは定義しないか、外部から設定されることを前提とする
    // 今回は、EnemyAI側がDestroy(bullet, lifetime)で寿命を設定しているため、Initializeも不要です。

    // public void Initialize(Action<Bullet> onReturn) // ★ 削除: プールを使わないため不要
    // {
    //     returnAction = onReturn;
    //     Invoke("ReturnToPool", 5f);
    // }

    void Update()
    {
        // 弾を前方に移動させる
        // Rigidbodyを使用しない単純な移動。高速な弾では壁抜けの原因になることがあります。
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
    }

    // 敵や壁に当たった時の処理
    private void OnTriggerEnter(Collider other)
    {
        // 衝突処理（ダメージなど）

        // ★ プールに返す代わりに、完全に破棄する
        DestroyBullet();
    }

    /// <summary>
    /// 弾をゲームから完全に消滅させる (Destroyを使用)
    /// </summary>
    private void DestroyBullet()
    {
        // Invokeで設定した自動破棄処理（もしあれば）をキャンセル
        // ※ このInitializeメソッドが削除されたため、ここではキャンセル処理は不要です。

        // ★ オブジェクトを完全に破棄する
        Destroy(gameObject);

        // Debug.Log("弾をDestroyしました。");
    }
}