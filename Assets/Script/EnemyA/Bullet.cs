using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))] // Rigidbodyを必須にする
public class Bullet : MonoBehaviour
{
    // private float moveSpeed = 10f; // ★ 削除: EnemyAI側でRigidbodyの速度を設定するため不要
    private Rigidbody rb;
    private Action<Bullet> returnAction; // プールに返すためのデリゲート

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Rigidbodyを使用する場合、IsKinematicはオフ、Collision DetectionはContinuous/Continuous Speculative推奨
        // ColliderはIsTriggerがオンになっていることを想定
    }

    /// <summary>
    /// 弾を発射する時に初期設定を行い、自動返却時間を設定する
    /// </summary>
    public void Initialize(Action<Bullet> onReturn, float speed, float lifetime)
    {
        returnAction = onReturn;

        // 弾を一定時間後に自動で返す処理を呼び出す
        Invoke("ReturnToPool", lifetime);

        // ※ 弾の速度設定はEnemyAI.csのShootBullet()で行うため、ここでは処理を省略
    }

    // ★★★ 削除: Update()メソッドを削除しました。移動はRigidbodyが担当します。 ★★★

    // 敵や壁に当たった時の処理
    private void OnTriggerEnter(Collider other)
    {
        // 衝突処理（ダメージなど）

        // プールに返す
        ReturnToPool();
    }

    // プールに返す処理
    private void ReturnToPool()
    {
        // Invokeで設定した自動返却をキャンセル（衝突で返却済みの場合に備える）
        CancelInvoke("ReturnToPool");

        // ★★★ リセット処理 ★★★
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 弾丸の速度をゼロにリセット
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        // パーティクルシステムがある場合は停止
        var ps = GetComponent<ParticleSystem>();
        if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        // ★★★ -------------------- ★★★

        // 外部のPoolManagerに自身を返すように依頼
        returnAction?.Invoke(this);

        // 非アクティブにする（オブジェクトの「消滅」）
        gameObject.SetActive(false);
    }
}