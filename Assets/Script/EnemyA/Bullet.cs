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

    public void Initialize(Action<Bullet> onReturn, float speed, float lifetime)
    {
        returnAction = onReturn;

        // ?? 【追加】発射前に念のためRigidbodyをクリア
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        // ---------------------------------------- ??

        Invoke("ReturnToPool", lifetime);

        // 速度設定はEnemyAI.csのShootBullet()で行うため、ここでは処理を省略
    }

    // ★★★ 削除: Update()メソッドを削除しました。移動はRigidbodyが担当します。 ★★★

    private void OnTriggerEnter(Collider other)
    {
        // 衝突相手が「プレイヤー」または「環境オブジェクト」（壁や地面）であるか確認
        // ※ 衝突レイヤー設定やタグ、またはレイヤーマスクで制御するのが最も効率的です。

        // 例：タグによるチェック
        if (other.CompareTag("Player") || other.CompareTag("Wall"))
        {
            // 衝突処理（ダメージなど）

            // プールに返す
            ReturnToPool();
        }
        // ※ 衝突相手が「Enemy」タグだったら何もしない、など
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